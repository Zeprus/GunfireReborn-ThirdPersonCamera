using System;
using UnityEngine;

namespace ThirdPersonCam;

/// <summary>
/// Unity Component for third-person.
/// </summary>
public class TpsController : MonoBehaviour
{
    private readonly TpsSession session = new TpsSession();
    private int lastToggleFrame = -1;

    public TpsController(IntPtr ptr) : base(ptr)
    {
    }

    /// <summary>
    /// Toggles the <see cref="Plugin.Enabled"/> config value.
    /// </summary>
    public void Toggle()
    {
        if (Time.frameCount == lastToggleFrame)
            return;

        lastToggleFrame = Time.frameCount;
        Plugin.Enabled.Value = !Plugin.Enabled.Value;
        Plugin.Log?.LogInfo($"Third person {(Plugin.Enabled.Value ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Flags live config values for re-application on the next frame.
    /// </summary>
    internal void NotifyConfigChanged() => session.MarkConfigDirty();

    private void Update()
    {
        if (!Plugin.EmberConfigPresent &&
            (Input.GetKeyDown(Plugin.ToggleKey.Value) || Input.GetKeyDown(Plugin.ToggleKeySecondary.Value)))
        {
            Toggle();
        }

        session.OnUpdate();
    }

    private void LateUpdate() => session.OnLateUpdate();
}
