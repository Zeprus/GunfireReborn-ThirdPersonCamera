using System;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Fix third-person animations.
/// </summary>
internal static class MoveAnimDriver
{
    private static readonly int HashX = Animator.StringToHash("MoveDirX");
    private static readonly int HashY = Animator.StringToHash("MoveDirY");
    private static readonly int HashIsMove = Animator.StringToHash("IsMove");

    private static Animator? animator;

    internal static void Bind(Transform? heroTran)
    {
        animator = heroTran?.Find("TPSModel")?.GetComponent<Animator>();
    }

    internal static void Clear()
    {
        animator = null;
    }

    /// <summary>
    /// Feed correct movement data to the animation manager.
    /// </summary>
    internal static void Apply()
    {
        var anim = animator;
        if (anim is null)
            return;

        try
        {
            var dir = CharacterMotionManager.HMData?.Direction ?? Vector3.zero;
            float mag = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);

            anim.SetFloat(HashX, mag > 0.01f ? dir.x / mag : 0f);
            anim.SetFloat(HashY, mag > 0.01f ? dir.z / mag : 0f);
            anim.SetBool(HashIsMove, mag > 0.01f);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"MoveAnimDriver failed: {ex.Message}");
            animator = null;
        }
    }
}
