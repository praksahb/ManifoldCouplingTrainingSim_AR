using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public class ManifoldConnectorController : MonoBehaviour
{
    [Header("Connector Root (Movable Object)")]
    [SerializeField] private Transform _connectorRoot;

    [Header("Movement Frame (Same As PlacementRig)")]
    [Tooltip("Assign ModelFacingPivot from MainController for consistent movement.")]
    [SerializeField] private Transform _modelFacingPivot;

    [Header("Movement Settings")]
    [SerializeField] private float _nudgeStep = 0.02f;
    [SerializeField] private float _moveSmoothing = 0.06f;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 60f;  // deg/sec
    [SerializeField] private int _rotationMultiplier = 1; // same as PlacementRig

    [Header("Optional Raycast Movement")]
    [SerializeField] private ARRaycastManager _raycastManager;

    private Vector3 _startPositionLocal;

    private bool _controlEnabled = false;
    private Vector3 _velocity = Vector3.zero;

    private WaitForSeconds _delay;
    private Coroutine _invokeRoutine;

    public static event Action OnAlignmentChanged;

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    // -----------------------------------------------------
    // Setup
    // -----------------------------------------------------

    private void Start()
    {
        _delay = new WaitForSeconds(_moveSmoothing);
        _startPositionLocal = transform.localPosition;
    }

    public void EnableControl(bool enable)
    {
        _controlEnabled = enable;
    }

    /// <summary>
    /// Called by ManifoldMainController to sync movement frame.
    /// </summary>
    public void SetMovementPivot(Transform pivot)
    {
        _modelFacingPivot = pivot;
    }

    // -----------------------------------------------------
    // Local Movement (MATCHES PlacementRig EXACTLY)
    // -----------------------------------------------------

    public void NudgeLocal(Vector3 localDir)
    {
        if (!_controlEnabled || _connectorRoot == null) return;

        if (_modelFacingPivot == null)
        {
            Debug.LogWarning("[ConnectorController] Missing ModelFacingPivot! Movement will be wrong.");
            return;
        }

        Vector3 worldDelta = _modelFacingPivot.TransformDirection(localDir.normalized) * _nudgeStep;

        _connectorRoot.position = Vector3.SmoothDamp(
            _connectorRoot.position,
            _connectorRoot.position + worldDelta,
            ref _velocity,
            _moveSmoothing
        );

        TriggerAlignmentCheckDelayed();
    }

    // -----------------------------------------------------
    // Vertical Movement (Identical to PlacementRig)
    // -----------------------------------------------------

    public void NudgeVertical(float sign)
    {
        if (!_controlEnabled || _connectorRoot == null) return;

        _connectorRoot.position = Vector3.SmoothDamp(
            _connectorRoot.position,
            _connectorRoot.position + Vector3.up * _nudgeStep * sign,
            ref _velocity,
            _moveSmoothing
        );

        TriggerAlignmentCheckDelayed();
    }

    // -----------------------------------------------------
    // Rotation (Identical Behaviour to PlacementRig.RotateYawSmooth)
    // -----------------------------------------------------

    public void RotateSmoothYaw(float degreesPerSecond)
    {
        if (!_controlEnabled || _connectorRoot == null) return;

        float yaw = _rotationMultiplier * degreesPerSecond * Time.deltaTime;

        _connectorRoot.Rotate(Vector3.up, yaw, Space.World);

        TriggerAlignmentCheckDelayed();
    }

    // -----------------------------------------------------
    // Utility: Raycast Movement (unchanged)
    // -----------------------------------------------------

    public bool TryRaycastScreenPoint(Vector2 screenPoint, Camera cam, out Pose pose)
    {
        pose = default;
        if (_raycastManager == null) return false;

        if (_raycastManager.Raycast(screenPoint, _hits, TrackableType.Planes))
        {
            pose = _hits[0].pose;
            return true;
        }
        return false;
    }

    // -----------------------------------------------------
    // Alignment Event Delay (unchanged)
    // -----------------------------------------------------

    private void TriggerAlignmentCheckDelayed()
    {
        if (_invokeRoutine != null)
            StopCoroutine(_invokeRoutine);

        _invokeRoutine = StartCoroutine(DelayedAlignmentInvoke());
    }

    private IEnumerator DelayedAlignmentInvoke()
    {
        yield return _delay;
        OnAlignmentChanged?.Invoke();
    }

    public void ResetConnector()
    {
        transform.localPosition = _startPositionLocal;
        _controlEnabled = false;
    }
}
