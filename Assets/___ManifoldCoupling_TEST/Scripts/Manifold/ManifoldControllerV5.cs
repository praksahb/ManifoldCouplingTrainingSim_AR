using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DisallowMultipleComponent]
public class ManifoldControllerV5 : MonoBehaviour
{
    [Header("AR refs (auto-find fallback)")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Hierarchy (auto-find by name)")]
    public Transform transformOffset;          // Transform_Offset (root pivot used for placement)
    public Transform tankSideFixed;            // tank fixed (stays still)
    public Transform connectorSideMovable;     // the part we move (female)
    public Transform snapSource;               // male reference for snap
    public Transform snapTarget;               // target on female
    public Transform handle;                   // optional handle transform

    [Header("Spawn / Plane settings")]
    public float spawnForwardOffset = 2.5f;
    public float spawnVerticalOffset = 0.08f;
    public Vector3 placementPlaneNormal = Vector3.up; // set on OnPlaced

    [Header("UI Movement / Rotation settings")]
    public float moveStep = 0.02f;         // per hold update delta in local units
    public float rotateSpeedDegreesPerSec = 90f; // degrees per second when held
    public float moveSmoothing = 0.08f;    // smoothing for MoveConnector/MoveTransformOffset

    [Header("Tap rotation (micro-precision)")]
    public float tapMaxDuration = 0.18f;   // <= this counts as a tap -> performs 45° step
    public float tapAngle = 45f;           // degrees per tap

    [Header("Restrictions")]
    public float snapDistance = 0.04f;          // snap threshold
    public float snapSmoothTime = 0.10f;
    public float placementClampRadius = 1.5f;   // optional clamp radius around placement centre (Spawning)

    // runtime
    public GameState CurrentState { get; private set; } = GameState.Spawning;
    bool isPlaced = false;
    bool isSnapped = false;

    Vector3 moveVelocity = Vector3.zero;
    Vector3 snapVel = Vector3.zero;

    // internal flags used by UIHoldButton (bridge calls these while held)
    bool holdMoveForward = false;
    bool holdMoveBackward = false;
    bool holdMoveLeft = false;
    bool holdMoveRight = false;
    bool holdMoveUp = false;
    bool holdMoveDown = false;

    bool holdRotateLeft = false;
    bool holdRotateRight = false;

    // rotation press timers for tap detection
    float rotateLeftPressedTime = 0f;
    float rotateRightPressedTime = 0f;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (planeManager == null) planeManager = FindFirstObjectByType<ARPlaneManager>();

        AutoFind("Transform_Offset", ref transformOffset);
        AutoFind("TankSide(LEFT)_FIXED", ref tankSideFixed);
        AutoFind("ConnectorSide(RIGHT)_Movable", ref connectorSideMovable);
        AutoFind("SnapSource", ref snapSource);
        AutoFind("SnapTarget", ref snapTarget);
        AutoFind("HANDLE", ref handle);

        // ensure starting state
        CurrentState = GameState.Spawning;
    }

    void Update()
    {
        if (!isPlaced) return;

        // handle holds for movement + rotation per state
        if (CurrentState == GameState.Spawning)
        {
            HandleSpawningMovement();
        }
        else if (CurrentState == GameState.TutorialMode || CurrentState == GameState.AssessmentMode)
        {
            HandleConnectorMovement();
        }

        // maintain snap ONLY if actually snapped
        if (isSnapped)
            MaintainSnapState();
        else
            CheckSnapCondition();
    }

    // ----------------- Movement when placing whole manifold -----------------
    void HandleSpawningMovement()
    {
        Vector3 offsetDelta = Vector3.zero;
        if (holdMoveForward) offsetDelta += transformOffset.forward * moveStep;
        if (holdMoveBackward) offsetDelta += -transformOffset.forward * moveStep;
        if (holdMoveLeft) offsetDelta += -transformOffset.right * moveStep;
        if (holdMoveRight) offsetDelta += transformOffset.right * moveStep;
        if (holdMoveUp) offsetDelta += Vector3.up * moveStep;
        if (holdMoveDown) offsetDelta += Vector3.down * moveStep;

        if (offsetDelta.sqrMagnitude > 0f)
            MoveTransformOffset(offsetDelta);

        // rotation: smooth while holding; tap handled in StopRotate*
        float rotDelta = 0f;
        if (holdRotateLeft) rotDelta -= rotateSpeedDegreesPerSec * Time.deltaTime;
        if (holdRotateRight) rotDelta += rotateSpeedDegreesPerSec * Time.deltaTime;

        if (Mathf.Abs(rotDelta) > 0f)
            RotateTransformOffset(rotDelta);
    }

    // ----------------- Movement when manipulating connector -----------------
    void HandleConnectorMovement()
    {
        Vector3 cDelta = Vector3.zero;
        if (holdMoveForward) cDelta += transformOffset.forward * moveStep;
        if (holdMoveBackward) cDelta += -transformOffset.forward * moveStep;
        if (holdMoveLeft) cDelta += -transformOffset.right * moveStep;
        if (holdMoveRight) cDelta += transformOffset.right * moveStep;
        if (holdMoveUp) cDelta += Vector3.up * moveStep;
        if (holdMoveDown) cDelta += Vector3.down * moveStep;

        if (cDelta.sqrMagnitude > 0f)
            MoveConnector(cDelta);

        float rotDelta = 0f;
        if (holdRotateLeft) rotDelta -= rotateSpeedDegreesPerSec * Time.deltaTime;
        if (holdRotateRight) rotDelta += rotateSpeedDegreesPerSec * Time.deltaTime;

        if (Mathf.Abs(rotDelta) > 0f)
            RotateConnector(rotDelta);
    }

