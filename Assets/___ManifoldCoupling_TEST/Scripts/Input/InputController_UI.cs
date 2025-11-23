using System;
using UnityEngine;

public class InputController_UI : MonoBehaviour
{
    // -------------------------------------------------------
    // Movement Mapping
    // -------------------------------------------------------
    [SerializeField] private Vector3 _moveUp = new(-1, 0, 0);
    [SerializeField] private Vector3 _moveDown = new(1, 0, 0);
    [SerializeField] private Vector3 _moveLeft = new(0, 0, -1);
    [SerializeField] private Vector3 _moveRight = new(1, 0, 1);

    // -------------------------------------------------------
    // Movement events
    // -------------------------------------------------------
    public event Action<Vector3> OnDirectionalMove;   // x,y for left/right, forward/back
    public event Action<float> OnRotateY;             // +1 / -1
    public event Action<float> OnMoveVertical;        // +1 / -1

    // -------------------------------------------------------
    // Handle events
    // -------------------------------------------------------
    public event Action<float> OnHandleRotate;        // +1 (CW) or -1 (CCW)

    // -------------------------------------------------------
    // Step / confirm events
    // -------------------------------------------------------
    public event Action OnConfirmPlacement; // Shifts to Tutorail Mode via state change in GameManager
    //public event Action OnNextStep; // Handling at runtime through UIController and TutorialController
    public event Action OnSnapPressed, OnUnSnapPressed;

    // ---------------- Button Wiring -------------------------

    public void MoveUp() => OnDirectionalMove?.Invoke(_moveUp);
    public void MoveDown() => OnDirectionalMove?.Invoke(_moveDown);
    public void MoveLeft() => OnDirectionalMove?.Invoke(_moveLeft);
    public void MoveRight() => OnDirectionalMove?.Invoke(_moveRight);

    public void RotateLeft() => OnRotateY?.Invoke(-1);
    public void RotateRight() => OnRotateY?.Invoke(+1);

    public void MoveHeightUp() => OnMoveVertical?.Invoke(+1);
    public void MoveHeightDown() => OnMoveVertical?.Invoke(-1);

    public void HandleRotateCW() => OnHandleRotate?.Invoke(+1);
    public void HandleRotateCCW() => OnHandleRotate?.Invoke(-1);

    public void ConfirmPlacement() => OnConfirmPlacement?.Invoke();
    //public void NextStep() => OnNextStep?.Invoke();

    public void SnapInvoked() => OnSnapPressed?.Invoke();
    public void UnSnapInvoked() => OnUnSnapPressed?.Invoke();
}
