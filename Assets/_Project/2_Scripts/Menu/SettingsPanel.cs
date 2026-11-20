using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] Slider volumeSlider;      // min 0, max 1
    [SerializeField] Toggle fullscreenToggle;

    GameObject returnTo; // the panel to show again when Back is pressed

    void Awake()
    {
        volumeSlider.onValueChanged.AddListener(v => GameSettings.Volume = v);
        fullscreenToggle.onValueChanged.AddListener(on => GameSettings.Fullscreen = on);
    }

    void OnEnable()
    {
        // Show the saved values without firing the listeners
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
    }

    public bool IsOpen => gameObject.activeSelf;

    public void Open(GameObject from)
    {
        returnTo = from;
        if (from) from.SetActive(false);
        gameObject.SetActive(true);
    }

    // Hook the Settings panel's Back button to this.
    public void Back()
    {
        GameSettings.Save();
        gameObject.SetActive(false);
        if (returnTo) returnTo.SetActive(true);
    }
}