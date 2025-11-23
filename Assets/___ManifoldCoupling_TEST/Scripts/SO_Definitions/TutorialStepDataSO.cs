using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Tutorial Step Data", fileName = "TutorialStep_")]
public class TutorialStepDataSO : ScriptableObject
{
    [Header("UI Text")]
    public string stepTitle;
    [TextArea(2, 4)]
    public string stepDescription;

    [Header("Indicator Text")]
    public string successLabel = "OK";
    public string failureLabel = "Not Ready";

    [Header("Requirements")]
    public bool requireAlignment;
    public bool requireSnap;
    public bool requireHandleLock;
    public bool requireFlowStart;
    public bool requireHandleUnlock;
    public bool requireUnSnap;
    public bool requireUnAlign;


    [Header("Hints")]
    public bool showArrows;
    public bool showHandAnimation;

    [Header("Auto-Advance")]
    public bool autoCompleteWhenRequirementMet = false;
}
