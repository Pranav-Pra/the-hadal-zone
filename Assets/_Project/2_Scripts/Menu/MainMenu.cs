using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "MainScene"; // must match the scene name in the build's scene list
    [SerializeField] GameObject mainPanel;               // holds Start / Settings / Quit buttons
    [SerializeField] SettingsPanel settingsPanel;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        settingsPanel.gameObject.SetActive(false);
        mainPanel.SetActive(true);
    }

    // Button OnClick hooks
    public void StartGame() => SceneManager.LoadScene(gameSceneName);

    public void OpenSettings() => settingsPanel.Open(mainPanel);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}