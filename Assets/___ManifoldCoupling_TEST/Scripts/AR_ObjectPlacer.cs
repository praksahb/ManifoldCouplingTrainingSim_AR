using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AR_ObjectPlacer : MonoBehaviour
{
    [Header("References and test values")]
    [SerializeField] private ManifoldControllerV5 objectPrefab;
    [SerializeField] private UIManifoldBridge m_uiBridge;
    [SerializeField] private Vector3 m_cameraOffset;

    [SerializeField] private ARRaycastManager raycastMgr;
    [SerializeField] private ARPlaneManager planeMgr;

    private ManifoldControllerV5 spawnedObject;
    private Pose placementPose;
    private bool placementPoseIsValid = false;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (spawnedObject == null)
        {
            UpdatePlacementPose();
            HandlePlacementInput();
        }
    }

    // ----------------------------------------------
    // 1. Determine pose from AR plane under screen center
    // ----------------------------------------------
    void UpdatePlacementPose()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastMgr.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            placementPoseIsValid = true;
            placementPose = hits[0].pose;

            // Lock rotation to horizontal facing camera (BEFORE placement only)
            var forward = Camera.main.transform.forward;
            forward.y = 0;
            placementPose.rotation = Quaternion.LookRotation(forward);
        }
        else
        {
            placementPoseIsValid = false;
        }
    }

    // ----------------------------------------------
    // 2. Handle tap to place — ONCE ONLY
    // ----------------------------------------------
    void HandlePlacementInput()
    {
        if (!placementPoseIsValid)
            return;

        bool pressed = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        // PC / Editor / Simulator → use left mouse click
        if (Input.GetMouseButtonDown(0))
            pressed = true;
#else
    // Mobile / Device → use touch
    if (Input.touchCount > 0)
    {
        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
            pressed = true;
    }
#endif

        if (!pressed)
            return;

        // ---------------------------------------------------
        // PLACE the object
        // ---------------------------------------------------
        if (spawnedObject == null)
        {
            Vector3 placementPosition = placementPose.position + m_cameraOffset;

            spawnedObject = Instantiate(objectPrefab, placementPosition, Quaternion.identity);

            DisablePlaneVisuals();

            if (spawnedObject != null)
            {
                // pass reference to UI bridge
                //m_uiBridge.SetManifoldReference(spawnedObject);

                // call controller placement
                spawnedObject.OnPlaced(placementPose, Camera.main);

                Debug.Log("[Placer] OnPlaced() called successfully");
            }

            // disable placer - placement done
            this.enabled = false;
        }
    }


    // ----------------------------------------------
    // 3. Disable mesh visuals (keeps AR plane detection alive)
    // ----------------------------------------------
    void DisablePlaneVisuals()
    {
        foreach (var plane in planeMgr.trackables)
            plane.gameObject.SetActive(false);
    }
}
