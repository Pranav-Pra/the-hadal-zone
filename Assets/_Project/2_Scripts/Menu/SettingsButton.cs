using UnityEngine;
public class SettingsButtonSound : MonoBehaviour
{
    [SerializeField] AudioSource audioSource; // must NOT be on SettingsCanvas
    [SerializeField] AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] float clickVolume = 1f;

    public void PlayClick()
    {
        audioSource.ignoreListenerPause = true; // opening the menu pauses audio; this keeps the click audible
        audioSource.PlayOneShot(clickSound, clickVolume);
    }
}