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

    private readonly HeroRig rig = new HeroRig();
    private readonly CameraBoomTuner boomTuner = new CameraBoomTuner();

    private bool applied;
    private int settleCountdown;
    private int reassertIdleFrames;
    private bool pitchResetPending;
    private bool suppressed;
    private bool rebindPending;
    private bool exiting;

    private IntPtr heroPtr;
    private NewPlayerObject? heroObj;

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
            pitchResetPending = true;
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
            else if (!suppressed && !exiting)
            {
                LookLimits.CaptureBaseline();
                SensitivityTuner.CaptureBaseline();
            }

            if (exiting && !BlendActive())
                FinalizeExit();

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

        if (pitchResetPending)
        {
            pitchResetPending = false;
            LookLimits.ResetPitch();
        }

        if (settleCountdown > 0)
        {
            settleCountdown--;
            return;
        }

        heroObj = hero;
        if (heroObj.gameTrans is null)
            return;

        if (rebindPending)
        {
            rebindPending = false;
            if (applied)
            {
                rig.Bind(heroObj);
                MoveAnimDriver.Bind(heroObj);
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
        if ((!applied && !exiting) || suppressed)
            return;

        FacingMode.Apply(Plugin.StrafeFacing.Value);
        rig.EnsureCameras();
        AimConverger.Apply(rig.FpsCamTransform, rig.TpsCamTransform);
        LookLimits.Apply();

        boomTuner.Apply(rig.Boom, rig.NormalCs, exitBlend: exiting);
        SensitivityTuner.Apply();
    }

    /// <summary>
    /// Pushes the config values the native transition reads at call time.
    /// </summary>
    private void PreloadBoomConfig() => boomTuner.Apply(rig.Boom, rig.NormalCs, skipArm: true);

    private void EnterTps()
    {
        heroPtr = heroObj!.Pointer;
        rig.Bind(heroObj);
        MoveAnimDriver.Bind(heroObj);
        applied = true;
        exiting = false;

        LookLimits.CaptureBaseline();
        SensitivityTuner.Restore();
        SensitivityTuner.CaptureBaseline();
        PreloadBoomConfig();
        var fpsPitch = 0f;
        try
        {
            var x = rig.FpsCamTransform?.eulerAngles.x ?? 0f;
            fpsPitch = x > 180f ? x - 360f : x;
        }
        catch (Exception)
        {
        }
        LookLimits.SeedPitch(fpsPitch, rig.Boom);

        try
        {
            heroObj.ChangeTpsState(true, StateKey);
            var curve = TransitionCurve(entering: true);
            if (curve is not null)
                HeroCameraManager.ChangeToTpsMode(TpsMode.FREE, curve);
            else
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
        exiting = true;

        try
        {
            if (heroObj is not null)
                heroObj.ChangeTpsState(false, StateKey);

            if (!TryNativeFpsExit())
            {
                rig.Deactivate();
                FinalizeExit();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Native TPS exit failed ({ex.Message}); using direct deactivation.");
            rig.Deactivate();
            FinalizeExit();
        }
    }

    private static bool BlendActive()
    {
        try
        {
            var cs = HeroCameraManager.CSObject?.TryCast<NormalCS>();
            return cs is not null && cs.isChanging;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Restores native config/axis/limit state after the exit blend finishes.
    /// </summary>
    private void FinalizeExit()
    {
        exiting = false;
        boomTuner.Restore();
        SensitivityTuner.Restore();
        LookLimits.Restore();
        FacingMode.Restore();
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
            if (!HeroCameraManager.InNormal())
                return true;
            var fd = CharacterMotionManager.HeroObj?.FakerDeadCom;
            return fd is not null && fd.IsFakerDead;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private bool TryNativeFpsExit()
    {
        try
        {
            HeroCameraManager.ChangeToFpsMode(TransitionCurve(entering: false) ?? new AnimationCurve());
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"HeroCameraManager.ChangeToFpsMode failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Builds the native arm-length blend curve from <see cref="Plugin.CameraTransitionTime"/>.
    /// </summary>
    private static AnimationCurve? TransitionCurve(bool entering)
    {
        var t = Plugin.CameraTransitionTime.Value;
        if (t <= 0f)
            return null;
        return entering
            ? AnimationCurve.EaseInOut(0f, 0f, t, 1f)
            : AnimationCurve.EaseInOut(0f, 1f, t, 0f);
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

        if (applied || exiting)
        {
            boomTuner.Restore();
            FacingMode.Restore();
            exiting = false;
        }

        LookLimits.Restore();
        LookLimits.Clear();
        SensitivityTuner.Restore();
        SensitivityTuner.Clear();
        applied = false;
        rebindPending = false;
        pitchResetPending = false;
        settleCountdown = SettleFrames;
        heroObj = null;
        heroPtr = IntPtr.Zero;
        rig.Clear();
        MoveAnimDriver.Clear();
    }
}
