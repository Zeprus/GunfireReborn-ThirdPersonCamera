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

    internal CameraBoom? Boom { get; private set; }
    internal NormalCS? NormalCs { get; private set; }
    internal Transform? TpsCamTransform { get; private set; }
    internal Transform? FpsCamTransform { get; private set; }

    internal bool IsBoomActive => tpsCamBoomGo is not null && tpsCamBoomGo.activeInHierarchy;

    /// <summary>
    /// Resolves the native TPS boom, model, and camera references for the hero.
    /// </summary>
    internal void Bind(NewPlayerObject? hero)
    {
        Clear();

        var heroTran = hero?.gameTrans;
        try
        {
            NormalCs = HeroCameraManager.CSObject?.TryCast<NormalCS>();
            Boom = NormalCs?.tpsCamBoom;
        }
        catch (Exception)
        {
            NormalCs = null;
            Boom = null;
        }
        tpsCamBoomGo = Boom?.gameObject;
        if (tpsCamBoomGo is null)
        {
            tpsCamBoomGo = heroTran?.Find("TPSCamBoom")?.gameObject;
            Boom = tpsCamBoomGo?.GetComponent<CameraBoom>();
        }

        try
        {
            tpsModelGo = hero?.PlayerCom?.TPSAnimator?.gameObject;
        }
        catch (Exception)
        {
            tpsModelGo = null;
        }
        tpsModelGo ??= heroTran?.Find("TPSModel")?.gameObject;

        EnsureCameras();

        if (tpsCamBoomGo is null || tpsModelGo is null)
        {
            Plugin.Log?.LogWarning("TPS rig objects not found on hero; third person cannot activate.");
        }
    }

    /// <summary>
    /// Re-resolves the camera transforms while unset — the TPS camera only exists after the first native transition.
    /// </summary>
    internal void EnsureCameras()
    {
        if (TpsCamTransform is not null && FpsCamTransform is not null)
            return;
        try
        {
            TpsCamTransform ??= CameraManager.TpsCamera;
            FpsCamTransform ??= CameraManager.MainCamera;
        }
        catch (Exception)
        {
        }
    }

    internal void Clear()
    {
        tpsCamBoomGo = null;
        tpsModelGo = null;
        Boom = null;
        NormalCs = null;
        TpsCamTransform = null;
        FpsCamTransform = null;
    }

    /// <summary>
    /// Fallback rig shutdown mirroring <c>NormalCS.SetFpsState</c>.
    /// </summary>
    internal void Deactivate()
    {
        try
        {
            if (tpsCamBoomGo is not null && tpsCamBoomGo.activeSelf)
                tpsCamBoomGo.SetActive(false);
            if (tpsModelGo is not null && tpsModelGo.activeSelf)
                tpsModelGo.SetActive(false);

            try
            {
                if (CameraManager.MainCameraCom is { } mainCam)
                    mainCam.enabled = true;
                if (CameraManager.WeaponCameraCom is { } weaponCam)
                    weaponCam.enabled = true;
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
}
