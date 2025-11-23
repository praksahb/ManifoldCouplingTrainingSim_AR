using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public class ManifoldControllerV4 : MonoBehaviour
{
    [Header("AR (auto-find if empty)")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Hierarchy (auto-find by name if left empty)")]
    public Transform transformOffset;            // Transform_Offset (used for overall placement if needed)
    public Transform tankSideFixed;              // TankSide(LEFT)_FIXED (male - fixed)
    public Transform connectorSideMovable;       // ConnectorSide(RIGHT)_Movable (female - movable)
    public Transform snapSource;                 // SnapSource (male reference)
    public Transform snapTarget;                 // SnapTarget (female reference)
    public Transform handle;                     // HANDLE (should be child of connectorSideMovable)

    [Header("Spawn / Interaction Settings")]
    public float spawnForwardOffset = 3.0f;      // 3 meters forward from camera (Option B was 1.0m; you requested 2-3m)
    public float spawnVerticalOffset = 0.05f;    // small lift above plane
    public float snapDistance = 0.05f;           // snap threshold in meters
    public float rotationSpeed = 0.2f;           // swipe rotation sensitivity
    public float snapSmoothTime = 0.12f;         // smoothing time for snap LERP
    public bool useSmoothSnap = true;            // toggle smooth snap (lerp) vs immediate

    // runtime state (instance-only)
    bool isDragging = false;
    bool isSnapped = false;
    bool isPlaced = false;
    float lastX;
    Vector3 snapVelocity = Vector3.zero;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // debug
    Vector3 lastRayHitPosition = Vector3.zero;
    bool lastRayHit = false;
    GUIStyle guiStyle;

    void Awake()
    {
        // enable logs explicitly for runtime debugging
        Debug.unityLogger.logEnabled = true;

        // auto-find AR components (non-static)
        if (raycastManager == null)
        {
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
            Debug.Log("[ManifoldV4] ARRaycastManager auto-found: " + (raycastManager != null));
        }

        if (planeManager == null)
        {
            planeManager = FindFirstObjectByType<ARPlaneManager>();
            Debug.Log("[ManifoldV4] ARPlaneManager auto-found: " + (planeManager != null));
        }

        // auto-find transforms by name (safe)
        AutoFindTransformIfNull("Transform_Offset", ref transformOffset);
        AutoFindTransformIfNull("TankSide(LEFT)_FIXED", ref tankSideFixed);
        AutoFindTransformIfNull("ConnectorSide(RIGHT)_Movable", ref connectorSideMovable);
        AutoFindTransformIfNull("SnapSource", ref snapSource);
        AutoFindTransformIfNull("SnapTarget", ref snapTarget);
        AutoFindTransformIfNull("HANDLE", ref handle);

        guiStyle = new GUIStyle { fontSize = 14, normal = { textColor = Color.white } };

        Debug.Log("[ManifoldV4] Awake complete.");
    }

    void Start()
    {
        Debug.Log("[ManifoldV4] Start called. Ready. isPlaced=" + isPlaced);
    }

    /// <summary>
    /// Call this immediately after instantiating the prefab (from AR_ObjectPlacer).
    /// It applies the forward + vertical offset relative to the camera and marks the instance placed.
    /// Example:
    /// var controller = spawnedObject.GetComponentInChildren<ManifoldControllerV4>();
    /// controller?.OnPlaced(placementPose, Camera.main);
    /// </summary>
    public void OnPlaced(Pose placementPose, Camera cam)
    {
        if (transformOffset == null)
        {
            // fallback to root transform if missing
            transformOffset = this.transform;
            Debug.LogWarning("[ManifoldV4] transformOffset was null; falling back to prefab root.");
        }

        // compute forward offset relative to camera orientation
        Vector3 forward = (cam != null) ? cam.transform.forward : Vector3.forward;
        Vector3 offset = forward.normalized * spawnForwardOffset + placementPose.up * spawnVerticalOffset;

        transformOffset.position = placementPose.position + offset;

        // rotate to face camera horizontally if camera available
        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.0001f)
                transformOffset.rotation = Quaternion.LookRotation(camForward);
        }
        else
        {
            transformOffset.rotation = placementPose.rotation;
        }

        isPlaced = true;
        Debug.Log($"[ManifoldV4] OnPlaced -> position {transformOffset.position:F3}, offset {spawnForwardOffset}m");
    }

    void Update()
    {
        // do nothing until this instance has been placed
        if (!isPlaced) return;

        // only movable connector interacts; male side stays fixed
        if (!isSnapped)
        {
            HandleDragging();
            HandleRotationGesture();
            CheckSnapCondition();
        }
        else
        {
            // when snapped, keep connector aligned (use smooth hold)
            MaintainSnapAlignment();
        }
    }

    #region Drag & Rotate

    void HandleDragging()
    {
        if (raycastManager == null) return;
        if (Input.touchCount == 0)
        {
            lastRayHit = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            isDragging = true;
            lastX = touch.position.x;
            Debug.Log("[ManifoldV4] Dragging started.");
        }

        if (isDragging)
        {
            // Raycast to planes to get a good world pose
            if (raycastManager.Raycast(touch.position, hits, TrackableType.Planes))
            {
                Pose p = hits[0].pose;
                lastRayHit = true;
                lastRayHitPosition = p.position;

                if (connectorSideMovable != null)
                {
                    // move the connector (female) only
                    connectorSideMovable.position = p.position;
                    Debug.Log($"[ManifoldV4] Connector moved to {p.position:F3}");
                }
            }
            else
            {
                lastRayHit = false;
            }
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (isDragging) Debug.Log("[ManifoldV4] Drag ended.");
            isDragging = false;
        }
    }

    void HandleRotationGesture()
    {
        // allow rotation only when not actively dragging
        if (isDragging) return;
        if (Input.touchCount != 1) return;

        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
        {
            lastX = t.position.x;
        }
        else if (t.phase == TouchPhase.Moved)
        {
            float deltaX = t.position.x - lastX;
            lastX = t.position.x;

            if (connectorSideMovable != null && !isSnapped)
            {
                connectorSideMovable.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
                Debug.Log($"[ManifoldV4] Connector rotated by {-deltaX * rotationSpeed:F3} deg (raw).");
            }
        }
    }

    #endregion

    #region Snap

    void CheckSnapCondition()
    {
        if (snapSource == null || snapTarget == null) return;

        float currentDistance = Vector3.Distance(snapSource.position, snapTarget.position);

        // occasional debug when in range
        if (currentDistance <= snapDistance * 2f)
            Debug.Log($"[ManifoldV4] Snap distance = {currentDistance:F4} (threshold {snapDistance})");

        if (currentDistance <= snapDistance)
        {
            Debug.Log("[ManifoldV4] Snap threshold reached. Performing snap...");
            PerformSnap();
        }
    }

    void PerformSnap()
    {
        isSnapped = true;

        if (connectorSideMovable == null || snapSource == null)
        {
            Debug.LogWarning("[ManifoldV4] Cannot perform snap; missing connector or snapSource.");
            return;
        }

        if (!useSmoothSnap)
        {
            connectorSideMovable.position = snapSource.position;
            connectorSideMovable.rotation = snapSource.rotation;
            Debug.Log("[ManifoldV4] Snap applied (immediate).");
        }
        else
        {
            // start smooth alignment by prepping velocities and letting MaintainSnapAlignment handle it
            snapVelocity = Vector3.zero;
            Debug.Log("[ManifoldV4] Snap started (smooth).");
        }

        // After snapping, prevent further whole-connector drag/rotation
        isDragging = false;
    }

    void MaintainSnapAlignment()
    {
        if (connectorSideMovable == null || snapSource == null) return;

        if (!useSmoothSnap)
        {
            connectorSideMovable.position = snapSource.position;
            connectorSideMovable.rotation = snapSource.rotation;
            return;
        }

        // Smoothly approach snapSource position and rotation
        connectorSideMovable.position = Vector3.SmoothDamp(connectorSideMovable.position, snapSource.position, ref snapVelocity, snapSmoothTime);
        connectorSideMovable.rotation = Quaternion.Slerp(connectorSideMovable.rotation, snapSource.rotation, Time.deltaTime / Mathf.Max(0.01f, snapSmoothTime));

        // If near enough, finalize
        float d = Vector3.Distance(connectorSideMovable.position, snapSource.position);
        if (d <= 0.001f)
        {
            connectorSideMovable.position = snapSource.position;
            connectorSideMovable.rotation = snapSource.rotation;
            Debug.Log("[ManifoldV4] Snap finished (finalized).");
        }
    }

    #endregion

    #region Utilities & Debug

    void AutoFindTransformIfNull(string name, ref Transform t)
    {
        if (t != null) return;
        Transform found = transform.Find(name);
        if (found == null)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) found = go.transform;
        }
        if (found != null)
        {
            t = found;
            Debug.Log($"[ManifoldV4] Auto-found '{name}' -> {t.name}");
        }
    }

    // Lightweight on-screen debug overlay
    void OnGUI()
    {
        if (!isPlaced)
        {
            GUI.Box(new Rect(8, 8, 360, 80), "Manifold V4 (not placed)");
            GUI.Label(new Rect(16, 32, 340, 40), "Waiting for placement... call OnPlaced from spawner.", guiStyle);
            return;
        }

        GUI.Box(new Rect(8, 8, 420, 200), "ManifoldV4 Debug");
        string s = BuildDebugString();
        GUI.Label(new Rect(16, 32, 400, 180), s, guiStyle);
    }

    string BuildDebugString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Placed: {isPlaced}  Snapped: {isSnapped}");
        sb.AppendLine($"ConnectorPos: {(connectorSideMovable ? connectorSideMovable.position.ToString("F3") : "null")}");
        sb.AppendLine($"SnapSource: {(snapSource ? snapSource.position.ToString("F3") : "null")}");
        sb.AppendLine($"SnapTarget: {(snapTarget ? snapTarget.position.ToString("F3") : "null")}");
        sb.AppendLine($"LastRayHit: {lastRayHit}  LastRayPos: {lastRayHitPosition.ToString("F3")}");
        sb.AppendLine($"Planes: {(planeManager ? planeManager.trackables.count.ToString() : "0")}");
        sb.AppendLine($"SpawnOffset: {spawnForwardOffset:F2} m");
        return sb.ToString();
    }

    #endregion
}
