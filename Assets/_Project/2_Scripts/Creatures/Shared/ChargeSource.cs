using UnityEngine;

// Lets a creature be clicked once to give charge
public class ChargeSource : MonoBehaviour
{
    [SerializeField] private ChargeData chargeData;
    [SerializeField] private int amount = 1;
    [SerializeField] private Light glowLight;
    [SerializeField] private AudioClip drainSound;
    [SerializeField, Range(0f, 1f)] private float drainVolume = 0.8f;

    private bool collected;

    public void Collect()
    {
        if (collected) return;   // each creature only counts once
        collected = true;

        if (glowLight != null) glowLight.enabled = false;
        chargeData.Add(amount);

        if (drainSound != null && Camera.main != null)
        AudioSource.PlayClipAtPoint(drainSound, Camera.main.transform.position, drainVolume);
    }
}