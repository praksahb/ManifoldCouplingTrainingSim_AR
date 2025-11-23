using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central input router.  
/// Binds input to gameplay controllers based on GameState,
/// with safe override hooks for Tutorial steps.
/// </summary>
[DisallowMultipleComponent]
public class InputRouter : Singleton<InputRouter>
{
    [Header("References")]
    [SerializeField] private InputController_UI _ui;

    public void HandleUnSnapControl()
    {
    }

    private ManifoldMainController _mainController;

    // Delegates for routing
    private Action<Vector3> _onDirectionalMove;
    private Action<float> _onRotateY;
    private Action<float> _onMoveVertical;
    private Action<float> _onHandleRotate;
    private Action _onConfirmPlacement;
    private Action _onSnapPressed;
    private Action _onUnsnapPressed;

    protected override void Awake()
    {
        base.Awake();
        if (_ui == null)
            _ui = FindFirstObjectByType<InputController_UI>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;

        if (_ui != null)
        {
            _ui.OnDirectionalMove += RouteDirectionalMove;
            _ui.OnRotateY += RouteRotateY;
            _ui.OnMoveVertical += RouteMoveVertical;
            _ui.OnHandleRotate += RouteHandleRotate;
            _ui.OnConfirmPlacement += RouteConfirmPlacement;
            _ui.OnSnapPressed += RouteSnapPressed;
            _ui.OnUnSnapPressed += RouteUnsnapPressed;   // NEW
        }

        if (GameManager.Instance)
            HandleStateChanged(GameState.Login, GameManager.Instance.CurrentState);
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;

        if (_ui != null)
        {
            _ui.OnDirectionalMove -= RouteDirectionalMove;
            _ui.OnRotateY -= RouteRotateY;
            _ui.OnMoveVertical -= RouteMoveVertical;
            _ui.OnHandleRotate -= RouteHandleRotate;
            _ui.OnConfirmPlacement -= RouteConfirmPlacement;
            _ui.OnSnapPressed -= RouteSnapPressed;
            _ui.OnUnSnapPressed -= RouteUnsnapPressed;
        }
    }

    // ===============================================================
    // STATE BINDINGS
    // ===============================================================
    private void HandleStateChanged(GameState prev, GameState next)
    {
        TryRefreshManifold();
        ClearBindings();

        switch (next)
        {
            case GameState.Spawning:
                BindSpawning();
                break;

            case GameState.TutorialMode:
                BindTutorial();
                break;

            case GameState.AssessmentMode:
                BindAssessment();
                break;

            default:
                ClearBindings();
                break;
        }

        Debug.Log($"[InputRouter] State changed → Rebound for {next}");
    }

    // ---------------------------
    // SPAWNING
    // ---------------------------
    private void BindSpawning()
    {
        if (_mainController == null) return;

        _onDirectionalMove = (dir) => _mainController.NudgePlacementLocal(dir);
        _onRotateY = (v) => _mainController.RotatePlacementSmooth(v);
        _onMoveVertical = (v) => _mainController.NudgePlacementVertical(v);

        _onConfirmPlacement = () =>
        {
            GameManager.Instance.SetState(GameState.TutorialMode);
        };
    }

    // ---------------------------
    // TUTORIAL
    // ---------------------------
    private void BindTutorial()
    {
        if (_mainController == null) return;

        _onDirectionalMove = (dir) => _mainController.NudgeConnectorLocal(dir);
        _onRotateY = (v) => _mainController.RotateConnectorSmooth(v);
        _onMoveVertical = null;

        _onHandleRotate = (v) => _mainController.RotateHandle(v);

        _onSnapPressed = () => _mainController.ManualSnap();
        _onUnsnapPressed = () => _mainController.ManualUnsnap();
    }

    // ---------------------------
    // ASSESSMENT
    // ---------------------------
    private void BindAssessment()
    {
        if (_mainController == null) return;

        _onDirectionalMove = (dir) => _mainController.NudgeConnectorLocal(dir);
        _onRotateY = (v) => _mainController.RotateConnectorSmooth(v);
        _onMoveVertical = null;
        _onHandleRotate = (v) => _mainController.RotateHandle(v);

        _onSnapPressed = () => _mainController.ManualSnap();
        _onUnsnapPressed = () => _mainController.ManualUnsnap();
    }

    private void TryRefreshManifold()
    {
        if (_mainController == null && GameManager.Instance != null)
            _mainController = GameManager.Instance.CurrentManifold;
    }

    private void ClearBindings()
    {
        _onDirectionalMove = null;
        _onRotateY = null;
        _onMoveVertical = null;
        _onHandleRotate = null;
        _onConfirmPlacement = null;
        _onSnapPressed = null;
        _onUnsnapPressed = null;
    }

    // ===============================================================
    // PUBLIC TUTORIAL OVERRIDE API  (NEW)
    // ===============================================================
    public void OverrideDirectionalMove(Action<Vector3> handler) => _onDirectionalMove = handler;
    public void OverrideRotationY(Action<float> handler) => _onRotateY = handler;
    public void OverrideVerticalMove(Action<float> handler) => _onMoveVertical = handler;
    public void OverrideHandleRotation(Action<float> handler) => _onHandleRotate = handler;
    public void OverrideSnap(Action handler) => _onSnapPressed = handler;
    public void OverrideUnsnap(Action handler) => _onUnsnapPressed = handler;

    public void RestoreDefaultTutorialBindings() => BindTutorial();

    public void RouteDirectionalUpDownToHandleRotation()
    {
        // Disable connector movement
        _onDirectionalMove = null;
        _onMoveVertical = null;

        // Forward UI's RotateY input -> Handle rotation
        _onRotateY = (value) =>
        {
            if (_mainController != null)
                _mainController.RotateHandle(value);
        };
    }

    public void RouteToSnapButton()
    {
        _ui.SnapInvoked();
    }
    public void RouteToUnsnapButton()
    {
        _ui.UnSnapInvoked();
    }


    // ===============================================================
    // ROUTE METHODS (UI → Bound delegate)
    // ===============================================================
    private void RouteRotateY(float v) => _onRotateY?.Invoke(v);
    private void RouteDirectionalMove(Vector3 v) => _onDirectionalMove?.Invoke(v);
    private void RouteMoveVertical(float v) => _onMoveVertical?.Invoke(v);
    private void RouteHandleRotate(float v) => _onHandleRotate?.Invoke(v);
    private void RouteConfirmPlacement() => _onConfirmPlacement?.Invoke(); 
    private void RouteSnapPressed() => _onSnapPressed?.Invoke();
    private void RouteUnsnapPressed() => _onUnsnapPressed?.Invoke();

    // Extra

    public void DisableAllControls()
    {
        _onDirectionalMove = null;
        _onRotateY = null;
        _onMoveVertical = null;
        _onHandleRotate = null;
    }
}
