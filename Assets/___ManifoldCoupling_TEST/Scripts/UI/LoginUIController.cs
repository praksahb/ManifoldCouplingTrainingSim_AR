using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private Button _startButton;
    [SerializeField] private GameObject _rootPanel;

    private void Start()
    {
        _startButton.onClick.AddListener(OnStartPressed);

        // Autofill if user already logged in before
        if (!string.IsNullOrEmpty(SaveManager.Instance.Data.traineeName))
        {
            _nameInput.text = SaveManager.Instance.Data.traineeName;
            _idInput.text = SaveManager.Instance.Data.traineeId;
        }

        _rootPanel.SetActive(true);
    }

    private void OnStartPressed()
    {
        string name = _nameInput.text.Trim();
        string id = _idInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Name cannot be empty!");
            return;
        }
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Id cannot be empty!");
            return;
        }

        // Save user info
        SaveManager.Instance.Data.traineeName = name;
        SaveManager.Instance.Data.traineeId = id;
        SaveManager.Instance.Data.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        SaveManager.Instance.Save();

        // Hide login panel
        _rootPanel.SetActive(false);

        // Move to spawning mode
        GameManager.Instance.SetState(GameState.Prespawning);
    }
}
