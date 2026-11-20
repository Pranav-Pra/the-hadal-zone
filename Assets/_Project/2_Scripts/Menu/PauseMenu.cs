using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the gameplay Canvas itself (an object that is ALWAYS active),
public class PauseMenu : MonoBehaviour
{
    //
    public static bool IsPaused { get; private set; }

    [SerializeField] GameObject pausePanel;          // Resume / Settings / Main Menu buttons
    [SerializeField] SettingsPanel settingsPanel;
    [SerializeField] string menuSceneName = "MainMenu";

    void Start()
    {
        SetPaused(false);
    }

    void Update()
    {
        if (!EscapePressed()) return;

        if (settingsPanel.IsOpen)
            settingsPanel.Back();      // Esc in settings goes back to the pause panel
        else
            SetPaused(!IsPaused);
    }

    bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;   // freezes physics, deltaTime, WaitForSeconds, Animators on Normal update
        AudioListener.pause = paused;        // pauses game sounds (UI sources can set ignoreListenerPause = true)

        pausePanel.SetActive(paused);
        if (!paused) settingsPanel.gameObject.SetActive(false);
    }

    // Button OnClick hooks
    public void Resume() => SetPaused(false);

    public void OpenSettings() => settingsPanel.Open(pausePanel);

    public void QuitToMainMenu()
    {
        SetPaused(false); // must reset timeScale or the next scene loads frozen
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy()
    {
        // IsPaused is static
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
