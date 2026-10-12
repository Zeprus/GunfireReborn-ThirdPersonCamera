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
    private const int SettleFrames = 45;
    private const int ReassertKickFrames = 30;
    private const int PitchResetFrames = 3;

    private readonly HeroRig rig = new HeroRig();
    private readonly CameraBoomTuner boomTuner = new CameraBoomTuner();

    private bool applied;
    private int settleCountdown;
    private int reassertIdleFrames;
    private int pitchResetCountdown;
    private bool suppressed;
    private bool rebindPending;

    private IntPtr heroPtr;
    private NewPlayerObject? heroObj;
    private Transform? heroTran;

    /// <summary>
    /// Checks if third-person should be applied.
    /// </summary>
    internal void OnUpdate()
    {
        var isSuppressed = Suppressed();
        if (isSuppressed && !suppressed)
            LookLimits.Restore();
        else if (!isSuppressed && suppressed)
        {
            rebindPending = true;
            pitchResetCountdown = PitchResetFrames;
        }
        suppressed = isSuppressed;
        MoveAnimDriver.Suspended = suppressed;

        if (!Plugin.Enabled.Value)
        {
            if (applied)
            {
                if (suppressed)
                {
                    try
                    {
                        if (heroObj is not null)
                            heroObj.ChangeTpsState(false, StateKey);
                        HeroCameraManager.TarCamMode = CameraMode.FPS;
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    ExitTps();
                }
            }
            else if (!suppressed)
            {
                LookLimits.CaptureBaseline();
                SensitivityTuner.CaptureBaseline();
            }

            return;
        }

        var hero = CharacterMotionManager.HeroObj;
        if (hero is null || (applied && hero.Pointer != heroPtr))
        {
            Detach();
            if (hero is null)
                return;
        }

        if (suppressed)
        {
            settleCountdown = SettleFrames;
            return;
        }

        if (pitchResetCountdown > 0)
        {
            pitchResetCountdown--;
            LookLimits.ResetPitch();
        }

        if (settleCountdown > 0)
        {
            settleCountdown--;
            return;
        }

        heroObj = hero;
        heroTran = CharacterMotionManager.HeroTran ?? heroObj.gameTrans;
        if (heroTran is null)
            return;

        if (rebindPending)
        {
            rebindPending = false;
            if (applied)
            {
                rig.Bind(heroTran);
                MoveAnimDriver.Bind(heroTran);
            }
        }

        if (!applied)
            EnterTps();
        else if (!IsTpsActive())
            ReassertTps();
        else
            reassertIdleFrames = 0;
    }

    /// <summary>
    /// Apply overrides.
    /// </summary>
    internal void OnLateUpdate()
    {
        if (!applied || suppressed)
            return;

        rig.ApplyWeaponCamState(IsTpsActive());
        FacingMode.Apply(Plugin.StrafeFacing.Value);
        AimConverger.Apply(rig.FpsCamTransform, rig.TpsCamTransform);
        LookLimits.Apply();
        MoveAnimDriver.Apply();

        boomTuner.Apply(rig.Boom);
        SensitivityTuner.Apply();
    }

    private void EnterTps()
    {
        heroPtr = heroObj!.Pointer;
        rig.Bind(heroTran);
        MoveAnimDriver.Bind(heroTran);
        applied = true;

        try
        {
            heroObj.ChangeTpsState(true, StateKey);
            HeroCameraManager.ChangeToTpsMode(TpsMode.FREE, 0f);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Native TPS enter failed: {ex.Message}");
        }
    }

    private void ExitTps()
    {
        applied = false;

        try
        {
            if (heroObj is not null)
                heroObj.ChangeTpsState(false, StateKey);

            if (!TryNativeFpsExit())
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
            SensitivityTuner.Restore();
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
                applied = false;
                EnterTps();
                return;
            }

            var normalCs = HeroCameraManager.CSObject?.TryCast<NormalCS>();
            if (normalCs is not null && (normalCs.isChanging || normalCs.tarCamMode == CameraMode.TPS))
                return;

            if (++reassertIdleFrames < ReassertKickFrames)
                return;
            reassertIdleFrames = 0;

            HeroCameraManager.ChangeToTpsMode(TpsMode.FREE, 0f);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"TPS re-assert failed: {ex.Message}");
        }
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
            var cs = HeroCameraManager.CSObject;
            return cs is null || cs.TryCast<NormalCS>() is null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool TryNativeFpsExit()
    {
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
        try
        {
            if (heroObj is not null)
                heroObj.ChangeTpsState(false, StateKey);
        }
        catch (Exception)
        {
        }

        LookLimits.Restore();
        LookLimits.Clear();
        SensitivityTuner.Restore();
        applied = false;
        rebindPending = false;
        settleCountdown = SettleFrames;
        heroObj = null;
        heroTran = null;
        heroPtr = IntPtr.Zero;
        rig.Clear();
        MoveAnimDriver.Clear();
    }
}
