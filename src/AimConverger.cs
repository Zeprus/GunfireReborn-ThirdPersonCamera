using System.Linq;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Aligns the FPS aim camera with the third-person crosshair direction.
/// </summary>
internal static class AimConverger
{
    private static readonly int SightMask = Layer.Mask.AimatMask | (1 << Layer.Monster);
    private const float MaxDistance = 500f;

    /// <summary>
    /// Aims the FPS camera at the point under the TPS crosshair.
    /// </summary>
    internal static void Apply(Transform? fpsCam, Transform? tpsCam)
    {
        if (fpsCam is null || tpsCam is null)
            return;

        var origin = tpsCam.position;
        var forward = tpsCam.forward;
        var target = origin + forward * MaxDistance;

        var hits = Physics.RaycastAll(origin, forward, MaxDistance, SightMask);
        if (hits.Length > 0)
        {
            hits = hits.OrderBy(h => h.distance).ToArray();
            var first = hits[0];
            target = first.point;

            if (first.collider.gameObject.layer == Layer.AimatCollider)
            {
                target = first.collider.bounds.center;
                var root = first.collider.transform.root;
                for (var i = 1; i < hits.Length; i++)
                {
                    var nextLayer = hits[i].collider.gameObject.layer;
                    if (nextLayer == Layer.AimatCollider)
                        continue;
                    if (hits[i].collider.transform.root != root)
                        break;
                    target = hits[i].point;
                    break;
                }
            }
        }

        fpsCam.rotation = Quaternion.LookRotation(target - fpsCam.position);
    }
}
