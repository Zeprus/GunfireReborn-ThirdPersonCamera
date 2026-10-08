using UnityEngine;

namespace ThirdPersonCam;

/// <summary>
/// Applies the configured pitch limits to the controller's look-pitch controller.
/// <para>
/// </summary>
internal static class LookLimits
{
    private static ETCTransformCtrl? pitchCtrl;
    private static float baseMin;
    private static float baseMax;
    private static bool hasBaseline;
    private static bool applied;

    internal static void CaptureBaseline()
    {
        var etc = Resolve();
        if (etc is null || !etc.GetAngleLimitX(out var min, out var max))
            return;

        baseMin = min;
        baseMax = max;
        hasBaseline = true;
    }

    internal static void Apply()
    {
        var etc = Resolve();
        if (etc is null)
            return;

        etc.SetAngleLimitX(
            Mathf.Abs(Plugin.PitchMin.Value),
            Mathf.Abs(Plugin.PitchMax.Value));
        applied = true;
    }

    internal static void Restore()
    {
        if (!applied)
            return;
        applied = false;

        var etc = Resolve();
        if (etc is not null && hasBaseline)
            etc.SetAngleLimitX(baseMin, baseMax);
    }

    private static ETCTransformCtrl? Resolve()
    {
        if (pitchCtrl is not null)
            return pitchCtrl;

        var go = GameObject.Find("PlayerControllerPitch/PlayerController");
        pitchCtrl = go?.GetComponent<ETCTransformCtrl>();
        return pitchCtrl;
    }
}
