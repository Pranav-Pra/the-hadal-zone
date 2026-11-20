using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the root of the SettingsCanvas prefab (replaces the earlier version).
// Showing the canvas pauses the game; hiding it resumes.
public class Pause : MonoBehaviour
{
    // Gameplay scripts check this: if (PauseWhileOpen.IsPaused) return;
    public static bool IsPaused { get; private set; }

    [SerializeField] string mainMenuSceneName = "MainMenu"; // must match your menu scene's name
    [SerializeField] bool pauseAudio = true;

    // HUD settings button -> OnClick
    public void Open()
    {
        gameObject.SetActive(true);
        PauseGame(); // pause explicitly, even if the canvas was already active
    }

    // Resume button -> OnClick
    public void Resume() => gameObject.SetActive(false);

    void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;
    }

    // Quit button -> OnClick. Back to the main menu; the Hadal scene and all its progress are thrown away.
    public void QuitGame()
    {
        gameObject.SetActive(false); // unpauses first (OnDisable) so the menu doesn't load frozen
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnEnable() => PauseGame();

    void OnDisable()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}