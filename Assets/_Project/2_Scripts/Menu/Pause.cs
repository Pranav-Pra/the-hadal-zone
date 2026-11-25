using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the root of the SettingsCanvas prefab (replaces the earlier version).
// Showing the canvas pauses the game; hiding it resumes.
public class Pause : MonoBehaviour
{
    // Gameplay scripts check this: if (PauseWhileOpen.IsPaused) return;
    public static bool IsPaused { get; private set; }

    [SerializeField] string mainMenuSceneName = "MainScene"; // must match your menu scene's name
    [SerializeField] bool pauseAudio = true;
    [SerializeField] GameObject volumeDropdown;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] float clickVolume = 1f;  

     // HUD settings button -> OnClick
    public void Open()
    {
        gameObject.SetActive(true);
        PauseGame(); // pause explicitly, even if the canvas was already active
    }

    // Resume button -> OnClick
    public void Resume()
    {
        PlayClick();
        gameObject.SetActive(false); // the AudioSource lives elsewhere, so hiding the canvas doesn't cut the sound
    }

    // Options button -> OnClick
    public void ToggleOptions()
    {
        PlayClick();
        if (volumeDropdown != null)
            volumeDropdown.SetActive(!volumeDropdown.activeSelf);
    }

    // Quit button -> OnClick. Back to the main menu; the Hadal scene and all its progress are thrown away.
    public void QuitGame()
    {
        PlayClick();
        StartCoroutine(QuitAfterClick());
    }

    // Wait for the click to finish, or the scene change cuts it off.
    // Realtime, because Time.timeScale is 0 while paused (Invoke and WaitForSeconds would wait forever).
    IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(clickSound.length);
        gameObject.SetActive(false); // unpauses first (OnDisable) so the menu doesn't load frozen
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void PlayClick()
    {
        audioSource.ignoreListenerPause = true; // otherwise AudioListener.pause silences the click
        audioSource.PlayOneShot(clickSound, clickVolume);
    }

    void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;
    }

    void OnEnable()
    {
        PauseGame();
        if (volumeDropdown != null) volumeDropdown.SetActive(false); // start collapsed every time the menu opens
    }

    void OnDisable()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

}