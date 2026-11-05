using System.Collections;
using UnityEngine;
 
// Attach to the GameObject with your flashlight's Light component (Spot Light).
[RequireComponent(typeof(Light))]
public class FlashlightFlicker : MonoBehaviour
{
    [Header("Time between outages (seconds)")]
    public float minTimeBetweenOutages = 15f;
    public float maxTimeBetweenOutages = 45f;
 
    [Header("Outage")]
    public float minOutageDuration = 0.5f;
    public float maxOutageDuration = 3f;
    public int minFlickers = 2;          // quick stutters before it dies / comes back
    public int maxFlickers = 6;
 
    [Header("Optional")]
    public AudioSource audioSource;
    public AudioClip flickerClip;
 
    Light flashlight;
    float baseIntensity;
 
    public bool IsOut { get; private set; }
 
    void Awake()
    {
        flashlight = GetComponent<Light>();
        baseIntensity = flashlight.intensity;
    }
 
    void OnEnable() => StartCoroutine(OutageLoop());
 
    IEnumerator OutageLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetweenOutages, maxTimeBetweenOutages));
            yield return Outage();
        }
    }
 
    // Public so you can trigger it from scripted moments (e.g. a monster getting close).
    public void TriggerOutage() { if (!IsOut) StartCoroutine(Outage()); }
 
    IEnumerator Outage()
    {
        IsOut = true;
        PlaySound();
 
        // Stutter, then die
        yield return Flicker(Random.Range(minFlickers, maxFlickers + 1));
        flashlight.intensity = 0f;
 
        yield return new WaitForSeconds(Random.Range(minOutageDuration, maxOutageDuration));
 
        // Stutter back to life
        PlaySound();
        yield return Flicker(Random.Range(minFlickers, maxFlickers + 1));
        flashlight.intensity = baseIntensity;
        IsOut = false;
    }
 
    IEnumerator Flicker(int count)
    {
        for (int i = 0; i < count; i++)
        {
            flashlight.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
            flashlight.intensity = baseIntensity * Random.Range(0.3f, 1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.15f));
        }
    }
 
    void PlaySound()
    {
        if (audioSource && flickerClip) audioSource.PlayOneShot(flickerClip);
    }
}