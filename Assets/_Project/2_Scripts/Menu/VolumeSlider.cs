using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(v => AudioListener.volume = v);
    }

    void OnEnable()
    {
        // Show the current volume whenever the slider appears.
        slider.SetValueWithoutNotify(AudioListener.volume);
    }
}