using System;
using HeroCameraName;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Applies the mod's camera settings onto the hero's active
/// <see cref="CameraBoomConfig"/>
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
        public bool DoCollisionTest;
    }

    private bool hasSnapshot;
    private IntPtr boomPtr;
    private Snapshot snapshot;

    /// <summary>
    /// Writes the mod's camera settings onto <paramref name="boom"/>'s active
    /// config every frame
    /// </summary>
    internal void Apply(CameraBoom? boom)
    {
        var ptr = boom?.Pointer ?? IntPtr.Zero;
        if (ptr != boomPtr)
        {
            // boom object was recreated, delete old snapshot
            boomPtr = ptr;
            hasSnapshot = false;
        }

        var config = ResolveConfig(boom);
        if (config is null)
            return;

        if (!hasSnapshot)
        {
            snapshot = new Snapshot
            {
                TargetOffset = config.TargetOffset,
                SocketOffset = config.SocketOffset,
                TargetArmLength = config.TargetArmLength,
                MaxArmLength = config.MaxArmLength,
                ProbeSize = config.ProbeSize,
                MinAngleX = config.MinAngleX,
                MaxAngleX = config.MaxAngleX,
                FocusSpeed = config.FocusSpeed,
                DoCollisionTest = config.DoCollisionTest,
            };
            hasSnapshot = true;
        }

        config.TargetArmLength = Plugin.CameraDistance.Value;
        config.MaxArmLength = Mathf.Max(Plugin.CameraDistance.Value, snapshot.MaxArmLength);
        config.SocketOffset = new Vector3(Plugin.ShoulderOffsetX.Value, Plugin.CameraOffsetY.Value, Plugin.CameraOffsetZ.Value);
        config.ProbeSize = Plugin.CollisionProbeSize.Value;
        config.MinAngleX = Plugin.PitchMin.Value;
        config.MaxAngleX = Plugin.PitchMax.Value;
        // avoid yaw drift
        config.FocusSpeed = 0f;
    }

    /// <summary>
    /// Restores the snapshotted values onto <paramref name="boom"/>'s active config
    /// </summary>
    internal void Restore(CameraBoom? boom)
    {
        if (!hasSnapshot)
            return;

        var config = ResolveConfig(boom);
        if (config is null)
            return;

        config.TargetOffset = snapshot.TargetOffset;
        config.SocketOffset = snapshot.SocketOffset;
        config.TargetArmLength = snapshot.TargetArmLength;
        config.MaxArmLength = snapshot.MaxArmLength;
        config.ProbeSize = snapshot.ProbeSize;
        config.MinAngleX = snapshot.MinAngleX;
        config.MaxAngleX = snapshot.MaxAngleX;
        config.FocusSpeed = snapshot.FocusSpeed;
        config.DoCollisionTest = snapshot.DoCollisionTest;
    }

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
