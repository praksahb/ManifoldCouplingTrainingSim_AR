public interface IInputLayer
{
    void MoveForward();
    void MoveBackward();
    void MoveLeft();
    void MoveRight();
    void MoveUp();
    void MoveDown();

    void RotateLeft();
    void RotateRight();

    void Confirm();
    void Cancel();
}
