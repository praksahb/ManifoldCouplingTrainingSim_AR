using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceObjectOnPlane : MonoBehaviour
{
    [Header("XR Origin Root Reference")]
    [SerializeField] private GameObject xrOrigin;   // only one reference

    [Header("Prefab to Place")]
    [SerializeField] private GameObject objectToPlace;

    [SerializeField] private int maxObjectCount = 5;  // how many allowed (set via inspector)
    private int placementCount = 0;                      // how many placed so far


    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;

    private GameObject placedObject;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isPlaced = false;

    // Central log function requested
    private void Log(string msg)
    {
        Debug.Log("[AR] " + msg);
    }

    private void Awake()
    {
        if (!xrOrigin)
        {
            Log("XR Origin reference missing!");
            return;
        }

        // Get both managers using only two GetComponent calls total
        raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        planeManager = xrOrigin.GetComponent<ARPlaneManager>();

        if (!raycastManager) Log("ARRaycastManager missing on XR Origin!");
        if (!planeManager) Log("ARPlaneManager missing on XR Origin!");

        if (!objectToPlace) Log("No objectToPlace assigned!");
    }

    private void Update()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.Planes))
        {
            var hit = hits[0];
            Pose pose = hit.pose;

            // DEBUG INFO BLOCK (still included)
            var plane = hit.trackable as ARPlane;
            if (plane != null)
            {
                Log("Plane Hit Info:");
                Log(" - Plane ID: " + plane.trackableId);
                Log(" - Alignment: " + plane.alignment);
                Log(" - Center: " + plane.center);
                Log(" - Size: " + plane.size);
                Log(" - Boundary Points: " + plane.boundary.Length);
                Log(" - Trackable GO: " + plane.gameObject.name);
            }

            // =======================
            // PLACE MULTIPLE OBJECTS
            // =======================
            if (maxObjectCount < 0 || placementCount < maxObjectCount)
            {
                // Instantiate a NEW object every tap
                GameObject newObj = Instantiate(objectToPlace, pose.position, pose.rotation);
                placementCount++;

                Log("Placed object instance #" + placementCount + " at " + pose.position);

                // Optionally hide plane visuals only once (on first placement)
                if (placementCount == 1)
                    HidePlaneVisualsOnly();
            }
            else
            {
                Log("Placement limit reached (" + maxObjectCount + ")");
            }
        }
        else
        {
            Log("Raycast MISSED");
        }

    }

    private void HidePlaneVisualsOnly()
    {
        if (planeManager == null)
        {
            Log("PlaneManager missing — cannot hide visuals.");
            return;
        }

        foreach (var plane in planeManager.trackables)
        {
            // Disable ONLY display — keep detection active
            plane.gameObject.SetActive(false);
        }

        Log("All plane visuals hidden, detection still active.");
    }


    private void DisablePlanes()
    {
        if (!planeManager)
            return;

        planeManager.enabled = false;

        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(false);

        Log("Planes disabled and hidden.");
    }
}
