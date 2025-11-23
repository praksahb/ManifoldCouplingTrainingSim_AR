using DG.Tweening;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ManifoldSnapController : MonoBehaviour
{
    [Header("Snap References")]
    [SerializeField] private Transform _snapSource;        // Male fixed reference
    [SerializeField] private Transform _snapTarget;        // Female snap point
    [SerializeField] private Transform _movableConnector;  // Root of movable female part

    [Header("Alignment Settings")]
    [SerializeField] private float _alignmentDistance = 0.08f;
    [SerializeField] private float _alignmentAngle = 20f;

    [Header("Snap Settings")]
    [SerializeField] private float _snapDuration = 0.75f;
    [SerializeField] private Ease _snapEase = Ease.OutCubic;

    private bool _isAligned = false;
    private bool _isSnapped = false;

    private Vector3 _alignConnectorPosition;

    // EVENTS
    public event Action<bool> OnAlignmentChanged;
    public event Action<bool> OnSnapStateChanged;

    public bool IsAligned => _isAligned;
    public bool IsSnapped => _isSnapped;

    private void OnEnable()
    {
        ManifoldConnectorController.OnAlignmentChanged += RecheckAlignment;
    }

    private void OnDisable()
    {
        ManifoldConnectorController.OnAlignmentChanged -= RecheckAlignment;
    }

    // 1) ALIGNMENT CHECK (triggered only when connector moves/rotates)
    private void RecheckAlignment()
    {
        if (_snapSource == null || _snapTarget == null)
            return;

        if (_isSnapped)
            return;

        float dist = Vector3.Distance(_snapSource.position, _snapTarget.position);
        float ang = Quaternion.Angle(_snapSource.rotation, _snapTarget.rotation);

        bool alignedNow = (dist <= _alignmentDistance && ang <= _alignmentAngle);

        if (alignedNow != _isAligned)
        {
            _isAligned = alignedNow;
            OnAlignmentChanged?.Invoke(_isAligned);
        }
    }

    // 2) MANUAL SNAP
    public void ManualSnap()
    {
        if (!_isAligned)
        {
            Debug.Log("[SnapController] Cannot snap because NOT ALIGNED.");
            return;
        }

        if (_movableConnector == null || _snapSource == null)
            return;

        Debug.Log("[SnapController] Manual_UnSnap()");

        _isSnapped = true;

        // Save position of the _movableConnector in worldSpace
        _alignConnectorPosition = _movableConnector.transform.position;

        // compute final position so snapTarget matches snapSource exactly
        Vector3 finalPos =
            _movableConnector.position +
            (_snapSource.position - _snapTarget.position);

        Quaternion finalRot = Quaternion.LookRotation(
            _snapSource.forward,
            _snapSource.up
        );

        // Tween into place
        _movableConnector.DOMove(finalPos, _snapDuration).SetEase(_snapEase);
        _movableConnector.DORotateQuaternion(finalRot, _snapDuration)
            .SetEase(_snapEase)
            .OnComplete(() =>
            {
                Debug.Log("[SnapController] Snap COMPLETE.");
                OnSnapStateChanged?.Invoke(true);
            });
    }

    // 3) MANUAL UNSNAP (Tutorial Step 5)
    public void ManualUnsnap()
    {
        if (!_isSnapped)
            return;

        Debug.Log("[SnapController] ManualUnsnap()");


        // Tween back to pre-snap alignment position
        _movableConnector.DOMove(_alignConnectorPosition, _snapDuration).SetEase(_snapEase).OnComplete(() =>
        {
            _isSnapped = false;
            // broadcast tutorial events
            OnSnapStateChanged?.Invoke(false);     // snap removed
        });
    }

    // Reset
    public void ResetSnap()
    {
        _isSnapped = false;
        _isAligned = false;

        OnAlignmentChanged?.Invoke(false);
        OnSnapStateChanged?.Invoke(false);
    }
}
