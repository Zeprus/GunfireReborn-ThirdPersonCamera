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
    /// Samples the live clamp state while the mod is not touching it.
    /// </summary>
    internal static void CaptureBaseline()
    {
        if (applied)
            return;

        try
        {
            var etc = Resolve();
            if (etc is null)
                return;

            baseClamp = etc.m_isClampRotationX;
            baseMin = etc.m_minAngleX;
            baseMax = etc.m_maxAngleX;
            hasBaseline = true;
        }
        catch (Exception)
        {
            pitchCtrl = null;
        }
    }

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
                etc.m_minAngleX = baseMin;
                etc.m_maxAngleX = baseMax;
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
    /// Resets pitch to level after a cutscene or revive.
    /// </summary>
    internal static void ResetPitch()
    {
        try
        {
            var t = Resolve()?.transform;
            if (t is null)
                return;

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
    /// Clears the cached controller and baseline
    /// </summary>
    internal static void Clear()
    {
        pitchCtrl = null;
        applied = false;
        hasBaseline = false;
    }

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

        var go = GameObject.Find("PlayerControllerPitch/PlayerController");
        pitchCtrl = go?.GetComponent<ETCTransformCtrl>();
        return pitchCtrl;
    }
}
