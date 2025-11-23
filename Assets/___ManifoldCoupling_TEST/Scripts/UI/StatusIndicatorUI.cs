using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatusIndicatorUI : MonoBehaviour
{
    [SerializeField] private Image _indicatorImage;
    [SerializeField] private TextMeshProUGUI _indicatorText;

    private readonly Color _green = new Color(0.1f, 0.9f, 0.1f, 1f);
    private readonly Color _red = new Color(0.9f, 0.1f, 0.1f, 1f);
    private readonly string _statusText = "Status:   ";

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += ToggleStatusIndicator;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= ToggleStatusIndicator;
    }

    private void ToggleStatusIndicator(GameState oldState, GameState newState)
    {
        if (newState == GameState.TutorialMode || newState == GameState.AssessmentMode)
        {
            ToggleStatus_UI(true);
        }
        else
        {
            ToggleStatus_UI(false);
        }
    }

    private void ToggleStatus_UI(bool toggle)
    {
        _indicatorImage.enabled = toggle;
        _indicatorText.enabled = toggle;
    }


    public void SetSuccess(string label)
    {
        _indicatorImage.color = _green;
        SetDefaultText(label);
    }

    public void SetFailure(string label)
    {
        _indicatorImage.color = _red;
        SetDefaultText(label);
    }

    private void SetDefaultText(string label)
    {
        _indicatorText.SetText(_statusText + label);
    }
}
