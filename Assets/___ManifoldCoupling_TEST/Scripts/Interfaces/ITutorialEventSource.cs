using Vector3 = UnityEngine.Vector3;

public interface ITutorialEventSource
{
    public event System.Action<Vector3> OnConnectorMoved;
    public event System.Action OnSnapAligned;
    public event System.Action<float> OnHandleRotated;   // angle passed in
    public event System.Action OnFlowStarted;
    public event System.Action OnUnlocked;
    public event System.Action OnDisconnected;
}
