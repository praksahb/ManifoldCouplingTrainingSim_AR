using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("Steps")]
    [SerializeField] private List<TutorialStepDataSO> _steps;

    [Header("UI")]
    [SerializeField] private TutorialUIController _uiController;

    private ManifoldMainController _mainController;

    //private ManifoldSnapController _snapController;
    //private ManifoldConnectorController _connectorController;
    //private ManifoldHandleController _handleController;

    private int _currentStepIndex = 0;
    private TutorialStepDataSO _currentStep;

    private bool _stepCompleted;

    private void OnEnable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged += StartTutorial;
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= StartTutorial;
    }


    private void StartTutorial(GameState oldState, GameState newState)
    {
        if (newState == GameState.TutorialMode)
        {
            _uiController.Init(this);
            SetupReferences();
            LoadStep(0);
            RegisterCallbacks();
        }
    }

    private void SetupReferences()
    {
        _mainController = GameManager.Instance.CurrentManifold;

        if (!_mainController)
        {
            Debug.LogError("Manifold reference is missing. Tutorial initialized before spawning?");
            return;
        }
        //_snapController = _mainController.SnapController;
        //_connectorController = _mainController.Connector;
        //_handleController = _mainController.HandleController;
    }

    private void RegisterCallbacks()
    {
        _mainController.SnapController.OnAlignmentChanged += HandleAlignmentCheck;
        _mainController.SnapController.OnSnapStateChanged += HandleSnapCheck;
        _mainController.HandleController.OnLockStateChanged += HandleLockCheck;
        _mainController.HandleController.OnUnlockStateChanged += HandleUnlockCheck;
        _mainController.SnapController.OnSnapStateChanged += HandleUnsnapCheck;
        _mainController.SnapController.OnAlignmentChanged += HandleUnalignCheck;
    }

    private void LoadStep(int index)
    {
        if (index < 0 || index >= _steps.Count)
        {
            Debug.Log("[Tutorial] Completed all steps!");
            return;
        }

        _currentStepIndex = index;
        _currentStep = _steps[index];
        _stepCompleted = false;

        ConfigureInputForStep(_currentStep);

        _uiController.UpdateStepUI(_currentStep);

        Debug.Log($"[Tutorial] Loaded step {index}: {_currentStep.stepTitle}");

        _currentStepIndex++;
    }

    private void ConfigureInputForStep(TutorialStepDataSO step)
    {
        var router = InputRouter.Instance;

        // STEP 1 — ALIGNMENT
        if (step.requireAlignment)
        {
            router.RestoreDefaultTutorialBindings();   // Connector movement only
            _mainController.EnableConnectorControl(true);
            _mainController.ToggleHandleControl(false);
            return;
        }

        // STEP 2 — SNAP (disable all movement, show SNAP button)
        if (step.requireSnap)
        {
            _uiController.EnableSnapButton();

            router.DisableAllControls();
            _mainController.ToggleHandleControl(false);
            return;
        }

        // STEP 3 — HANDLE LOCK
        if (step.requireHandleLock)
        {
            Debug.Log("Tutorial: Handle Lock Step Active");

            // disable connector movement
            router.DisableAllControls();

            // re-enable rotation but override input to handle
            router.RouteDirectionalUpDownToHandleRotation();
            _mainController.ToggleHandleControl(true);

            return;
        }

        // Step 5 - Handle Lock
        if (step.requireHandleUnlock)
        {
            router.RouteDirectionalUpDownToHandleRotation();
            _mainController.ToggleHandleControl(true);
            return;
        }


        // Handle - Unsnap
        if (step.requireUnSnap)
        {
            _uiController.EnableUnsnapButton();
            router.DisableAllControls();
            return;
        }

        // Handle - Un-align
        if (step.requireUnAlign)
        {
            router.RestoreDefaultTutorialBindings();
            return;
        }

    }

    private void MarkStepCompleted()
    {
        if (_stepCompleted) return;

        _stepCompleted = true;
        AudioManager.Instance.PlayStepCompleted();
        _uiController.SetStepCompleted(_currentStep.successLabel);

        Debug.Log($"[Tutorial] Step {_currentStepIndex} completed: {_currentStep.stepTitle}");

        if (_currentStep.autoCompleteWhenRequirementMet)
            NextStep();
    }

    private void MarkStepFailed()
    {
        if (_stepCompleted) return;

        _uiController.SetStepNotCompleted(_currentStep);
    }

    // -------------------------------------------------------
    // Step condition checks
    // -------------------------------------------------------

    private void HandleAlignmentCheck(bool isAligned)
    {
        // Normal alignment step
        if (_currentStep.requireAlignment)
        {
            if (isAligned)
            {
                AudioManager.Instance.PlayAlignmentSuccess();
                MarkStepCompleted();
            }
            else
            {
                AudioManager.Instance.PlayAlignmentFail();
                MarkStepFailed();
            }

            return;
        }

        // FINAL UNALIGN STEP
        if (_currentStep.requireUnAlign)
        {
            if (!isAligned)
            {
                
                MarkStepCompleted();
            }
            else
            {
                MarkStepFailed();
            }

            return;
        }
    }

    private void HandleSnapCheck(bool isSnapped)
    {
        if (!_currentStep.requireSnap) return;

        Debug.Log("Snapped is it? : :  " + isSnapped);

        if (isSnapped)
        {
            AudioManager.Instance.PlaySnap();
            MarkStepCompleted();
        }
        else MarkStepFailed();
    }

    private void HandleLockCheck(bool isLocked)
    {
        if (!_currentStep.requireHandleLock) return;

        if (isLocked)
        {
            AudioManager.Instance.PlayHandleLock();
            MarkStepCompleted();
        }
        else MarkStepFailed();
    }

    private void HandleUnlockCheck(bool isUnlocked)
    {
        //if (!_currentStep.requireDisconnection) return;

        if (isUnlocked)
        {
            AudioManager.Instance.PlayHandleUnlock();
            MarkStepCompleted();
        }
        else MarkStepFailed();
    }

    private void HandleUnsnapCheck(bool isSnapped)
    {
        if (!_currentStep.requireUnSnap) return;

        if (!isSnapped)
        {
            AudioManager.Instance.PlayUnsnap();
            MarkStepCompleted();
        }
        else
            MarkStepFailed();
    }

    private void HandleUnalignCheck(bool isAligned)
    {
        if (!_currentStep.requireUnAlign) return;

        if (!isAligned)
            MarkStepCompleted();
        else
            MarkStepFailed();
    }

    // -------------------------------------------------------
    // Move to next step
    // -------------------------------------------------------

    public void NextStep()
    {
        if (!_stepCompleted)
        {
            Debug.Log("[Tutorial] Step incomplete; cannot continue.");
            return;
        }

        // If current step index is already past final index → tutorial finished
        if (_currentStepIndex >= _steps.Count)
        {
            Debug.Log("[Tutorial] ALL STEPS COMPLETE");

            _uiController.ShowTutorialCompletePopup();   // <--- ADD
            GameManager.Instance.OnTutorialFinished();   // optional
            return;
        }

        LoadStep(_currentStepIndex);
    }


    public void RestartTutorial()
    {
        Debug.Log("[Tutorial] Restarting tutorial...");

        ResetAllControllers();
        LoadStep(0);
    }

    public void BeginAssessment()
    {
        Debug.Log("[Tutorial] Moving to assessment mode...");

        ResetAllControllers();
        GameManager.Instance.SetState(GameState.AssessmentMode);
    }

    private void ResetAllControllers()
    {
        // Reset handle, snap and connector
        _mainController.ResetAll();
        _mainController.ToggleHandleControl(false);

        // Reset routing
        InputRouter.Instance.RestoreDefaultTutorialBindings();
    }
}
