using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TransmissionBox : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text transcript;
    public float charsPerSecond = 40f;
    public float periodPause = 0.25f;    // pause after . ! ?
    public float ellipsisPause = 0.7f;   // pause after ... or …
    public float minHold = 1.2f;        // minimum time a section stays up
    public float holdPerChar = 0.03f;   // longer sections stay up longer
    public float fadeOutTime = 0.2f;    // quick fade before the next section

    [Header("Button")]
    public Button actionButton;
    public TMP_Text actionLabel;

    [Header("Waveform")]
    public RectTransform barTemplate;    // one bar inside a Horizontal Layout Group
    public int barCount = 24;
    [Range(0f, 1f)] public float idleHeight = 0.08f;
    public float waveSpeed = 12f;

    [Header("Audio")]
    public AudioSource staticSource;     // optional clip; generated if empty
    public float staticVolume = 0.35f;

    public bool IsTyping { get; private set; }

    RectTransform[] bars;
    bool clicked;
    float talk;   // smoothed 0 (silent) -> 1 (transmitting)

    void Awake()
    {
        bars = new RectTransform[barCount];
        bars[0] = barTemplate;
        for (int i = 1; i < barCount; i++)
            bars[i] = Instantiate(barTemplate, barTemplate.parent);

        actionButton.onClick.AddListener(() => clicked = true);
        actionButton.gameObject.SetActive(false);

        if (staticSource.clip == null) staticSource.clip = MakeStatic();
        staticSource.loop = true;
        staticSource.volume = 0f;
        staticSource.Play();

        Clear();
    }

    void Update()
    {
        talk = Mathf.MoveTowards(talk, IsTyping ? 1f : 0f, Time.deltaTime * 8f);

        // Waveform: flat when silent, jumpy when "talking", taller in the middle
        for (int i = 0; i < bars.Length; i++)
        {
            float center = 1f - Mathf.Abs(i / (float)Mathf.Max(1, bars.Length - 1) * 2f - 1f) * 0.6f;
            float n = Mathf.PerlinNoise(i * 0.7f, Time.time * waveSpeed);
            float h = Mathf.Lerp(idleHeight, Mathf.Max(idleHeight, n * center), talk);
            bars[i].localScale = new Vector3(1f, h, 1f);
        }

        // Static: only audible while typing, with a little flutter
        staticSource.volume = staticVolume * talk * (0.6f + 0.4f * Mathf.PerlinNoise(Time.time * 6f, 0f));
    }

    public void Clear()
    {
        transcript.text = "";
        transcript.maxVisibleCharacters = 0;
        transcript.alpha = 1f;
    }

    // Shows one section. hold < 0 = automatic reading time; pass 0 when a button comes right after.
    public IEnumerator Type(string section, float hold = -1f)
    {
        if (transcript.text.Length > 0) yield return FadeOut();

        transcript.text = section;
        transcript.maxVisibleCharacters = 0;
        transcript.alpha = 1f;
        transcript.ForceMeshUpdate();
        int total = transcript.textInfo.characterCount;

        IsTyping = true;
        float delay = 1f / charsPerSecond;
        for (int shown = 1; shown <= total; shown++)
        {
            transcript.maxVisibleCharacters = shown;

            float extra = ExtraPause(shown - 1, total);
            if (extra > 0f)
            {
                IsTyping = false;
                yield return new WaitForSeconds(extra);
                IsTyping = true;
            }
            yield return new WaitForSeconds(delay);
        }
        IsTyping = false;

        if (hold < 0f) hold = minHold + total * holdPerChar;
        yield return new WaitForSeconds(hold);
    }

    IEnumerator FadeOut()
    {
        for (float e = 0f; e < fadeOutTime; e += Time.deltaTime)
        {
            transcript.alpha = 1f - e / fadeOutTime;
            yield return null;
        }
        transcript.alpha = 0f;
    }

    public IEnumerator WaitForButton(string label)
    {
        actionLabel.text = label;
        clicked = false;
        actionButton.gameObject.SetActive(true);
        yield return new WaitUntil(() => clicked);
        actionButton.gameObject.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);   // stop Space/Enter re-clicking it later
    }

    float ExtraPause(int i, int total)
    {
        var info = transcript.textInfo.characterInfo;
        char c = info[i].character;
        char prev = i > 0 ? info[i - 1].character : ' ';
        char next = i + 1 < total ? info[i + 1].character : ' ';

        if (c == '…') return ellipsisPause;
        if (c == '.' && next == '.') return 0f;              // middle of "..."
        if (c == '.' && prev == '.') return ellipsisPause;   // end of "..."
        if (c == '.' || c == '!' || c == '?') return periodPause;
        if (c == ',') return periodPause * 0.4f;
        return 0f;
    }

    static AudioClip MakeStatic()
    {
        int rate = 44100;
        var data = new float[rate];
        float smooth = 0f;
        for (int i = 0; i < rate; i++)
        {
            smooth = Mathf.Lerp(smooth, Random.value * 2f - 1f, 0.35f);
            data[i] = smooth * 0.5f;
        }
        var clip = AudioClip.Create("Static", rate, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}