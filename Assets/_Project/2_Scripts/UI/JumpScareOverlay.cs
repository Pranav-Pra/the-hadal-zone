using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Plays the isopod jump scare when SandSiteSpawner fires OnJumpScare (the moment the isopod on the sand vanishes):
/// 1. the arm's spotlight cuts out
/// 2. a random 1-3 s wait in the dark
/// 3. isopod slams onto the lens and wriggles + scream + camera shake
/// 4. isopod fades away, spotlight comes back on
/// Put this on an object under the Canvas, as the LAST child so it draws on top of all other UI.
/// </summary>
public class JumpScareOverlay : MonoBehaviour
{
    [Header("Hook up")]
    [SerializeField] SandSiteSpawner spawner;
    [Tooltip("Camera to shake. If another script moves this camera every frame, put the camera under an empty parent and shake the camera (the child).")]
    [SerializeField] Transform cameraToShake;
    [Tooltip("The spotlight on the robot arm.")]
    [SerializeField] Light spotlight;
    [Tooltip("Full-screen Image with the isopod-on-the-lens PNG.")]
    [SerializeField] Image isopodImage;

    [Header("1. Spotlight off")]
    [Tooltip("Optional click / power-down sound when the light cuts out.")]
    [SerializeField] AudioClip lightOffClip;
    [SerializeField, Range(0f, 1f)] float lightOffVolume = 1f;
    [Tooltip("Random wait (min, max seconds) in the dark before the isopod hits.")]
    [SerializeField] Vector2 darkWait = new Vector2(1f, 3f);

    [Header("2. Isopod hits the camera")]
    [SerializeField] AudioClip jumpScareClip;
    [SerializeField, Range(0f, 1f)] float jumpScareVolume = 1f;
    [Tooltip("Loops for as long as the isopod is on the lens.")]
    [SerializeField] AudioClip shakeClip;
    [SerializeField, Range(0f, 1f)] float shakeVolume = 0.8f;
    [Tooltip("How long the isopod stays on the lens.")]
    [SerializeField] float scareDuration = 1.8f;
    [Tooltip("Seconds for the image to slam in from big to normal size.")]
    [SerializeField] float popInTime = 0.08f;
    [SerializeField] float popStartScale = 1.5f;

    [Header("Camera shake")]
    [Tooltip("How far the camera moves while shaking, in world units.")]
    [SerializeField] float shakeStrength = 0.25f;
    [SerializeField] float shakeFrequency = 25f;
    [Tooltip("Parent of all the HUD under the Canvas")]
    [SerializeField] RectTransform uiToShake;
    [Tooltip("How far the HUD moves while shaking, in pixels.")]
    [SerializeField] float uiShakePixels = 25f;
    [Tooltip("How far the HUD tilts while shaking, in degrees.")]
    [SerializeField] float uiShakeAngle = 2f;

    [Header("Wriggle (the isopod image)")]
    [Tooltip("Random twitching, in pixels.")]
    [SerializeField] float jitterPixels = 30f;
    [SerializeField] float jitterSpeed = 18f;
    [Tooltip("How far it twists side to side, in degrees.")]
    [SerializeField] float wriggleAngle = 10f;
    [Tooltip("Twists per second.")]
    [SerializeField] float wriggleSpeed = 7f;
    [Tooltip("How much it squashes and stretches as it squirms (0.08 = 8%).")]
    [SerializeField] float squirmAmount = 0.08f;
    [Tooltip("Sudden lunges toward the lens, per second.")]
    [SerializeField] float lungesPerSecond = 2.5f;
    [Tooltip("How much bigger it gets on a lunge (0.12 = 12%).")]
    [SerializeField] float lungeScale = 0.12f;

    [Header("3. Back to normal")]
    [Tooltip("Seconds for the isopod to fade away.")]
    [SerializeField] float fadeOutTime = 0.3f;

    [Header("Events")]
    [Tooltip("When the light cuts out. e.g. disable the robot's controls.")]
    public UnityEvent onScareStarted;
    [Tooltip("When everything is back to normal. e.g. re-enable the robot's controls.")]
    public UnityEvent onScareFinished;

    AudioSource sfx;
    AudioSource shakeSource;
    bool playing;

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        shakeSource = gameObject.AddComponent<AudioSource>();
        shakeSource.playOnAwake = false;
        shakeSource.spatialBlend = 0f;
        shakeSource.loop = true;

        if (isopodImage != null)
        {
            isopodImage.raycastTarget = false;   // never blocks the Detect button
            isopodImage.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (spawner != null) spawner.OnJumpScare += HandleJumpScare;
    }

    void OnDisable()
    {
        if (spawner != null) spawner.OnJumpScare -= HandleJumpScare;
    }

    void HandleJumpScare(SandSite site)
    {
        if (!playing) StartCoroutine(PlayScare());
    }

    [ContextMenu("Test Jump Scare")]   // right-click the component in Play mode to test without digging
    void TestScare()
    {
        if (Application.isPlaying && !playing) StartCoroutine(PlayScare());
    }

