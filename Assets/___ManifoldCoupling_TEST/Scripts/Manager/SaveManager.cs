using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveModel
{
    public string traineeName;
    public string traineeId;
    public string lastLoginDate;
}
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string _savePath;

    public SaveModel Data { get; private set; } = new SaveModel();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, "user_data.json");
        Load();
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(_savePath, json);
            Debug.Log($"[SaveManager] Saved to {_savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Save failed: {ex}");
        }
    }

    public void Load()
    {
        if (!File.Exists(_savePath))
        {
            Debug.Log("[SaveManager] No save file found, creating new.");
            Data = new SaveModel();
            return;
        }

        try
        {
            string json = File.ReadAllText(_savePath);
            Data = JsonUtility.FromJson<SaveModel>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Load failed: {ex}");
            Data = new SaveModel();
        }
    }
}
