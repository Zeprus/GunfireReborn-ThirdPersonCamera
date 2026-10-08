using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Transforms aim-ray for the third-person crosshair.
/// </summary>
internal static class AimConverger
{
    private const int SightMask = 1073813505;
    private const float MaxDistance = 500f;

    internal static void Apply(Transform? fpsCam, Transform? tpsCam)
    {
        if (fpsCam is null || tpsCam is null)
            return;

        var origin = tpsCam.position;
        var forward = tpsCam.forward;
        var target = Physics.Raycast(origin, forward, out var hit, MaxDistance, SightMask)
            ? hit.point
            : origin + forward * MaxDistance;

        fpsCam.rotation = Quaternion.LookRotation(target - fpsCam.position);
    }
}
