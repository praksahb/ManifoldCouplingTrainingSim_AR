using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ManifoldHandleController : MonoBehaviour
{
    [Header("Handle Parts")]
    [SerializeField] private Transform _handleRoot;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 90f;     // deg/sec while holding
    [SerializeField] private float _minAngle = 0f;           // fully unlocked angle
    [SerializeField] private float _maxAngle = 90f;          // fully locked angle
    [SerializeField] private float _lockThreshold = 85f;     // threshold for lock state
    [SerializeField] private float _unlockThreshold = 5f;    // threshold for unlock state

    [Header("Smoothing")]
    [SerializeField] private bool _useSmoothing = true;
    [SerializeField] private float _smoothTime = 0.08f;

    // runtime
    private float _currentAngle = 0f;
    private float _targetAngle = 0f;
    private float _angleVelocity = 0f;

    private bool _isLocked = false;
    private bool _rotationAllowed = true; // tutorial input will toggle this

    // events
    public event Action<bool> OnLockStateChanged;      // true = locked
    public event Action<bool> OnUnlockStateChanged;    // true = unlocked
    public event Action<float> OnHandleAngleChanged;

    private void Reset()
    {
        if (_handleRoot == null)
            _handleRoot = transform;
    }

    private void Start()
    {
        _currentAngle = 0f;
        _targetAngle = 0f;
        ApplyAngleToTransform(0f);
    }

    private void Update()
    {
        if (_useSmoothing)
            _currentAngle = Mathf.SmoothDamp(_currentAngle, _targetAngle, ref _angleVelocity, _smoothTime);
        else
            _currentAngle = _targetAngle;

        ApplyAngleToTransform(_currentAngle);
        OnHandleAngleChanged?.Invoke(_currentAngle);

        CheckLockUnlockState();
    }

    // ---------------------------------------------------------
    // INPUT COMMANDS
    // ---------------------------------------------------------

    public void EnableRotation(bool enable)
    {
        _rotationAllowed = enable;
    }

    /// <summary>
    /// Rotate while holding. direction = +1 or -1.
    /// </summary>
    public void RotateHold(float direction)
    {
        if (!_rotationAllowed)
            return;

        float delta = direction * _rotationSpeed * Time.deltaTime;
        _targetAngle = Mathf.Clamp(_targetAngle + delta, _minAngle, _maxAngle);
    }

    /// <summary>
    /// Force angle (for tutorial initialization)
    /// </summary>
    public void SetAngle(float angle)
    {
        _targetAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);
        _currentAngle = _targetAngle;
        ApplyAngleToTransform(_currentAngle);
        CheckLockUnlockState(forceEvent: true);
    }

    public float GetAngle() => _currentAngle;

    public bool IsLocked() => _isLocked;

    public void ResetHandle()
    {
        SetAngle(0f);
        _isLocked = false;
        OnUnlockStateChanged?.Invoke(true);
    }

    // ---------------------------------------------------------
    // INTERNAL HELPERS
    // ---------------------------------------------------------

    private void ApplyAngleToTransform(float angle)
    {
        // You said: rotate around Z-axis
        _handleRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CheckLockUnlockState(bool forceEvent = false)
    {
        bool lockedNow = _currentAngle >= _lockThreshold;
        bool unlockedNow = _currentAngle <= _unlockThreshold;

        if (lockedNow && !_isLocked)
        {
            _isLocked = true;
            OnLockStateChanged?.Invoke(true);

            // once locked, disallow rotation unless tutorial re-enables
            _rotationAllowed = false;
        }
        else if (unlockedNow && _isLocked)
        {
            _isLocked = false;
            OnUnlockStateChanged?.Invoke(true);
        }
        else if (forceEvent)
        {
            if (_isLocked) OnLockStateChanged?.Invoke(true);
            else OnUnlockStateChanged?.Invoke(true);
        }
    }
}
