using System;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Applies the configured pitch limits to the controller's look-pitch controller.
/// </summary>
internal static class LookLimits
{
    private static ETCTransformCtrl? pitchCtrl;
    private static bool baseClamp;
    private static float baseMin;
    private static float baseMax;
    private static bool hasBaseline;
    private static bool applied;

    /// <summary>
    /// Samples the game's own FPS pitch limits via <see cref="ETCTransformCtrl.GetAngleLimitX"/>.
    /// </summary>
    internal static void CaptureBaseline()
    {
        if (applied || hasBaseline)
            return;

        try
        {
            var etc = Resolve();
            if (etc is null)
                return;

            baseClamp = etc.m_isClampRotationX;
            etc.GetAngleLimitX(out baseMin, out baseMax);
            hasBaseline = true;
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

    /// <summary>
    /// Applies the configured pitch limits to the controller ETC each frame.
    /// </summary>
    internal static void Apply()
    {
        try
        {
            var etc = Resolve();
            if (etc is null)
                return;

            etc.m_minAngleX = Mathf.Abs(Plugin.PitchMin.Value);
            etc.m_maxAngleX = Mathf.Abs(Plugin.PitchMax.Value);
            etc.m_isClampRotationX = true;
            applied = true;
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

    /// <summary>
    /// Hands the native pitch limits back via <see cref="ETCTransformCtrl.SetAngleLimitX"/>.
    /// </summary>
    internal static void Restore()
    {
        if (!applied)
            return;
        applied = false;

        try
        {
            var etc = Resolve();
            if (etc is null)
                return;

            if (hasBaseline)
            {
                etc.SetAngleLimitX(baseMin, baseMax);
                etc.m_isClampRotationX = baseClamp;
            }
            else
            {
                etc.m_isClampRotationX = false;
            }
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

    /// <summary>
    /// Resets controller pitch to level after a cutscene or revive.
    /// </summary>
    internal static void ResetPitch()
    {
        try
        {
            var etc = Resolve();
            if (etc?.transform is not { } t)
                return;

            etc.m_LastVerticalValue = 0f;
            try
            {
                if (ETCTransformCtrl.ControllerPitchETCCtrl is { } pitchEtc && !ReferenceEquals(pitchEtc, etc))
                    pitchEtc.m_LastVerticalValue = 0f;
                if (ETCTransformCtrl.ControllerETCCtrl is { } ctrlEtc && !ReferenceEquals(ctrlEtc, etc))
                    ctrlEtc.m_LastVerticalValue = 0f;
            }
            catch (Exception)
            {
            }

            var e = t.localEulerAngles;
            t.localEulerAngles = new Vector3(0f, e.y, e.z);
            if (t.parent is not null)
            {
                var pe = t.parent.localEulerAngles;
                t.parent.localEulerAngles = new Vector3(0f, pe.y, pe.z);
            }
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

    /// <summary>
    /// Seeds the controller pitch on TPS entry so the camera continues the FPS look direction.
    /// </summary>
    internal static void SeedPitch(float pitchDeg, CameraBoom? boom)
    {
        try
        {
            WritePitch(Resolve(), pitchDeg);
            try
            {
                var other = ETCTransformCtrl.ControllerPitchETCCtrl;
                if (!ReferenceEquals(other, pitchCtrl))
                    WritePitch(other, pitchDeg);
            }
            catch (Exception)
            {
            }
            try { WriteEuler(HeroCtrlMgr.Controller, pitchDeg); } catch (Exception) { }
            try { WriteEuler(boom?.parentEtcTranCtrl?.transform, pitchDeg); } catch (Exception) { }
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

    /// <summary>
    /// Clears the ETC's queued rotation delta and writes pitch on its transform.
    /// </summary>
    private static void WritePitch(ETCTransformCtrl? etc, float pitchDeg)
    {
        if (etc is null)
            return;
        try
        {
            etc.ClearRotateVector("");
            WriteEuler(etc.transform, pitchDeg);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Sets a transform's local pitch, preserving yaw and roll.
    /// </summary>
    private static void WriteEuler(Transform? t, float pitchDeg)
    {
        if (t is null)
            return;
        var e = t.localEulerAngles;
        t.localEulerAngles = new Vector3(pitchDeg, e.y, e.z);
    }

    /// <summary>
    /// Clears the cached controller and baseline
    /// </summary>
    internal static void Clear()
    {
        pitchCtrl = null;
        applied = false;
        hasBaseline = false;
    }

    /// <summary>
    /// Resolves the controller's ETC component, with a hierarchy fallback.
    /// </summary>
    private static ETCTransformCtrl? Resolve()
    {
        if (pitchCtrl is not null)
        {
            try
            {
                if (pitchCtrl.gameObject is not null)
                    return pitchCtrl;
            }
            catch (Exception)
            {
            }

            pitchCtrl = null;
        }

        try
        {
            pitchCtrl = ETCTransformCtrl.ControllerETCCtrl;
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }

        if (pitchCtrl is null)
        {
            var go = GameObject.Find("PlayerControllerPitch/PlayerController");
            pitchCtrl = go?.GetComponent<ETCTransformCtrl>();
        }

        return pitchCtrl;
    }
}
