using System;
using HeroCameraName;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Handles third-person state.
/// </summary>
internal sealed class TpsSession
{
    private const string StateKey = "ThirdPersonCamera";
    private const int VerifyFrames = 45;

    private readonly HeroRig rig = new HeroRig();
    private readonly CameraBoomTuner boomTuner = new CameraBoomTuner();

    private bool applied;
    private bool weAddedKey;
    private int verifyCountdown;
    private bool nativeTransitionFailed;
    private bool configDirty = true;

    private IntPtr heroPtr;
    private NewPlayerObject? heroObj;
    private Transform? heroTran;

    /// <summary>
    /// Marks the boom configuration for re-application.
    /// </summary>
    internal void MarkConfigDirty() => configDirty = true;

    /// <summary>
    /// Checks if third-person should be applied.
    /// </summary>
    internal void OnUpdate()
    {
        if (!Plugin.Enabled.Value)
        {
            if (applied)
                ExitTps();
            else
                LookLimits.CaptureBaseline();
            return;
        }

        var hero = CharacterMotionManager.HeroObj;
        if (hero is null || (applied && hero.Pointer != heroPtr))
        {
            Detach();
            if (hero is null)
                return;
        }

        if (Suppressed())
            return;

        heroObj = hero;
        heroTran = CharacterMotionManager.HeroTran ?? heroObj.gameTrans;
        if (heroTran is null)
            return;

        if (!applied)
            EnterTps();
        else if (!IsTpsActive())
            ReassertTps();
    }

    /// <summary>
    /// Apply overrides.
    /// </summary>
    internal void OnLateUpdate()
    {
        if (!applied)
            return;

        if (verifyCountdown > 0 && --verifyCountdown == 0)
            VerifyEnterResult();

        rig.ApplyWeaponCamState(IsTpsActive());
        FacingMode.Apply(Plugin.StrafeFacing.Value);
        AimConverger.Apply(rig.FpsCamTransform, rig.TpsCamTransform);
        LookLimits.Apply();

        if (configDirty)
        {
            configDirty = false;
            boomTuner.MarkDirty();
        }
        boomTuner.Apply(rig.Boom);
    }

    private void EnterTps()
    {
        heroPtr = heroObj!.Pointer;
        rig.Bind(heroTran);
        applied = true;

        try
        {
            if (!weAddedKey)
            {
                heroObj.ChangeTpsState(true, StateKey);
                weAddedKey = true;
            }

            if (nativeTransitionFailed)
            {
                rig.Activate();
            }
            else
            {
                HeroCameraManager.ChangeToTpsMode(TpsMode.FREE, 0f);
                verifyCountdown = VerifyFrames;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Native TPS enter failed ({ex.Message}); using direct activation.");
            nativeTransitionFailed = true;
            rig.Activate();
        }
    }

    private void ExitTps()
    {
        applied = false;
        verifyCountdown = 0;

        try
        {
            if (weAddedKey && heroObj is not null)
            {
                heroObj.ChangeTpsState(false, StateKey);
            }
            weAddedKey = false;

            if (nativeTransitionFailed || !TryNativeFpsExit())
                rig.Deactivate();
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Native TPS exit failed ({ex.Message}); using direct deactivation.");
            rig.Deactivate();
        }
        finally
        {
            boomTuner.Restore(rig.Boom);
            LookLimits.Restore();
            rig.RestoreWeaponCam();
            FacingMode.Restore();
        }
    }

    private void ReassertTps()
    {
        try
        {
            if (heroObj is not null && !heroObj.IsMoreTpsState())
            {
                weAddedKey = false;
                applied = false;
                EnterTps();
                return;
            }

            if (nativeTransitionFailed)
                rig.Activate();
            else
                HeroCameraManager.ChangeToTpsMode(TpsMode.FREE, 0f);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"TPS re-assert failed: {ex.Message}");
        }
    }

    private void VerifyEnterResult()
    {
        if (IsTpsActive())
            return;

        if (!nativeTransitionFailed)
        {
            Plugin.Log?.LogWarning("Native TPS transition did not take effect; activating rig objects directly.");
            nativeTransitionFailed = true;
        }

        rig.Activate();
    }

    private bool IsTpsActive()
    {
        bool boomActive = rig.IsBoomActive;
        try
        {
            return CameraManager.CurCameraMode == CameraMode.TPS || boomActive;
        }
        catch (Exception)
        {
            return boomActive;
        }
    }

    private static bool Suppressed()
    {
        try
        {
            if (HeroCameraManager.InCG)
                return true;

            var data = HeroCameraManager.CSDataObj;
            if (data is not null && data.IsLookOpen)
                return true;

            var watch = CameraManager.WatchCamera;
            if (watch is not null && watch.isActiveAndEnabled)
                return true;
        }
        catch (Exception)
        {
        }

        return false;
    }

    private bool TryNativeFpsExit()
    {
        try
        {
            var normalCs = HeroCameraManager.CSObject?.TryCast<NormalCS>();
            if (normalCs is not null)
            {
                normalCs.ChangeToFpsMode(0f);
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"NormalCS.ChangeToFpsMode failed: {ex.Message}");
        }

        try
        {
            HeroCameraManager.ChangeToFpsMode(new AnimationCurve());
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"HeroCameraManager.ChangeToFpsMode failed: {ex.Message}");
            return false;
        }
    }

    private void Detach()
    {
        LookLimits.Restore();
        applied = false;
        weAddedKey = false;
        heroObj = null;
        heroTran = null;
        heroPtr = IntPtr.Zero;
        rig.Clear();
    }
}
