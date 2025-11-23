using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public class ManifoldController_v3 : MonoBehaviour
{
    [Header("AR (auto-find if empty)")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Hierarchy (auto-find by name if left empty)")]
    public Transform transformOffset;            // Transform_Offset (pivot used for moving)
    public Transform tankSideFixed;              // TankSide(LEFT)_FIXED
    public Transform connectorSideMovable;       // ConnectorSide(RIGHT)_Movable
    public Transform snapSource;                 // SnapSource (left fixed)
    public Transform snapTarget;                 // SnapTarget (right movable)
    public Transform handle;                     // HANDLE (child of movable)

    [Header("Spawn / Interaction Settings")]
    public float spawnForwardOffset = 1.0f;      // Option B: 1.0 m in front of camera
    public float spawnVerticalOffset = 0.05f;    // lift above plane
    public float snapDistance = 0.05f;           // 5 cm threshold to snap
    public float rotationSpeed = 0.2f;

    // runtime state
    bool isDragging = false;
    bool isSnapped = false;
    float lastX;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // debug
    Vector3 lastRayHitPosition = Vector3.zero;
    bool lastRayHit = false;
    GUIStyle guiStyle;

    void Awake()
    {
        // Ensure runtime logging is enabled
        Debug.unityLogger.logEnabled = true;

        // auto-find AR components (non-static)
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();

        if (planeManager == null)
            planeManager = FindFirstObjectByType<ARPlaneManager>();

        // Auto-find transforms if not assigned (safe, non-static)
        AutoFindTransformIfNull("Transform_Offset", ref transformOffset);
        AutoFindTransformIfNull("TankSide(LEFT)_FIXED", ref tankSideFixed);
        AutoFindTransformIfNull("ConnectorSide(RIGHT)_Movable", ref connectorSideMovable);
        AutoFindTransformIfNull("SnapSource", ref snapSource);
        AutoFindTransformIfNull("SnapTarget", ref snapTarget);
        AutoFindTransformIfNull("HANDLE", ref handle);

        guiStyle = new GUIStyle { fontSize = 14, normal = { textColor = Color.white } };

        Debug.Log($"[ManifoldV3] Awake called. RaycastManager found: {raycastManager != null}");
    }

    void Start()
    {
        Debug.Log("[ManifoldV3] Start called.");
    }

    // IMPORTANT: Call this from your spawner (AR_ObjectPlacer) right after Instantiate()
    // so the prefab is placed with the camera-forward offset (1.0m) and slight vertical lift.
    public void OnPlaced(Pose placementPose, Camera cam)
    {
        if (transformOffset == null)
        {
            Debug.LogWarning("[ManifoldV3] OnPlaced called but transformOffset is null");
            transformOffset = this.transform; // fallback
        }

        // compute forward offset relative to camera orientation
        Vector3 forward = (cam != null) ? cam.transform.forward : Vector3.forward;
        Vector3 offset = forward * spawnForwardOffset + placementPose.up * spawnVerticalOffset;

        transformOffset.position = placementPose.position + offset;

        // rotate to face the camera horizontally (preserve up)
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

        Debug.Log($"[ManifoldV3] Placed at {transformOffset.position:F3}; spawnOffset={spawnForwardOffset}m");
    }

    void Update()
    {
        // The controller runs per-instance, logs behavior and manages interactions.
        if (!isSnapped)
        {
            HandleDragging();
            HandleRotationGesture();
            CheckSnapCondition();
        }
    }

    // Dragging moves only the connector (right movable) if available
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
            Debug.Log("[ManifoldV3] Dragging started.");
        }

        if (isDragging)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.Planes))
            {
                Pose p = hits[0].pose;
                lastRayHit = true;
                lastRayHitPosition = p.position;

                if (connectorSideMovable != null)
                {
                    connectorSideMovable.position = p.position;
                    Debug.Log($"[ManifoldV3] Drag Move to {p.position:F3}");
                }
            }
            else
            {
                lastRayHit = false;
            }
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (isDragging)
                Debug.Log("[ManifoldV3] Drag ended.");
            isDragging = false;
        }
    }

    void HandleRotationGesture()
    {
        // do not rotate while actively dragging
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
                Debug.Log($"[ManifoldV3] Rotate by {-deltaX * rotationSpeed:F3}");
            }
        }
    }

    void CheckSnapCondition()
    {
        if (snapSource == null || snapTarget == null) return;

        float d = Vector3.Distance(snapSource.position, snapTarget.position);
        // log snap distance occasionally to help debug
        if (d <= snapDistance * 2f)
            Debug.Log($"[ManifoldV3] SnapDistance: {d:F4} (threshold {snapDistance})");

        if (d <= snapDistance)
            PerformSnap();
    }

    void PerformSnap()
    {
        isSnapped = true;
        if (connectorSideMovable != null && snapSource != null)
        {
            connectorSideMovable.position = snapSource.position;
            connectorSideMovable.rotation = snapSource.rotation;
        }
        Debug.Log("[ManifoldV3] SNAPPED: connector aligned to snapSource. Dragging disabled.");
    }

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
            Debug.Log($"[ManifoldV3] Auto-found transform '{name}' -> {t.name}");
        }
    }

    // lightweight on-screen debug
    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        GUI.Box(new Rect(8, 8, 420, 180), "ManifoldV3 Debug");
        GUI.Label(new Rect(16, 32, 400, 160), BuildDebugString(), guiStyle);
    }

    string BuildDebugString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"SNAPPED: {isSnapped}");
        sb.AppendLine($"ConnectorPos: {(connectorSideMovable ? connectorSideMovable.position.ToString("F3") : "null")}");
        sb.AppendLine($"SnapSource: {(snapSource ? snapSource.position.ToString("F3") : "null")}");
        sb.AppendLine($"SnapTarget: {(snapTarget ? snapTarget.position.ToString("F3") : "null")}");
        sb.AppendLine($"LastRayHit: {lastRayHit}  LastRayPos: {lastRayHitPosition.ToString("F3")}");
        sb.AppendLine($"Planes: {(planeManager ? planeManager.trackables.count.ToString() : "0")}");
        return sb.ToString();
    }
}
