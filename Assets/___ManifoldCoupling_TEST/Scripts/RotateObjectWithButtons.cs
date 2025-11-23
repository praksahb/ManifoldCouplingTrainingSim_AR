using UnityEngine;

public class RotateObjectWithButtons : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f; // degrees per second


    // Called by UI button
    public void RotateLeft()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void RotateRight()
    {
        transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
    }

    public void RotateUp()
    {
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void RotateDown()
    {
        transform.Rotate(Vector3.right, -rotationSpeed * Time.deltaTime, Space.World);
    }

    // Optional reset function
    public void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
    }
}
