using UnityEngine;

public class ARFloatingAnimation : MonoBehaviour
{
    [Header("Floating Parameters")]
    public float floatAmplitude = 0.03f;     // How high it floats
    public float floatFrequency = 1.5f;      // How fast it floats

    [Header("Rotation Parameters")]
    public float rotationSpeed = 15f;        // Degrees per second

    [Header("Wander Parameters")]
    public float wanderRadius = 0.02f;       // Small random movement radius
    public float wanderSpeed = 0.4f;         // How fast it wanders

    [Header("Pulse Scale")]
    public bool enablePulsing = true;
    public float pulseAmplitude = 0.03f;
    public float pulseFrequency = 2.0f;

    private Vector3 startPos;
    private Vector3 randomOffset;

    void Start()
    {
        startPos = transform.localPosition;

        // Assign a random wander seed so each object floats differently
        randomOffset = new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            Random.Range(-wanderRadius, wanderRadius),
            Random.Range(-wanderRadius, wanderRadius)
        );
    }

    void Update()
    {
        float time = Time.time;

        // -------------------------
        // Vertical Float (Sin Wave)
        // -------------------------
        float newY = startPos.y + Mathf.Sin(time * floatFrequency) * floatAmplitude;

        // -------------------------
        // Random Gentle Wander
        // -------------------------
        float wanderX = Mathf.Sin(time * wanderSpeed) * randomOffset.x;
        float wanderZ = Mathf.Cos(time * wanderSpeed) * randomOffset.z;

        transform.localPosition = new Vector3(
            startPos.x + wanderX,
            newY,
            startPos.z + wanderZ
        );

        // -------------------------
        // Continuous Rotation
        // -------------------------
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // -------------------------
        // Optional Scale Pulsing
        // -------------------------
        if (enablePulsing)
        {
            float scale = 1f + Mathf.Sin(time * pulseFrequency) * pulseAmplitude;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
