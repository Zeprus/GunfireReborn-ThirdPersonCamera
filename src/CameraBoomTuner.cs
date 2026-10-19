using System;
using System.Collections.Generic;
using HeroCameraName;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Writes the mod's camera settings onto the active <see cref="CameraBoomConfig"/>.
/// </summary>
internal sealed class CameraBoomTuner
{
    private struct Snapshot
    {
        public Vector3 TargetOffset;
        public Vector3 SocketOffset;
        public float TargetArmLength;
        public float MaxArmLength;
        public float ProbeSize;
        public float MinAngleX;
        public float MaxAngleX;
        public float FocusSpeed;
        public bool UseDefaultAngleX;
        public bool DoCollisionTest;
    }

    private readonly Dictionary<IntPtr, (CameraBoomConfig config, Snapshot snapshot)> snapshots = new();
    private float offsetBlend;

    /// <summary>
    /// Writes the mod's camera settings onto the active config each frame.
    /// </summary>
    internal void Apply(CameraBoom? boom, NormalCS? normalCs, bool skipArm = false, bool exitBlend = false)
    {
        var config = ResolveConfig(boom);
        if (config is null)
            return;

        var changing = false;
        try
        {
            if (normalCs is { isChanging: true })
                changing = true;
        }
        catch (Exception)
        {
        }
        var nativeBlendActive = skipArm || changing;

        var targetBlend = exitBlend ? 0f : 1f;
        var tt = Plugin.CameraTransitionTime.Value;
        offsetBlend = tt <= 0f
            ? targetBlend
            : Mathf.MoveTowards(offsetBlend, targetBlend, Time.deltaTime / tt);

        var ptr = config.Pointer;
        if (!snapshots.TryGetValue(ptr, out var entry))
        {
            entry = (config, new Snapshot
            {
                TargetOffset = config.TargetOffset,
                SocketOffset = config.SocketOffset,
                TargetArmLength = config.TargetArmLength,
                MaxArmLength = config.MaxArmLength,
                ProbeSize = config.ProbeSize,
                MinAngleX = config.MinAngleX,
                MaxAngleX = config.MaxAngleX,
                FocusSpeed = config.FocusSpeed,
                UseDefaultAngleX = config.UseDefaultAngleX,
                DoCollisionTest = config.DoCollisionTest,
            });
            snapshots[ptr] = entry;
        }

        if (skipArm && !changing)
            config.TargetArmLength = 0f;
        else if (!nativeBlendActive)
            config.TargetArmLength = Plugin.CameraDistance.Value;
        config.MaxArmLength = Plugin.CameraDistance.Value;
        config.SocketOffset = new Vector3(Plugin.ShoulderOffsetX.Value, Plugin.CameraOffsetY.Value, Plugin.CameraOffsetZ.Value) * offsetBlend;
        config.ProbeSize = Plugin.CollisionProbeSize.Value;
        config.MinAngleX = Mathf.Abs(Plugin.PitchMin.Value);
        config.MaxAngleX = Mathf.Abs(Plugin.PitchMax.Value);
        config.UseDefaultAngleX = false;
        config.FocusSpeed = 0f;
    }

    /// <summary>
    /// Restores every snapshotted config to its native values.
    /// </summary>
    internal void Restore()
    {
        foreach (var (config, snapshot) in snapshots.Values)
        {
            try
            {
                config.TargetOffset = snapshot.TargetOffset;
                config.SocketOffset = snapshot.SocketOffset;
                config.TargetArmLength = snapshot.TargetArmLength;
                config.MaxArmLength = snapshot.MaxArmLength;
                config.ProbeSize = snapshot.ProbeSize;
                config.MinAngleX = snapshot.MinAngleX;
                config.MaxAngleX = snapshot.MaxAngleX;
                config.FocusSpeed = snapshot.FocusSpeed;
                config.UseDefaultAngleX = snapshot.UseDefaultAngleX;
                config.DoCollisionTest = snapshot.DoCollisionTest;
            }
            catch (Exception)
            {
            }
        }

        snapshots.Clear();
        offsetBlend = 0f;
    }

    /// <summary>
    /// Returns the boom's active per-mode config.
    /// </summary>
    private static CameraBoomConfig? ResolveConfig(CameraBoom? boom)
    {
        if (boom is null)
            return null;

        try
        {
            return boom.CurrentCameraBoomConfig;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
