using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Spawner that places the manifold prefab at the AR-detected pose position (no automatic rotation).
/// After instantiation it calls the PlacementRig.OnPlaced(...) which will align the prefab's Z
/// (ModelFacingPivot.forward) to face the camera horizontally.
/// Works for Editor (mouse) and device (touch).
/// </summary>
[DisallowMultipleComponent]
public class ManifoldSpawner : MonoBehaviour
{
    [Header("Prefab & UI Bridge")]
    [SerializeField] private ManifoldMainController _manifoldPrefab;
    [SerializeField] private InputController_UI _uiBridge;

    private ARRaycastManager _raycastManager;
    private ARPlaneManager _planeManager;
    private Camera _mainCamera;

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private bool _placementPoseValid;
    private Pose _placementPose;
    private ManifoldMainController _spawnedInstance;

    private void Start()
    {
        // Prefer GameManager references if available
        _mainCamera = GameManager.Instance?.MainCamera;
        _raycastManager = GameManager.Instance?.RaycastManager;
        _planeManager = GameManager.Instance?.PlaneManager;

        if (_raycastManager == null) Debug.LogWarning("[ManifoldSpawner] ARRaycastManager not found via GameManager.");
        if (_planeManager == null) Debug.LogWarning("[ManifoldSpawner] ARPlaneManager not found via GameManager.");
        if (_mainCamera == null) Debug.LogWarning("[ManifoldSpawner] MainCamera not found via GameManager.");
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Prespawning)
            return;

        UpdatePlacementPose();
        HandlePlacementInput();
    }

    private void UpdatePlacementPose()
    {
        if (_raycastManager == null)
        {
            _placementPoseValid = false;
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (_raycastManager.Raycast(screenCenter, _hits, TrackableType.Planes))
        {
            _placementPoseValid = true;
            _placementPose = _hits[0].pose;
            // We intentionally DO NOT use placementPose.rotation as final rotation.
            // Rotation will be decided by the placement rig using the model's Z vector.
        }
        else
        {
            _placementPoseValid = false;
        }
    }

    private bool IsPlacementTriggered()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#else
        if (Input.touchCount == 0) return false;
        return Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    private void HandlePlacementInput()
    {
        if (!_placementPoseValid || !IsPlacementTriggered() || _spawnedInstance != null)
            return;

        SpawnAtPose();
    }

    private void SpawnAtPose()
    {
        if (_manifoldPrefab == null)
        {
            Debug.LogError("[ManifoldSpawner] Manifold prefab not assigned.");
            return;
        }

        // Instantiate using pose.position only; rotation will be corrected by OnPlaced
        _spawnedInstance = Instantiate(_manifoldPrefab, _placementPose.position, Quaternion.identity);

        if (_spawnedInstance == null)
        {
            Debug.LogError("[ManifoldSpawner] Instantiate failed.");
            return;
        }

        GameManager.Instance.CurrentManifold = _spawnedInstance;
        GameManager.Instance.SetState(GameState.Spawning);

        // disable plane visuals (keep plane detection)
        if (_planeManager != null)
        {
            foreach (var p in _planeManager.trackables)
                p.gameObject.SetActive(false);

            // stop detecting planes after spawning
            _planeManager.enabled = false;
        }

        // Wire UI bridge (if available)
        //if (_uiBridge != null)
        //_uiBridge.SetManifoldReference(_spawnedInstance);
        else
            Debug.Log("[ManifoldSpawner] UI bridge not assigned. Manual wiring possible via inspector.");

        // Now call placement rig to align rotation using model-facing pivot (this will align Z->camera)

        if (_spawnedInstance.PlacementRig != null)
        {
            _spawnedInstance.OnPlaced(_placementPose, _mainCamera);
            _spawnedInstance.EnablePlacementMovement(true);
        }
        else
        {
            Debug.LogWarning("[ManifoldSpawner] PlacementRig not found on spawned prefab.");
        }

        // Done: we only spawn once; disable spawner
        enabled = false;

        Debug.Log("[ManifoldSpawner] Spawned and aligned manifold instance.");
    }
}
