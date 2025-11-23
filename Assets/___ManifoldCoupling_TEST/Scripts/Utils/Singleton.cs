using UnityEngine;

/// <summary>
/// Generic singleton template.
/// Attach this to any manager script: public class GameManager : Singleton<GameManager>
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _shuttingDown = false;
    private static object _lock = new object();

    [Header("Singleton Settings")]
    [Tooltip("If true, this singleton persists between scene loads.")]
    public bool persistent = true;

    /// <summary>
    /// Global access to this singleton instance.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // attempt find in scene
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        Debug.LogError($"[Singleton] No instance of {typeof(T)} found in scene.");
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (persistent)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate {typeof(T)} found. Destroying new one.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _shuttingDown = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _shuttingDown = true;
    }
}
