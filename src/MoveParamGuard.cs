using HarmonyLib;

namespace ThirdPersonCamera;

/// <summary>
/// Apply Harmony patches to fix third-person animations.
/// </summary>
[HarmonyPatch]
internal static class MoveParamGuard
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterMotionManager), "Update")]
    private static void AfterMotionUpdate() => MoveAnimDriver.Apply();
}
