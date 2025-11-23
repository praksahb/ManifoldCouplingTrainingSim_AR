using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class TutorialUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _snapButton;
    [SerializeField] private StatusIndicatorUI _indicatorUI;
    [SerializeField] private GameObject _tutorialCompletePanel;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _assessmentButton;

    private TutorialController _tutorial;

    private void Start()
    {

        _snapButton.gameObject.SetActive(false);

        _tutorialCompletePanel.SetActive(false);
    }

    public void Init(TutorialController controller)
    {
        _tutorial = controller;

        _nextButton.onClick.AddListener(OnNextClicked);
        _snapButton.onClick.AddListener(DisableSnapButton);

        _restartButton.onClick.AddListener(() =>
        {
            _tutorial.RestartTutorial();
            DisableTutorialPanel();
        });

        _assessmentButton.onClick.AddListener(() =>
        {
            _tutorial.BeginAssessment();
            DisableTutorialPanel();
        });
    }

    public void UpdateStepUI(TutorialStepDataSO step)
    {
        _titleText.text = step.stepTitle;
        _descriptionText.text = step.stepDescription;

        // default indicator state = failure
        _indicatorUI.SetFailure(step.failureLabel);

        _nextButton.interactable = false;
    }

    public void SetStepCompleted(string stepLabel)
    {
        _nextButton.interactable = true;
        _indicatorUI.SetSuccess(stepLabel);
    }

    public void SetStepNotCompleted(TutorialStepDataSO step)
    {
        _nextButton.interactable = false;
        _indicatorUI.SetFailure(step.failureLabel);
    }

    private void OnNextClicked()
    {
        _tutorial.NextStep();
    }

    public void EnableSnapButton()
    {
        ChangeSnapBtnText("SNap!");

        _snapButton.onClick.RemoveAllListeners();
        if (InputRouter.Instance)
            _snapButton.onClick.AddListener(InputRouter.Instance.RouteToSnapButton);

        _snapButton.gameObject.SetActive(true);
        _snapButton.onClick.AddListener(DisableSnapButton);
        _snapButton.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutSine);

    }

    public void EnableUnsnapButton()
    {
        ChangeSnapBtnText("Unsnap!");

        _snapButton.onClick.RemoveAllListeners();
        if (InputRouter.Instance)
            _snapButton.onClick.AddListener(InputRouter.Instance.RouteToUnsnapButton);


        _snapButton.gameObject.SetActive(true);
        _snapButton.onClick.AddListener(DisableSnapButton);
        _snapButton.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutSine);

    }

    private void DisableSnapButton()
    {
        _snapButton.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            _snapButton.gameObject.SetActive(false);
        });
    }

    private void ChangeSnapBtnText(string text)
    {
        var btnTxt = _snapButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        if (btnTxt)
        {
            btnTxt.SetText(text);
        }
    }

    public void ShowTutorialCompletePopup()
    {

        // Hide step UI
        _nextButton.gameObject.SetActive(false);
        _descriptionText.gameObject.SetActive(false);
        _titleText.gameObject.SetActive(false);

        // Show final popup (you create this as a panel in Unity)
        _tutorialCompletePanel.SetActive(true);

        // Animate
        _tutorialCompletePanel.transform.localScale = Vector3.zero;
        _tutorialCompletePanel.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
    }

    private void DisableTutorialPanel()
    {
        _tutorialCompletePanel.transform.DOScale(Vector3.zero, 0.45f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            _tutorialCompletePanel.SetActive(false);

            _nextButton.gameObject.SetActive(true);
            _descriptionText.gameObject.SetActive(true);
            _titleText.gameObject.SetActive(true);
        });
    }
}
