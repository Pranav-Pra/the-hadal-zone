using UnityEngine;

// Random glitch bursts for any UI element (text or image)
[RequireComponent(typeof(CanvasGroup))]
public class UIGlitch : MonoBehaviour
{
    [SerializeField] private Vector2 timeBetweenGlitches = new Vector2(2f, 6f); // random wait, seconds
    [SerializeField] private float glitchDuration = 0.15f;  // how long each burst lasts
    [SerializeField] private float jitterAmount = 6f;       // max pixel jump
    [SerializeField] private float minAlpha = 0.2f;         // how much it can flicker out

    private RectTransform rt;
    private CanvasGroup group;
    private Vector2 basePos;
    private float nextGlitch, glitchEnd;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        basePos = rt.anchoredPosition;
        nextGlitch = Time.time + Random.Range(timeBetweenGlitches.x, timeBetweenGlitches.y);
    }

    void Update()
    {
        // Start a new burst when it's time
        if (Time.time >= nextGlitch)
        {
            glitchEnd = Time.time + glitchDuration;
            nextGlitch = Time.time + Random.Range(timeBetweenGlitches.x, timeBetweenGlitches.y);
        }

        if (Time.time < glitchEnd)
        {
            // Mostly horizontal jumps, like a bad video signal
            rt.anchoredPosition = basePos + new Vector2(
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount) * 0.3f);
            group.alpha = Random.value < 0.5f ? Random.Range(minAlpha, 1f) : 1f;
        }
        else
        {
            rt.anchoredPosition = basePos;
            group.alpha = 1f;
        }
    }
}