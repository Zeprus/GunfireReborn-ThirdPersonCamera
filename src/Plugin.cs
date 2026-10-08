using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ThirdPersonCamera;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(EmberConfigGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    internal const string EmberConfigGuid = "zeprus.gunfire.EmberConfig";

    internal static new ManualLogSource Log = null!;
    internal static bool EmberConfigPresent;

    internal static ConfigEntry<bool> Enabled { get; private set; } = null!;
    internal static ConfigEntry<bool> StrafeFacing { get; private set; } = null!;
    internal static ConfigEntry<KeyCode> ToggleKey { get; private set; } = null!;
    internal static ConfigEntry<KeyCode> ToggleKeySecondary { get; private set; } = null!;
    internal static ConfigEntry<float> CameraDistance { get; private set; } = null!;
    internal static ConfigEntry<float> ShoulderOffsetX { get; private set; } = null!;
    internal static ConfigEntry<float> CameraOffsetY { get; private set; } = null!;
    internal static ConfigEntry<float> CameraOffsetZ { get; private set; } = null!;
    internal static ConfigEntry<float> CollisionProbeSize { get; private set; } = null!;
    internal static ConfigEntry<float> PitchMin { get; private set; } = null!;
    internal static ConfigEntry<float> PitchMax { get; private set; } = null!;
    internal static ConfigEntry<bool> HideFirstPersonWeapon { get; private set; } = null!;

    public override void Load()
    {
        Log = base.Log;

        BindConfiguration();

        ClassInjector.RegisterTypeInIl2Cpp<TpsController>();
        var controller = AddComponent<TpsController>();

        Config.SettingChanged += (_, _) => controller.NotifyConfigChanged();

        EmberConfigPresent = IL2CPPChainloader.Instance?.Plugins.ContainsKey(EmberConfigGuid) ?? false;

        if (EmberConfigPresent)
        {
            InitializeEmberConfig(controller);
        }
        else
        {
            Log.LogInfo("EmberConfig not found; Third Person Camera will use BepInEx config fallback.");
        }

        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} loaded.");
    }

    private void BindConfiguration()
    {
        Enabled = Config.Bind(
            "Third Person",
            "Enabled",
            false,
            "Whether third-person camera mode is active.");

        StrafeFacing = Config.Bind(
            "Third Person",
            "StrafeFacing",
            true,
            "Switch between strafe movement and free movement.");

        ToggleKey = Config.Bind(
            "Third Person",
            "ToggleKey",
            KeyCode.F6,
            "Key used to toggle third-person.");

        ToggleKeySecondary = Config.Bind(
            "Third Person",
            "ToggleKeySecondary",
            KeyCode.None,
            "Secondary key used to toggle third-person.");

        CameraDistance = Config.Bind(
            "Camera",
            "CameraDistance",
            2.5f,
            new ConfigDescription(
                "Distance of the third-person camera behind the player.",
                new AcceptableValueRange<float>(0.5f, 8f)));

        ShoulderOffsetX = Config.Bind(
            "Camera",
            "ShoulderOffsetX",
            1.0f,
            new ConfigDescription(
                "Lateral camera offset. Positive values look over the right shoulder.",
                new AcceptableValueRange<float>(-2f, 2f)));

        CameraOffsetY = Config.Bind(
            "Camera",
            "CameraOffsetY",
            0f,
            new ConfigDescription(
                "Vertical camera offset relative to the boom pivot.",
                new AcceptableValueRange<float>(-1.5f, 1.5f)));

        CameraOffsetZ = Config.Bind(
            "Camera",
            "CameraOffsetZ",
            0f,
            new ConfigDescription(
                "Forward/back camera offset relative to the boom pivot.",
                new AcceptableValueRange<float>(-1.5f, 1.5f)));

        CollisionProbeSize = Config.Bind(
            "Camera",
            "CollisionProbeSize",
            0.2f,
            new ConfigDescription(
                "Camera collision distance.",
                new AcceptableValueRange<float>(0f, 1f)));

        PitchMin = Config.Bind(
            "Camera",
            "PitchMin",
            -89f,
            new ConfigDescription(
                "Minimum camera pitch angle. How far you can look up.",
                new AcceptableValueRange<float>(-89f, 0f)));

        PitchMax = Config.Bind(
            "Camera",
            "PitchMax",
            89f,
            new ConfigDescription(
                "Maximum camera pitch angle. How far you can look down.",
                new AcceptableValueRange<float>(0f, 89f)));

        HideFirstPersonWeapon = Config.Bind(
            "Third Person",
            "HideFirstPersonWeapon",
            false,
            "Force-disable the first-person weapon camera in third person if necessary.");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void InitializeEmberConfig(TpsController controller)
    {
        EmberConfigBridge.RegisterAll(controller);
    }
}
