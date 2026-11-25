using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Hadal"; // must match your gameplay scene's name exactly
    [SerializeField] GameObject volumeDropdown;       // the panel that holds the slider

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clickSound;   
    [SerializeField, Range(0f, 1f)] float clickVolume = 1f;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        volumeDropdown.SetActive(false);
    }

    // New Game button -> OnClick
    public void StartNewGame()
    {
        audioSource.PlayOneShot(clickSound, clickVolume);
        Invoke(nameof(LoadGame), clickSound.length); // wait for the sound, or the scene change cuts it off
    }

    void LoadGame() => SceneManager.LoadScene(gameSceneName);

    // Options button -> OnClick
    public void ToggleOptions()
    {
        audioSource.PlayOneShot(clickSound);
        volumeDropdown.SetActive(!volumeDropdown.activeSelf);
    }
}
