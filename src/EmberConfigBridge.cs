using System;
using System.Runtime.CompilerServices;
using EmberConfig.Public;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// EmberConfig binding
/// </summary>
internal static class EmberConfigBridge
{
    private const string Tab = "Third Person";
    private const string ControlGroup = "Controls";
    private const string CameraGroup = "Camera";
    private const string AdvancedGroup = "Advanced";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void RegisterAll(TpsController controller)
    {
        RegisterEnabled();
        RegisterStrafeFacing();
        RegisterToggleKeybind(controller);
        RegisterCameraSettings();
        RegisterAdvancedSettings();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RegisterEnabled()
    {
        var entry = Plugin.Enabled;

        var options = new SettingOptions<bool>(
            Section: entry.Definition.Section,
            Key: entry.Definition.Key,
            DefaultValue: entry.Value,
            Description: entry.Description?.Description ?? "Whether third-person camera mode is active.",
            Label: "Third Person",
            Tab: Tab,
            Group: ControlGroup,
            ModName: MyPluginInfo.PLUGIN_NAME);

        SettingsMenu.Register(entry, options);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RegisterStrafeFacing()
    {
        var entry = Plugin.StrafeFacing;

        var options = new SettingOptions<bool>(
            Section: entry.Definition.Section,
            Key: entry.Definition.Key,
            DefaultValue: entry.Value,
            Description: entry.Description?.Description ?? "Switch between strafe movement and free movement.",
            Label: "Strafe movement",
            Tab: Tab,
            Group: ControlGroup,
            ModName: MyPluginInfo.PLUGIN_NAME);

        SettingsMenu.Register(entry, options);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RegisterToggleKeybind(TpsController controller)
    {
        var entry = Plugin.ToggleKey;

        var options = new KeybindOptions(
            Section: entry.Definition.Section,
            Key: entry.Definition.Key,
            DefaultPrimary: entry.Value,
            Description: entry.Description?.Description ?? "Key used to toggle third-person.",
            Label: "Toggle Third Person",
            Tab: Tab,
            Group: ControlGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            DefaultSecondary: Plugin.ToggleKeySecondary.Value,
            OnPressed: controller.Toggle);

        SettingsMenu.RegisterKeybind(entry, Plugin.ToggleKeySecondary, options);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RegisterCameraSettings()
    {
        SettingsMenu.Register(Plugin.CameraDistance, new SettingOptions<float>(
            Section: Plugin.CameraDistance.Definition.Section,
            Key: Plugin.CameraDistance.Definition.Key,
            DefaultValue: Plugin.CameraDistance.Value,
            Description: Plugin.CameraDistance.Description?.Description ?? "Distance of the third-person camera behind the player.",
            Label: "Camera Distance",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.CameraDistance.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.ShoulderOffsetX, new SettingOptions<float>(
            Section: Plugin.ShoulderOffsetX.Definition.Section,
            Key: Plugin.ShoulderOffsetX.Definition.Key,
            DefaultValue: Plugin.ShoulderOffsetX.Value,
            Description: Plugin.ShoulderOffsetX.Description?.Description ?? "Lateral camera offset.",
            Label: "Shoulder Offset",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.ShoulderOffsetX.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.CameraOffsetY, new SettingOptions<float>(
            Section: Plugin.CameraOffsetY.Definition.Section,
            Key: Plugin.CameraOffsetY.Definition.Key,
            DefaultValue: Plugin.CameraOffsetY.Value,
            Description: Plugin.CameraOffsetY.Description?.Description ?? "Vertical camera offset.",
            Label: "Height Offset",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.CameraOffsetY.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.CameraOffsetZ, new SettingOptions<float>(
            Section: Plugin.CameraOffsetZ.Definition.Section,
            Key: Plugin.CameraOffsetZ.Definition.Key,
            DefaultValue: Plugin.CameraOffsetZ.Value,
            Description: Plugin.CameraOffsetZ.Description?.Description ?? "Forward/back camera offset.",
            Label: "Depth Offset",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.CameraOffsetZ.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.CollisionProbeSize, new SettingOptions<float>(
            Section: Plugin.CollisionProbeSize.Definition.Section,
            Key: Plugin.CollisionProbeSize.Definition.Key,
            DefaultValue: Plugin.CollisionProbeSize.Value,
            Description: Plugin.CollisionProbeSize.Description?.Description ?? "Camera collision distance.",
            Label: "Collision Radius",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.CollisionProbeSize.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.CameraSensitivityMultiplier, new SettingOptions<float>(
            Section: Plugin.CameraSensitivityMultiplier.Definition.Section,
            Key: Plugin.CameraSensitivityMultiplier.Definition.Key,
            DefaultValue: Plugin.CameraSensitivityMultiplier.Value,
            Description: Plugin.CameraSensitivityMultiplier.Description?.Description ?? "Multiplier applied to the third-person camera sensitivity.",
            Label: "Camera Sensitivity Multiplier",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.CameraSensitivityMultiplier.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.PitchMin, new SettingOptions<float>(
            Section: Plugin.PitchMin.Definition.Section,
            Key: Plugin.PitchMin.Definition.Key,
            DefaultValue: Plugin.PitchMin.Value,
            Description: Plugin.PitchMin.Description?.Description ?? "Minimum camera pitch angle. How far you can look up.",
            Label: "Min Pitch",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.PitchMin.Description?.AcceptableValues));

        SettingsMenu.Register(Plugin.PitchMax, new SettingOptions<float>(
            Section: Plugin.PitchMax.Definition.Section,
            Key: Plugin.PitchMax.Definition.Key,
            DefaultValue: Plugin.PitchMax.Value,
            Description: Plugin.PitchMax.Description?.Description ?? "Maximum camera pitch angle. How far you can look down.",
            Label: "Max Pitch",
            Tab: Tab,
            Group: CameraGroup,
            ModName: MyPluginInfo.PLUGIN_NAME,
            AcceptableValues: Plugin.PitchMax.Description?.AcceptableValues));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RegisterAdvancedSettings()
    {
        SettingsMenu.Register(Plugin.HideFirstPersonWeapon, new SettingOptions<bool>(
            Section: Plugin.HideFirstPersonWeapon.Definition.Section,
            Key: Plugin.HideFirstPersonWeapon.Definition.Key,
            DefaultValue: Plugin.HideFirstPersonWeapon.Value,
            Description: Plugin.HideFirstPersonWeapon.Description?.Description ?? "Force-disable the first-person weapon camera in third person if necessary.",
            Label: "Hide FP Weapon",
            Tab: Tab,
            Group: AdvancedGroup,
            ModName: MyPluginInfo.PLUGIN_NAME));
    }
}
