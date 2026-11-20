using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Hadal"; // must match your gameplay scene's name exactly
    [SerializeField] GameObject volumeDropdown;       // the panel that holds the slider

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        volumeDropdown.SetActive(false);
    }

    // New Game button -> OnClick
    public void StartNewGame() => SceneManager.LoadScene(gameSceneName); // always a fresh copy

    // Options button -> OnClick
    public void ToggleOptions() => volumeDropdown.SetActive(!volumeDropdown.activeSelf);
}
