using System;
using HeroCameraName;
using UnityEngine;

namespace ThirdPersonCamera;

/// <summary>
/// Manages the games native third-person rig.
/// </summary>
internal sealed class HeroRig
{
    private GameObject? tpsCamBoomGo;
    private GameObject? tpsModelGo;
    private GameObject? npcTriggerGo;
    private GameObject? fpsCamGo;
    private Camera? weaponCam;
    private bool weaponCamDisabledByUs;

    internal CameraBoom? Boom { get; private set; }
    internal Transform? TpsCamTransform { get; private set; }
    internal Transform? FpsCamTransform { get; private set; }

    internal bool IsBoomActive => tpsCamBoomGo is not null && tpsCamBoomGo.activeInHierarchy;

    /// <summary>
    /// Resolves the rig objects under the hero transform.
    /// </summary>
    internal void Bind(Transform? heroTran)
    {
        if (heroTran is null)
            return;

        tpsCamBoomGo = heroTran.Find("TPSCamBoom")?.gameObject;
        tpsModelGo = heroTran.Find("TPSModel")?.gameObject;
        npcTriggerGo = heroTran.Find("NpcTrigger")?.gameObject;
        fpsCamGo = heroTran.Find("FPSCam")?.gameObject;

        Boom = tpsCamBoomGo?.GetComponent<CameraBoom>();
        TpsCamTransform = tpsCamBoomGo?.transform.Find("TPSCam");
        FpsCamTransform = fpsCamGo?.transform;

        try
        {
            weaponCam = CameraManager.WeaponCameraCom;
        }
        catch (Exception)
        {
            weaponCam = fpsCamGo?.transform.Find("WeaponCam")?.GetComponent<Camera>();
        }

        if (tpsCamBoomGo is null || tpsModelGo is null)
        {
            Plugin.Log?.LogWarning("TPS rig objects not found on hero; third person cannot activate.");
        }
    }

    internal void Clear()
    {
        tpsCamBoomGo = null;
        tpsModelGo = null;
        npcTriggerGo = null;
        fpsCamGo = null;
        Boom = null;
        TpsCamTransform = null;
        FpsCamTransform = null;
        weaponCam = null;
        weaponCamDisabledByUs = false;
    }

    /// <summary>
    /// Last-resort rig shutdown when the native FPS transition throws.
    /// </summary>
    internal void Deactivate()
    {
        try
        {
            if (tpsCamBoomGo is not null && tpsCamBoomGo.activeSelf)
                tpsCamBoomGo.SetActive(false);
            if (tpsModelGo is not null && tpsModelGo.activeSelf)
                tpsModelGo.SetActive(false);
            if (npcTriggerGo is not null && npcTriggerGo.activeSelf)
                npcTriggerGo.SetActive(false);

            if (fpsCamGo is not null && !fpsCamGo.activeSelf)
                fpsCamGo.SetActive(true);

            try
            {
                CameraManager.CurCameraMode = CameraMode.FPS;
            }
            catch (Exception)
            {
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Fallback TPS deactivation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies <see cref="Plugin.HideFirstPersonWeapon"/>: disables the
    /// first-person weapon viewmodel camera while third person is active and
    /// re-enables it otherwise.
    /// </summary>
    internal void ApplyWeaponCamState(bool tpsActive)
    {
        if (weaponCam is null)
            return;

        bool shouldHide = tpsActive && Plugin.HideFirstPersonWeapon.Value;
        if (shouldHide && weaponCam.enabled)
        {
            weaponCam.enabled = false;
            weaponCamDisabledByUs = true;
        }
        else if (!shouldHide && weaponCamDisabledByUs && !weaponCam.enabled)
        {
            weaponCam.enabled = true;
            weaponCamDisabledByUs = false;
        }
    }

    internal void RestoreWeaponCam()
    {
        if (weaponCam is not null && weaponCamDisabledByUs)
        {
            weaponCam.enabled = true;
            weaponCamDisabledByUs = false;
        }
    }
}
