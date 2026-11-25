using UnityEngine;

// Put this on each AudioSource the slider should control. Leave it off everything else.
[RequireComponent(typeof(AudioSource))]
public class LoopVolume : MonoBehaviour
{
    AudioSource source;

    void Awake() => source = GetComponent<AudioSource>();

    // Every frame, match the saved slider value (also runs while paused).
    void Update() => source.volume = PlayerPrefs.GetFloat(VolumeSlider.SaveKey, 1f);
}
