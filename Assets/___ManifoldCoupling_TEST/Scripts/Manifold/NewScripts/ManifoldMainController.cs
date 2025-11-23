// File: Assets/Scripts/Manifold/ManifoldMainController.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ManifoldMainController : MonoBehaviour
{
    [Header("Sub-controllers (assign in inspector)")]
    [SerializeField] private ManifoldPlacementRig _placementRig;
    [SerializeField] private ManifoldSnapController _snapController;      // if you have a snap controller attached under Transform_Offset
    [SerializeField] private ManifoldConnectorController _connector;      // optional, assign if present
    [SerializeField] private ManifoldHandleController _handleController;  // assign the handle controller

    public event Action OnSnapped;
    public event Action OnLocked;
    public event Action OnUnlocked;

    public ManifoldPlacementRig PlacementRig {  get { return _placementRig; } }
    public ManifoldConnectorController Connector { get { return _connector; } }
    public ManifoldSnapController SnapController { get { return _snapController; } }
    public ManifoldHandleController HandleController { get { return _handleController; } }

    private void Reset()
    {
        // Auto-find children when possible to speed up wiring
        if (_placementRig == null) _placementRig = GetComponentInChildren<ManifoldPlacementRig>(true);
        if (_snapController == null) _snapController = GetComponentInChildren<ManifoldSnapController>(true);
        if (_connector == null) _connector = GetComponentInChildren<ManifoldConnectorController>(true);
        if (_handleController == null) _handleController = GetComponentInChildren<ManifoldHandleController>(true);
    }

    private void OnEnable()
    {
        if (_snapController != null) _snapController.OnSnapStateChanged += HandleSnapped;
        if (_handleController != null)
        {
            //_handleController.OnLocked += HandleLocked;
            //_handleController.OnUnlocked += HandleUnlocked;
        }
    }

    private void OnDisable()
    {
        if (_snapController != null) _snapController.OnSnapStateChanged -= HandleSnapped;
        if (_handleController != null)
        {
            //_handleController.OnLocked -= HandleLocked;
            //_handleController.OnUnlocked -= HandleUnlocked;
        }
    }

    private void HandleSnapped(bool val)
    {
        if (!val) return;

        Debug.Log("[ManifoldMainController] Snap event.");
        //OnSnapped?.Invoke();
        // Allow handle control after snap
        //_handleController?.EnableHandle(true);
    }

    private void HandleLocked()
    {
        Debug.Log("[ManifoldMainController] Locked event.");
        OnLocked?.Invoke();
    }

    private void HandleUnlocked()
    {
        Debug.Log("[ManifoldMainController] Unlocked event.");
        OnUnlocked?.Invoke();
    }

    // Called by spawner after instantiate
    public void OnPlaced(Pose placementPose, Camera cam, float modelFacingOffsetY = 0f)
    {
        if (_placementRig != null)
            _placementRig.OnPlaced(placementPose, cam, modelFacingOffsetY);
    }

    #region Public API for UI/Input

    // Placement operations
    public void EnablePlacementMovement(bool enable) => _placementRig?.EnableMovement(enable); // enabled after instantiating at spawner
    public void NudgePlacementLocal(Vector3 localDirection) => _placementRig?.NudgeLocal(localDirection);
    public void NudgePlacementVertical(float sign) => _placementRig?.NudgeVertical(sign);
    public void RotatePlacementSmooth(float degreesPerSecond) => _placementRig?.RotateYawSmooth(degreesPerSecond);
    public void SnapPlacementYaw45() => _placementRig?.SnapYawTo45();

    // Connector 
    public void EnableConnectorControl(bool enable) => _connector?.EnableControl(enable); // enabled directly in game manager after state changes to tutorial

    public void NudgeConnectorLocal(Vector3 localDir) => _connector.NudgeLocal(localDir);

    public void RotateConnectorSmooth(float degsPerSecond) => _connector.RotateSmoothYaw(degsPerSecond);

    // SNAP

    public void ManualSnap()
    {
        _snapController?.ManualSnap();
        _connector.EnableControl(false);
    }
    public void ManualUnsnap()
    {
        _snapController?.ManualUnsnap();
        _connector.EnableControl(true);
    }

    public bool IsSnapped() => _snapController != null && _snapController.IsSnapped;

    // HANDLE

    public void RotateHandle(float degsPerSecond) => _handleController.RotateHold(degsPerSecond);

    public void ToggleHandleControl(bool toggle) => _handleController.EnableRotation(toggle); 

    // Reset utilities
    public void ResetAll()
    {
        _snapController.ResetSnap();
        _handleController.ResetHandle();
        _connector.ResetConnector();
    }

    #endregion
}
