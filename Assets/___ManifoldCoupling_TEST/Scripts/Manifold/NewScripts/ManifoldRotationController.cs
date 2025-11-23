using UnityEngine;

/// <summary>
/// Light-weight rotation controller used by the root.  
/// It wraps the offset rig rotation calls so the bridge can call a single component.
/// </summary>
[DisallowMultipleComponent]
public class ManifoldRotationController : MonoBehaviour
{
    [SerializeField] private ManifoldPlacementRig _offsetRig;
    [SerializeField] private ManifoldConnectorController _connectorController;

    [SerializeField] private float _rotateSpeedDegPerSec = 90f;

    void Update() { /* Intentionally empty - rotation driven by bridge per-frame */ }

    public void StartRotateLeft()
    {
        // just records start; actual per-frame rotation is driven by UI bridge calling ApplyRotateLeft per-frame
    }

    public void StopRotateLeft() { /* no-op: snap handled by offset rig or connector */ }

    public void StartRotateRight() { }

    public void StopRotateRight() { }

    // Called from bridge while hold is active to apply smooth frame delta rotation
    public void ApplyRotateLeft(float deltaTime)
    {
        //if (_offsetRig != null)
        //    _offsetRig.RotateYaw(-_rotateSpeedDegPerSec * deltaTime);
    }

    public void ApplyRotateRight(float deltaTime)
    {
        //if (_offsetRig != null)
        //    _offsetRig.RotateYaw(_rotateSpeedDegPerSec * deltaTime);
    }

    // For connector-specific holds (tutorial/assessment)
    public void ApplyConnectorRotateLeft(float deltaTime)
    {
        //if (_connectorController != null)
            //_connectorController.RotateConnectorHold(-_rotateSpeedDegPerSec * deltaTime);
    }

    public void ApplyConnectorRotateRight(float deltaTime)
    {
        //if (_connectorController != null)
            //_connectorController.RotateConnectorHold(_rotateSpeedDegPerSec * deltaTime);
    }
}
