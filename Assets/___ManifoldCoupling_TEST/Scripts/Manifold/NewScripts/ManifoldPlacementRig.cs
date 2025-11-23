using UnityEngine;

/// <summary>
/// Placement rig attached to Transform_Offset (the prefab node).
/// Places the manifold at a pose position and rotates it so the model's forward (ModelFacingPivot.forward)
/// points toward the camera horizontally (yaw only). Keeps the placement aligned to the plane normal.
/// Also exposes nudges, vertical movement and snap yaw to 45 degrees.
/// </summary>
[DisallowMultipleComponent]
public class ManifoldPlacementRig : MonoBehaviour
{
    [Header("Nodes (assign in inspector)")]
    [SerializeField] private Transform _rootTransform;       // Transform_Offset (this)
    [SerializeField] private Transform _rotationOffset;      // Rotation_Offset (yaw pivot)
    [SerializeField] private Transform _modelFacingPivot;    // ModelFacingPivot (child whose +Z is model forward)

    [Header("Placement settings")]
    [SerializeField] private float _verticalOffset = 0.05f;  // slight lift above plane
    [SerializeField] private float _nudgeStep = 0.02f;       // per-nudge movement in meters
    [SerializeField] private float _smoothRotationSpeed = 60f; // deg/sec while holding rotation
    [SerializeField, Range(0,10)] private int _rotationMultiplier; // deg/sec while holding rotation

    private Camera _mainCamera;
    private bool _canMove = false;

    private void Reset()
    {
        if (_rootTransform == null) _rootTransform = this.transform;
        if (_rotationOffset == null)
        {
            // try to locate common path
            var t = _rootTransform.Find("Scale_Offset/Rotation_Offset");
            _rotationOffset = t != null ? t : _rootTransform;
        }
    }

    private void Awake()
    {
        _mainCamera = GameManager.Instance?.MainCamera;
        if (_mainCamera == null) Debug.LogWarning("[ManifoldPlacementRig] MainCamera not found in GameManager.");
    }

    /// <summary>
    /// Place instance at pose position (use only position) and orient so
    /// ModelFacingPivot.forward points toward camera position (horizontally).
    /// Keeps object's up aligned to plane normal (placementPose.up).
    /// </summary>
    public void OnPlaced(Pose placementPose, Camera cam, float modelFacingOffsetY = 0f)
    {
        if (_rootTransform == null) _rootTransform = this.transform;

        // 1) Set base position on plane + small vertical lift
        _rootTransform.position = placementPose.position + placementPose.up * _verticalOffset;

        // 2) Set a base rotation that aligns up with the plane normal (so it sits flat)
        //    this creates a predictable world-space orientation before yaw correction
        Quaternion baseUpRot = Quaternion.FromToRotation(Vector3.up, placementPose.up);
        _rootTransform.rotation = baseUpRot;

        // 3) Determine camera (or provided) position and project onto the plane of placement
        Camera usedCam = cam ?? _mainCamera;
        if (usedCam == null)
        {
            Debug.LogWarning("[ManifoldPlacementRig] No camera available; skipping facing alignment.");
            return;
        }

        // world-space vector from manifold to camera
        Vector3 toCamera = usedCam.transform.position - _rootTransform.position;
        // project onto plane normal (placementPose.up) to remove vertical
        toCamera = Vector3.ProjectOnPlane(toCamera, placementPose.up);
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            // fallback to camera forward projected
            toCamera = Vector3.ProjectOnPlane(usedCam.transform.forward, placementPose.up);
            if (toCamera.sqrMagnitude < 0.0001f) return;
        }
        Vector3 targetDir = toCamera.normalized;

        // 4) Compute current world-space forward of modelFacingPivot
        if (_modelFacingPivot == null)
        {
            Debug.LogWarning("[ManifoldPlacementRig] ModelFacingPivot not assigned. Using rotationOffset.forward for alignment.");
            // fallback to rotation offset forward
            Vector3 currentForward = (_rotationOffset != null) ? _rotationOffset.forward : _rootTransform.forward;
            currentForward = Vector3.ProjectOnPlane(currentForward, placementPose.up).normalized;
            Quaternion yawDeltaFallback = Quaternion.FromToRotation(currentForward, targetDir);
            // restrict to yaw only
            yawDeltaFallback = ExtractYaw(yawDeltaFallback);
            // apply
            _rootTransform.rotation = yawDeltaFallback * _rootTransform.rotation;
            return;
        }

        // world-space forward of modelFacingPivot (given current base rotation)
        Vector3 pivotForward = _modelFacingPivot.forward;
        pivotForward = Vector3.ProjectOnPlane(pivotForward, placementPose.up).normalized;

        // 5) Compute yaw-only delta rotation that rotates pivotForward -> targetDir
        Quaternion delta = Quaternion.FromToRotation(pivotForward, targetDir);
        Quaternion deltaYaw = ExtractYaw(delta);

        // apply offset if desired (allows prefab designer tweak)
        if (Mathf.Abs(modelFacingOffsetY) > 0.001f)
        {
            deltaYaw = Quaternion.Euler(0f, modelFacingOffsetY, 0f) * deltaYaw;
        }

        _rootTransform.rotation = deltaYaw * _rootTransform.rotation;
    }

    // --------------------------
    // Public movement API (UI)
    // --------------------------
    public void EnableMovement(bool enable) => _canMove = enable;

    /// <summary>
    /// Nudge in local model axes: Vector3.forward points along Rotation_Offset.forward (model forward).
    /// </summary>
    public void NudgeLocal(Vector3 localDirection)
    {
        Debug.Log("Local dir: " + localDirection);

        if (!_canMove || _rootTransform == null) return;
        // prefer rotation pivot as the local frame
        Transform source = _modelFacingPivot != null ? _modelFacingPivot : _rotationOffset;
        Vector3 worldDelta = _nudgeStep * source.TransformDirection(localDirection.normalized);
        _rootTransform.position += worldDelta;
    }

    /// <summary>
    /// Move in world Y axis (absolute up/down)
    /// </summary>
    public void NudgeVertical(float sign)
    {
        if (!_canMove || _rootTransform == null) return;
        _rootTransform.position += _nudgeStep * sign * Vector3.up;
    }

    /// <summary>
    /// Smooth yaw rotation around Up (hold-based)
    /// </summary>
    public void RotateYawSmooth(float degreesPerSecond)
    {
        if (!_canMove || _rotationOffset == null) return;
        _rotationOffset.Rotate(Vector3.up, _rotationMultiplier * degreesPerSecond * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Snap yaw of rotation pivot to nearest 45 degrees (tap-based)
    /// </summary>
    public void SnapYawTo45()
    {
        if (_rotationOffset == null) return;
        Vector3 e = _rotationOffset.eulerAngles;
        float snapped = Mathf.Round(e.y / 45f) * 45f;
        _rotationOffset.rotation = Quaternion.Euler(e.x, snapped, e.z);
    }

    // --------------------------
    // Utility: extract yaw component from rotation quaternion
    // --------------------------
    private static Quaternion ExtractYaw(Quaternion q)
    {
        // Convert to euler, zero X and Z to keep only Y rotation
        Vector3 e = q.eulerAngles;
        return Quaternion.Euler(0f, e.y, 0f);
    }
}
