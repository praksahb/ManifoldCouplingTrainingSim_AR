using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Single bridge that maps UIHoldButton holdAction/releaseAction to the active ManifoldController_Root.
/// Only the bridge is on the Canvas and receives button references.
/// </summary>
public class UIManifoldBridge : MonoBehaviour
{
    [Header("Runtime manifold (set by spawner)")]
    //[SerializeField] private ManifoldController_Root _manifold;

    [Header("Movement hold buttons (assign UIHoldButton components)")]
    public UIHoldButton btnMoveF, btnMoveB, btnMoveL, btnMoveR, btnMoveU, btnMoveD;
    public UIHoldButton btnRotLeft, btnRotRight;
    public Button btnManualSnap, btnConfirmPlacement;

    private bool _rotLeftHeld = false;
    private bool _rotRightHeld = false;

    //void Start()
    //{
    //    // wire hold actions for movement
    //    if (btnMoveF != null) { btnMoveF.holdAction = () => _manifold?.StartMoveForward(); btnMoveF.releaseAction = () => _manifold?.StopMoveForward(); }
    //    if (btnMoveB != null) { btnMoveB.holdAction = () => _manifold?.StartMoveBack(); btnMoveB.releaseAction = () => _manifold?.StopMoveBack(); }
    //    if (btnMoveL != null) { btnMoveL.holdAction = () => _manifold?.StartMoveLeft(); btnMoveL.releaseAction = () => _manifold?.StopMoveLeft(); }
    //    if (btnMoveR != null) { btnMoveR.holdAction = () => _manifold?.StartMoveRight(); btnMoveR.releaseAction = () => _manifold?.StopMoveRight(); }
    //    if (btnMoveU != null) { btnMoveU.holdAction = () => _manifold?.StartMoveUp(); btnMoveU.releaseAction = () => _manifold?.StopMoveUp(); }
    //    if (btnMoveD != null) { btnMoveD.holdAction = () => _manifold?.StartMoveDown(); btnMoveD.releaseAction = () => _manifold?.StopMoveDown(); }

    //    // rotation buttons: start/stop and per-frame rotate via Update
    //    if (btnRotLeft != null) { btnRotLeft.holdAction = () => { _manifold?.StartRotateLeft(); _rotLeftHeld = true; }; btnRotLeft.releaseAction = () => { _manifold?.StopRotateLeft(); _rotLeftHeld = false; }; }
    //    if (btnRotRight != null) { btnRotRight.holdAction = () => { _manifold?.StartRotateRight(); _rotRightHeld = true; }; btnRotRight.releaseAction = () => { _manifold?.StopRotateRight(); _rotRightHeld = false; }; }

    //    if (btnManualSnap != null) btnManualSnap.onClick.AddListener(() => _manifold?.ManualSnap());
    //    if (btnConfirmPlacement != null) btnConfirmPlacement.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.TutorialMode); });
    //}

    //void Update()
    //{
    //    // per-frame apply rotation while held (use GameManager deltaTime indirectly)
    //    var dt = Time.deltaTime;
    //    if (_rotLeftHeld)
    //    {
    //        // If in spawning state rotate whole rig, else rotate connector (root handles routing)
    //        _manifold?.StartRotateLeft(); // ensures internal timers set
    //        _manifold?.RotationController?.ApplyRotateLeft(dt); // internal call - small direct access; you can expose wrapper
    //    }
    //    if (_rotRightHeld)
    //    {
    //        _manifold?.StartRotateRight();
    //        _manifold?.RotationController?.ApplyRotateRight(dt);
    //    }
    //}

    // called by spawner to inject runtime manifold instance
    //public void SetManifoldReference(ManifoldController_Root instance)
    //{
    //    _manifold = instance;

    //    // re-wire actions to new manifold
    //    Start(); // rebind (simple quick approach)
    //}
}
