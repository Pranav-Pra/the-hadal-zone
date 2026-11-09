using UnityEngine;
using UnityEngine.UI;

// bootm screen signal bar, circle moves and player clicks while in zone
public class DrainBar : MonoBehaviour
{
    [SerializeField] private RawImage waveform;
    [SerializeField] private Image zone;
    [SerializeField] private Image spark;
    [SerializeField] private Image flash;

    private RectTransform bar;
    private Vector2 barHome;
    private DrainPattern pattern;

    private float pos, dir = 1f, speed;
    private float burstLeft, nextBurst;
    private float zoneCenter;
    private float amplitude = 1f;   // waveform height
    private float spike, noise, flashAlpha;
    private bool stopped;

    public bool SparkInZone => !stopped && Mathf.Abs(pos - zoneCenter) <= pattern.zoneWidth * 0.5f;

    void Awake()
    {
        bar = (RectTransform)transform;
        barHome = bar.anchoredPosition;

        // zone and spark are positioned from the bar left edge
        foreach (RectTransform r in new[] { zone.rectTransform, spark.rectTransform })
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void Show(DrainPattern p, float glowFraction)
    {
        CancelInvoke(nameof(Hide));
        gameObject.SetActive(true);

        pattern = p;
        speed = p.sparkSpeed;
        pos = Random.value;
        dir = 1f;
        burstLeft = 0f;
        nextBurst = Random.Range(p.burstInterval.x, p.burstInterval.y);
        amplitude = glowFraction;
        spike = noise = flashAlpha = 0f;
        stopped = false;

        waveform.color = p.color;
        zone.color = new Color(p.color.r, p.color.g, p.color.b, 0.25f);
        spark.color = Color.Lerp(Color.white, p.color, 0.3f);
        spark.enabled = true;
        zone.enabled = true;

        MoveZone();
    }

    public void PlayHit(float glowFraction)
    {
        amplitude = glowFraction;
        spike = 1f;
        flashAlpha = 0.6f;
        speed *= pattern.speedUpPerHit;
        MoveZone();
    }

    public void PlayMiss() => noise = 0.4f;

    // Final hit : freeze spark, show the flatline briefly, then hide
    public void Finish()
    {
        stopped = true;
        spark.enabled = false;
        zone.enabled = false;
        Invoke(nameof(Hide), 0.8f);
    }

    public void Fail()
    {
        stopped = true;
        spark.enabled = false;
        zone.enabled = false;
        noise = 0.5f;
        Invoke(nameof(Hide), 0.5f);
    }

    public void Hide()
    {
        CancelInvoke(nameof(Hide));
        bar.anchoredPosition = barHome;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (pattern == null) return;
        float dt = Time.deltaTime;
        float width = bar.rect.width;

        // spark with random speed bursts
        if (!stopped)
        {
            nextBurst -= dt;
            if (nextBurst <= 0f)
            {
                burstLeft = pattern.burstDuration;
                nextBurst = Random.Range(pattern.burstInterval.x, pattern.burstInterval.y);
            }
            burstLeft -= dt;

            float s = speed * (burstLeft > 0f ? pattern.burstMultiplier : 1f);
            pos += dir * s * dt;
            if (pos > 1f) { pos = 1f; dir = -1f; }
            else if (pos < 0f) { pos = 0f; dir = 1f; }

            spark.rectTransform.anchoredPosition = new Vector2(pos * width, 0f);
            spark.rectTransform.localScale = Vector3.one * (burstLeft > 0f ? 1.3f : 1f);
        }

        // waveform scrolls and its height shows how much charge is left
        Rect uv = waveform.uvRect;
        uv.x += pattern.waveScrollSpeed * dt;
        uv.width = pattern.waveTiling;
        waveform.uvRect = uv;

        spike = Mathf.MoveTowards(spike, 0f, dt * 2.5f);
        noise = Mathf.MoveTowards(noise, 0f, dt);
        float height = amplitude + spike * 0.8f;
        if (noise > 0f) height = Random.Range(0.2f, 2f);                 // static on a miss
        waveform.rectTransform.localScale = new Vector3(1f, Mathf.Max(height, 0.03f), 1f);   // 0.03 = flatline

        // shake on a miss
        bar.anchoredPosition = barHome + (noise > 0f ? Random.insideUnitCircle * 6f : Vector2.zero);

        // flash on a hit
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 3f);
        flash.color = new Color(1f, 1f, 1f, flashAlpha);
    }

    private void MoveZone()
    {
        float half = pattern.zoneWidth * 0.5f;
        zoneCenter = Random.Range(half, 1f - half);
        zone.rectTransform.sizeDelta = new Vector2(pattern.zoneWidth * bar.rect.width, bar.rect.height);
        zone.rectTransform.anchoredPosition = new Vector2(zoneCenter * bar.rect.width, 0f);
    }
}