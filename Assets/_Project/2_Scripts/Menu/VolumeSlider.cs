using UnityEngine;
using UnityEngine.UI;

// Saves the slider value. Any AudioSource with a LoopVolume script follows it.
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public const string SaveKey = "LoopVolume";

    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SaveKey, v));
    }

    void OnEnable()
    {
        // Show the saved volume whenever the slider appears.
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SaveKey, 1f));
    }
}