    // ---------- Placement called by placer ----------
    public void OnPlaced(Pose placementPose, Camera cam)
    {
        if (transformOffset == null) transformOffset = this.transform;

        // Place exactly on the detected pose. No automatic forward offset.
        transformOffset.position = placementPose.position;
        // prefer facing camera horizontally (no tilt)
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.000001f)
            transformOffset.rotation = Quaternion.LookRotation(camForward);
        else
            transformOffset.rotation = placementPose.rotation;

        placementPlaneNormal = placementPose.up;

        isPlaced = true;
        isSnapped = false;
        CurrentState = GameState.Spawning;

        Debug.Log("[V5.1] OnPlaced: placed at " + transformOffset.position + " State=Spawning");
    }

    // ---------- Movement helpers ----------
    void MoveTransformOffset(Vector3 worldDelta)
    {
        // Allow vertical movement during Spawning (user requested)
        Vector3 target = transformOffset.position + worldDelta;
        transformOffset.position = Vector3.SmoothDamp(transformOffset.position, target, ref moveVelocity, moveSmoothing);
    }

    void RotateTransformOffset(float degrees)
    {
        transformOffset.Rotate(Vector3.up, degrees, Space.World);
    }

    void MoveConnector(Vector3 worldDelta)
    {
        if (connectorSideMovable == null) return;
        Vector3 target = connectorSideMovable.position + worldDelta;
        connectorSideMovable.position = Vector3.SmoothDamp(connectorSideMovable.position, target, ref moveVelocity, moveSmoothing);
    }

    void RotateConnector(float degrees)
    {
        if (connectorSideMovable == null) return;
        connectorSideMovable.Rotate(Vector3.up, degrees, Space.World);
    }

    // ---------- Snapping ----------
    void CheckSnapCondition()
    {
        if (snapSource == null || snapTarget == null) return;

        float d = Vector3.Distance(snapSource.position, snapTarget.position);
        if (d <= snapDistance)
        {
            isSnapped = true;
            snapVel = Vector3.zero;
            Debug.Log("[V5.1] Snap achieved.");
        }
    }

    void MaintainSnapState()
    {
        if (connectorSideMovable == null || snapSource == null) return;

        connectorSideMovable.position = Vector3.SmoothDamp(connectorSideMovable.position, snapSource.position, ref snapVel, snapSmoothTime);
        connectorSideMovable.rotation = Quaternion.Slerp(connectorSideMovable.rotation, snapSource.rotation, Time.deltaTime / Mathf.Max(0.001f, snapSmoothTime));
    }

    // ---------- Public hold start/stop API ----------
    // Movement
    public void StartMoveForward() => holdMoveForward = true;
    public void StopMoveForward() => holdMoveForward = false;

    public void StartMoveBackward() => holdMoveBackward = true;
    public void StopMoveBackward() => holdMoveBackward = false;

    public void StartMoveLeft() => holdMoveLeft = true;
    public void StopMoveLeft() => holdMoveLeft = false;

    public void StartMoveRight() => holdMoveRight = true;
    public void StopMoveRight() => holdMoveRight = false;

    public void StartMoveUp() => holdMoveUp = true;
    public void StopMoveUp() => holdMoveUp = false;

    public void StartMoveDown() => holdMoveDown = true;
    public void StopMoveDown() => holdMoveDown = false;

    // Rotation (hold vs tap detection)
    public void StartRotateLeft()
    {
        // Starting a left-rotation hold
        holdRotateLeft = true;
        rotateLeftPressedTime = Time.time;
        // reset any snap smoothing to avoid "ghost lerp"
        snapVel = Vector3.zero;
    }

    public void StopRotateLeft()
    {
        holdRotateLeft = false;
        float held = Time.time - rotateLeftPressedTime;

        // If a short tap, perform a micro-rotation step
        if (held <= tapMaxDuration)
        {
            // micro-tap: rotate immediate -tapAngle degrees
            if (connectorSideMovable != null)
                connectorSideMovable.Rotate(Vector3.up, -tapAngle, Space.World);
            Debug.Log("[V5.1] Tap RotateLeft performed: -" + tapAngle);
        }

        // IMPORTANT: do NOT snap to nearest 45° here unless actually snapped
        // (we intentionally avoid automatic snapping while free-rotating)
    }

    public void StartRotateRight()
    {
        holdRotateRight = true;
        rotateRightPressedTime = Time.time;
        snapVel = Vector3.zero;
    }

    public void StopRotateRight()
    {
        holdRotateRight = false;
        float held = Time.time - rotateRightPressedTime;

        if (held <= tapMaxDuration)
        {
            if (connectorSideMovable != null)
                connectorSideMovable.Rotate(Vector3.up, tapAngle, Space.World);
            Debug.Log("[V5.1] Tap RotateRight performed: +" + tapAngle);
        }

        // do NOT snap automatically
    }

    // Manual snap button (keeps behavior for testing)
    public void ManualSnap()
    {
        if (snapSource == null || connectorSideMovable == null) return;
        isSnapped = true;
        connectorSideMovable.position = snapSource.position;
        connectorSideMovable.rotation = snapSource.rotation;
        Debug.Log("[V5.1] ManualSnap executed (forced).");
    }

    // --------- State control ----------
    public void SetGameState(GameState state)
    {
        CurrentState = state;
        // clear all hold flags
        holdMoveForward = holdMoveBackward = holdMoveLeft = holdMoveRight = holdMoveUp = holdMoveDown = false;
        holdRotateLeft = holdRotateRight = false;
    }

    // Utility auto-find
    void AutoFind(string name, ref Transform t)
    {
        if (t != null) return;
        Transform f = transform.Find(name);
        if (f != null) { t = f; return; }
        var go = GameObject.Find(name);
        if (go != null) t = go.transform;
    }
}