    IEnumerator PlayScare()
    {
        playing = true;
        onScareStarted?.Invoke();

        // 1. Spotlight cuts out
        bool lightWasOn = spotlight != null && spotlight.enabled;
        if (spotlight != null) spotlight.enabled = false;
        if (lightOffClip != null) sfx.PlayOneShot(lightOffClip, lightOffVolume);

        // 2. Random wait in the dark, so the player can't time it
        yield return new WaitForSeconds(Random.Range(darkWait.x, darkWait.y));

        // 3. Isopod hits the lens
        if (jumpScareClip != null) sfx.PlayOneShot(jumpScareClip, jumpScareVolume);
        if (shakeClip != null)
        {
            shakeSource.clip = shakeClip;
            shakeSource.volume = shakeVolume;
            shakeSource.Play();
        }
        if (isopodImage != null)
        {
            SetAlpha(isopodImage, 1f);
            isopodImage.gameObject.SetActive(true);
        }
        yield return ShakeAndWriggle(scareDuration, true);
        shakeSource.Stop();

        // 4. Fade away (still wriggling), then the light comes back
        yield return ShakeAndWriggle(fadeOutTime, false);
        if (isopodImage != null) isopodImage.gameObject.SetActive(false);
        if (spotlight != null && lightWasOn) spotlight.enabled = true;

        playing = false;   // ready for the next jump scare circle
        onScareFinished?.Invoke();
    }

    // Makes the isopod image squirm. While onLens is true the camera shakes too;
    // when it's false the image fades out instead and the camera stays still.
    IEnumerator ShakeAndWriggle(float duration, bool onLens)
    {
        Vector3 camHome = cameraToShake != null ? cameraToShake.localPosition : Vector3.zero;
        RectTransform img = isopodImage != null ? isopodImage.rectTransform : null;
        Vector2 imgHome = img != null ? img.anchoredPosition : Vector2.zero;
        Vector2 uiHome = uiToShake != null ? uiToShake.anchoredPosition : Vector2.zero;
        float seed = Random.value * 100f;
        float lunge = 0f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float p = t / duration;

            // Camera + HUD shake together (same noise), strong at first, easing off a little
            if (onLens)
            {
                float k = Mathf.Lerp(1f, 0.4f, p);
                float cx = Mathf.PerlinNoise(seed, t * shakeFrequency) * 2f - 1f;
                float cy = Mathf.PerlinNoise(seed + 37f, t * shakeFrequency) * 2f - 1f;

                if (cameraToShake != null)
                    cameraToShake.localPosition = camHome + new Vector3(cx, cy, 0f) * shakeStrength * k;

                if (uiToShake != null)
                {
                    uiToShake.anchoredPosition = uiHome + new Vector2(cx, cy) * uiShakePixels * k;
                    uiToShake.localRotation = Quaternion.Euler(0f, 0f, cx * uiShakeAngle * k);
                }
            }

            if (img != null)
            {
                // Twitchy crawling around the lens
                float jx = Mathf.PerlinNoise(seed + 11f, t * jitterSpeed) * 2f - 1f;
                float jy = Mathf.PerlinNoise(seed + 53f, t * jitterSpeed) * 2f - 1f;
                img.anchoredPosition = imgHome + new Vector2(jx, jy) * jitterPixels;

                // Twisting side to side, with some noise so it isn't a clean metronome
                float twist = Mathf.Sin(t * wriggleSpeed * Mathf.PI * 2f)
                            + (Mathf.PerlinNoise(seed + 71f, t * wriggleSpeed) * 2f - 1f) * 0.5f;
                img.localRotation = Quaternion.Euler(0f, 0f, twist * wriggleAngle);

                // Random lunges toward the lens that snap in and ease back
                if (onLens && Random.value < lungesPerSecond * Time.deltaTime) lunge = 1f;
                lunge = Mathf.MoveTowards(lunge, 0f, Time.deltaTime * 6f);

                // Squash and stretch as it squirms, on top of the slam-in and lunges
                float squirm = Mathf.Sin(t * wriggleSpeed * 1.3f * Mathf.PI * 2f) * squirmAmount;
                float pop = onLens && t < popInTime ? Mathf.Lerp(popStartScale, 1f, t / popInTime) : 1f;
                float size = pop * (1f + lunge * lungeScale);
                img.localScale = new Vector3(size * (1f + squirm), size * (1f - squirm), 1f);

                if (!onLens) SetAlpha(isopodImage, 1f - p);
            }

            yield return null;
        }

        if (cameraToShake != null) cameraToShake.localPosition = camHome;
        if (uiToShake != null)
        {
            uiToShake.anchoredPosition = uiHome;
            uiToShake.localRotation = Quaternion.identity;
        }
        if (img != null)
        {
            img.anchoredPosition = imgHome;
            img.localRotation = Quaternion.identity;
            img.localScale = Vector3.one;
        }
    }

    static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}