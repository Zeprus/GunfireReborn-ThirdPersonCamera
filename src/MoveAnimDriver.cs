using System;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Feeds directional locomotion params to the local player's TPS animator.
/// </summary>
internal static class MoveAnimDriver
{
    private const float DampRate = 12f;

    private static Animator? animator;
    private static float curX;
    private static float curY;

    /// <summary>
    /// Set by the session while the game owns the camera.
    /// </summary>
    internal static bool Suspended;

    /// <summary>
    /// Binds to the hero's TPS animator.
    /// </summary>
    internal static void Bind(NewPlayerObject? hero)
    {
        try
        {
            animator = hero?.PlayerCom?.TPSAnimator;
        }
        catch (Exception)
        {
            animator = null;
        }
    }

    /// <summary>
    /// Drops the animator binding and resets damped state.
    /// </summary>
    internal static void Clear()
    {
        animator = null;
        Suspended = false;
        curX = 0f;
        curY = 0f;
    }

    /// <summary>
    /// Feed correct movement data to the animation manager.
    /// </summary>
    internal static void Apply()
    {
        var anim = animator;
        if (anim is null || Suspended)
            return;

        try
        {
            var dir = CharacterMotionManager.HMData?.Direction ?? Vector3.zero;
            float mag = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);

            var k = 1f - Mathf.Exp(-DampRate * Time.deltaTime);
            curX = Mathf.Lerp(curX, mag > 0.01f ? dir.x / mag : 0f, k);
            curY = Mathf.Lerp(curY, mag > 0.01f ? dir.z / mag : 0f, k);

            AnimatorInterface.SetFloatWithAnimator(anim, Game.AnimatorEvent.MoveDirX, curX);
            AnimatorInterface.SetFloatWithAnimator(anim, Game.AnimatorEvent.MoveDirY, curY);
            AnimatorInterface.SetBoolWithAnimator(anim, Game.AnimatorEvent.IsMove, mag > 0.01f);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"MoveAnimDriver failed: {ex.Message}");
            animator = null;
        }
    }
}
