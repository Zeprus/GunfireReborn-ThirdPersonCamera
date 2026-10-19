using System;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Derives third-person look sensitivity from the FPS standards times the configured multiplier.
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
    private static float baseFpsX;
    private static float baseFpsY;
    private static float baseCtrFpsX;
    private static float baseCtrFpsY;
    private static float baseSpeedX;
    private static float baseSpeedY;

    /// <summary>
    /// Samples the live TPS sensitivity standards while the mod is not touching them.
    /// </summary>
    internal static void CaptureBaseline()
    {
        try
        {
            if (!Resolve())
                return;

            var speedX = pad!.axisX.speed;
            var speedY = pad.axisY.speed;
            if (speedX <= 0f || speedY <= 0f)
                return;

            baseTpsX = logic!.m_axisXStandard_Tps;
            baseTpsY = logic!.m_axisYStandard_Tps;
            baseCtrTpsX = logic!.m_Ctr_axisXStandard_Tps;
            baseCtrTpsY = logic!.m_Ctr_axisYStandard_Tps;
            baseFpsX = logic!.m_axisXStandard;
            baseFpsY = logic!.m_axisYStandard;
            baseCtrFpsX = logic!.m_Ctr_axisXStandard;
            baseCtrFpsY = logic!.m_Ctr_axisYStandard;
            baseSpeedX = speedX;
            baseSpeedY = speedY;
            hasBaseline = true;
        }
        catch (Exception)
        {
            logic = null;
            pad = null;
        }
    }

    /// <summary>
    /// Writes the FPS-derived TPS standards and the derived axis speeds each frame.
    /// </summary>
    internal static void Apply()
    {
        try
        {
            if (!hasBaseline)
                CaptureBaseline();
            if (!hasBaseline || !Resolve())
                return;

            var mult = Plugin.CameraSensitivityMultiplier.Value;
            logic!.m_axisXStandard_Tps = baseFpsX * mult;
            logic!.m_axisYStandard_Tps = baseFpsY * mult;
            logic!.m_Ctr_axisXStandard_Tps = baseCtrFpsX * mult;
            logic!.m_Ctr_axisYStandard_Tps = baseCtrFpsY * mult;

            pad!.axisX.speed = NativeSpeed(pad.axisX.speed, baseFpsX * mult, baseCtrFpsX * mult);
            pad!.axisY.speed = NativeSpeed(pad.axisY.speed, baseFpsY * mult, baseCtrFpsY * mult);
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
            if (pad is not null)
            {
                pad.axisX.speed = baseSpeedX;
                pad.axisY.speed = baseSpeedY;
            }
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

    /// <summary>
    /// Returns the derived axis speed for the live standard closest to the current speed.
    /// </summary>
    private static float NativeSpeed(float current, float std, float ctrStd)
    {
        var ratio = logic!.m_axisRatioByOpenSight;
        return ratio * (Mathf.Abs(current - ratio * std) <= Mathf.Abs(current - ratio * ctrStd) ? std : ctrStd);
    }
}
