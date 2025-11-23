using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public enum GameState
{
    Login,
    Prespawning,
    Spawning,
    TutorialMode,
    AssessmentMode,
    Paused,
    Completed
}

public class GameManager : Singleton<GameManager>
{
    // ------------------------------------------------------------
    //  PROPERTIES
    // ------------------------------------------------------------

    public GameState CurrentState { get; private set; } = GameState.Login;

    public ManifoldMainController CurrentManifold { get; set; }

    public Camera MainCamera => _mainCamera;
    public ARRaycastManager RaycastManager => _raycastManager;
    public ARPlaneManager PlaneManager => _planeManager;

    public event Action<GameState, GameState> OnGameStateChanged;

    // ------------------------------------------------------------
    //  REFERENCES
    // ------------------------------------------------------------

    [Header("Global AR References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private XROrigin _xrOrigin;
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private ARPlaneManager _planeManager;

    // ------------------------------------------------------------
    //  UNITY LIFECYCLE
    // ------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        AutoFindReferences();
    }

    private void Start()
    {
        // Start fully idle → LoginUI appears via its own script
        SetState(GameState.Login);
    }

    // ------------------------------------------------------------
    //  STATE MACHINE
    // ------------------------------------------------------------

    public void SetState(GameState newState)
    {
        if (newState == CurrentState)
            return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameManager] State changed: {oldState} → {newState}");

        OnGameStateChanged?.Invoke(oldState, newState);

        if (newState == GameState.Completed)
        {
            // The SaveManager handles the saving — GameManager stays clean
            SaveManager.Instance.Save();
        }
    }

    // ------------------------------------------------------------
    //  HELPERS
    // ------------------------------------------------------------

    private void AutoFindReferences()
    {
        if (_mainCamera == null && Camera.main != null)
            _mainCamera = Camera.main;

        if (_xrOrigin == null)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                _xrOrigin = origin;
        }

        if (_xrOrigin != null)
        {
            if (_raycastManager == null)
                _raycastManager = _xrOrigin.GetComponent<ARRaycastManager>();

            if (_planeManager == null)
                _planeManager = _xrOrigin.GetComponent<ARPlaneManager>();
        }
    }

    // ------------------------------------------------------------
    //  CALLED WHEN TUTORIAL FINISHES
    // ------------------------------------------------------------

    public void OnTutorialFinished()
    {
        Debug.Log("[GameManager] Tutorial finished — Show popup or go to Assessment.");
    }
}
