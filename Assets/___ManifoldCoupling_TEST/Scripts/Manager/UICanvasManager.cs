using UnityEngine;

public class UICanvasManager : MonoBehaviour
{
    [Header("Canvas Roots")]
    [SerializeField] private GameObject _spawningCanvasRoot;
    [SerializeField] private GameObject _trainingCanvasRoot;
    [SerializeField] private GameObject _assessmentCanvasRoot;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UICanvasManager] GameManager not found.");
            enabled = false;
            return;
        }

        // initial sync
        ApplyState(GameManager.Instance.CurrentState);

        GameManager.Instance.OnGameStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(GameState state)
    {
        _spawningCanvasRoot.SetActive(state == GameState.Spawning);
        _trainingCanvasRoot.SetActive(state == GameState.TutorialMode);
        _assessmentCanvasRoot.SetActive(state == GameState.AssessmentMode);

        if (state == GameState.Paused || state == GameState.Completed)
        {
            _spawningCanvasRoot.SetActive(false);
            _trainingCanvasRoot.SetActive(false);
            _assessmentCanvasRoot.SetActive(false);
        }

        Debug.Log($"[UICanvasManager] Applied state: {state}");
    }
}
