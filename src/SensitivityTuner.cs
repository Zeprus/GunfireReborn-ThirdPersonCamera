using System;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Scales the native look-axis sensitivity while third-person is active.
/// <see cref="JoystickTouch_logic"/> stores separate sensitivity standards for
/// first/third person and mouse/controller; the game re-derives
/// <see cref="ETCAxis.speed"/> from them on every camera-mode switch, so the
/// multiplier is applied to the standards rather than the derived speed.
/// </summary>
internal static class SensitivityTuner
{
    private static JoystickTouch_logic? logic;
    private static ETCTouchPad? pad;
    private static bool hasBaseline;
    private static float baseTpsX;
    private static float baseTpsY;
    private static float baseCtrTpsX;
    private static float baseCtrTpsY;

    /// <summary>
    /// Samples the live sensitivity standards while the mod is not touching them.
    /// </summary>
    internal static void CaptureBaseline()
    {
        try
        {
            if (!Resolve())
                return;

            baseTpsX = logic!.m_axisXStandard_Tps;
            baseTpsY = logic!.m_axisYStandard_Tps;
            baseCtrTpsX = logic!.m_Ctr_axisXStandard_Tps;
            baseCtrTpsY = logic!.m_Ctr_axisYStandard_Tps;
            hasBaseline = true;
        }
        catch (Exception)
        {
            logic = null;
            pad = null;
        }
    }

    /// <summary>
    /// Writes the scaled standards (consumed by the game's own derivation on
    /// mode switches) and the derived axis speeds (for live config changes).
    /// The derived speed is recovered by matching the current speed against
    /// every standard pair the game could have written, which keeps an
    /// unchanged multiplier a no-op for both mouse and controller input.
    /// </summary>
    internal static void Apply()
    {
        try
        {
            if (!hasBaseline || !Resolve())
                return;

            var mult = Plugin.CameraSensitivityMultiplier.Value;
            logic!.m_axisXStandard_Tps = baseTpsX * mult;
            logic!.m_axisYStandard_Tps = baseTpsY * mult;
            logic!.m_Ctr_axisXStandard_Tps = baseCtrTpsX * mult;
            logic!.m_Ctr_axisYStandard_Tps = baseCtrTpsY * mult;

            pad!.axisX.speed = NativeSpeed(pad.axisX.speed, logic!.m_axisXStandard, logic!.m_axisXStandard_Tps, logic!.m_Ctr_axisXStandard, logic!.m_Ctr_axisXStandard_Tps);
            pad!.axisY.speed = NativeSpeed(pad.axisY.speed, logic!.m_axisYStandard, logic!.m_axisYStandard_Tps, logic!.m_Ctr_axisYStandard, logic!.m_Ctr_axisYStandard_Tps);
        }
        catch (Exception)
        {
            logic = null;
            pad = null;
        }
    }

    /// <summary>
    /// Restores the sampled sensitivity standards.
    /// </summary>
    internal static void Restore()
    {
        if (!hasBaseline)
            return;

        try
        {
            if (logic is null)
                return;

            logic.m_axisXStandard_Tps = baseTpsX;
            logic.m_axisYStandard_Tps = baseTpsY;
            logic.m_Ctr_axisXStandard_Tps = baseCtrTpsX;
            logic.m_Ctr_axisYStandard_Tps = baseCtrTpsY;
        }
        catch (Exception)
        {
            logic = null;
            pad = null;
        }
    }

    private static bool Resolve()
    {
        if (pad is not null && logic is not null)
            return true;

        logic = UnityEngine.Object.FindObjectOfType<JoystickTouch_logic>();
        if (logic is null)
            return false;

        pad = logic.GetComponent<ETCTouchPad>();
        return pad is not null;
    }

    private static float NativeSpeed(float current, float fps, float tps, float ctrFps, float ctrTps)
    {
        var ratio = logic!.m_axisRatioByOpenSight;
        if (logic!.UseFpsAxisSpeed)
        {
            tps = fps;
            ctrTps = ctrFps;
        }

        var best = fps;
        var bestDelta = Mathf.Abs(current - ratio * fps);
        TryCandidate(tps);
        TryCandidate(ctrFps);
        TryCandidate(ctrTps);
        return ratio * best;

        void TryCandidate(float candidate)
        {
            var d = Mathf.Abs(current - ratio * candidate);
            if (d < bestDelta)
            {
                best = candidate;
                bestDelta = d;
            }
        }
    }
}
