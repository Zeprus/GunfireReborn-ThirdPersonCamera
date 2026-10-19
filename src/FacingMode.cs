using System;
using HeroCameraName;

namespace ThirdPersonCamera;

/// <summary>
/// Applies the configured movement-facing mode onto the character's rotation flags.
/// </summary>
internal static class FacingMode
{
    /// <summary>
    /// Pushes the facing flags for the requested mode.
    /// </summary>
    internal static void Apply(bool strafe)
    {
        ETCTransformCtrl? ctrl;
        try
        {
            ctrl = ETCTransformCtrl.CharacterETCCtrl;
        }
        catch (Exception)
        {
            return;
        }
        if (ctrl is null)
            return;

        try
        {
            if (ctrl.UseControllerRotationYaw != strafe)
                ctrl.UseControllerRotationYaw = strafe;
            if (ctrl.OrientRotationToMovement == strafe)
                ctrl.OrientRotationToMovement = !strafe;
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Restores the FPS Camera.
    /// </summary>
    internal static void Restore()
    {
        try
        {
            var ctrl = ETCTransformCtrl.CharacterETCCtrl;
            if (ctrl is null)
                return;
            ctrl.UseControllerRotationYaw = true;
            ctrl.OrientRotationToMovement = false;
        }
        catch (Exception)
        {
        }
    }
}
