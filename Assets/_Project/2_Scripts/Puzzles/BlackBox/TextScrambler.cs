using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class TextScrambler : MonoBehaviour
{
    [Header("Scramble Level")]
    [Tooltip("1 = mostly scrambled, 5 = fully readable.")]
    [Range(1, 5)] public int level = 3;

    float[] revealFraction = { 0.15f, 0.35f, 0.55f, 0.80f, 1.0f };

    [Header("Behaviour")]
    [Tooltip("How often the random glyphs change (seconds).")]
    public float scrambleInterval = 0.05f;
    [Tooltip("If on, the real characters move around each refresh. If off, the same characters stay readable.")]
    public bool reshuffleRevealed = false;
    [Tooltip("Seconds until the text settles to the full real text. 0 = stay scrambled until you stop it.")]
    public float settleAfter = 0f;

    [Header("Characters")]
    public string glyphs = "!<>-_\\/[]{}—=+*^?#";
    public bool keepSpaces = true;

    [Header("Optional")]
    public bool playOnEnable = false;
    [TextArea] public string startText = "";

    TMP_Text label;
    Coroutine routine;
    bool[] shown;
    string currentTarget;

    /// <summary>Raises the level (less scrambled), keeping already revealed characters.</summary>
    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 5);
        if (shown == null || currentTarget == null) return;

        int length = shown.Length;
        int want = Mathf.RoundToInt(length * revealFraction[level - 1]);
        var hidden = new System.Collections.Generic.List<int>();
        int have = 0;
        for (int i = 0; i < length; i++)
        {
            if (shown[i]) have++;
            else hidden.Add(i);
        }

        for (int n = have; n < want && hidden.Count > 0; n++)
        {
            int k = Random.Range(0, hidden.Count);
            shown[hidden[k]] = true;
            hidden.RemoveAt(k);
        }
    }

    void Awake() => label = GetComponent<TMP_Text>();

    void OnEnable()
    {
        if (playOnEnable)
            Scramble(string.IsNullOrEmpty(startText) ? label.text : startText);
    }

    public void Scramble(string target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ScrambleRoutine(target));
    }


    /// <summary>Stops the effect and shows the real text.</summary>
    public void Stop(string target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        label.text = target;
    }

    IEnumerator ScrambleRoutine(string target)
    {
        int length = target.Length;
        shown = new bool[length];
        currentTarget = target;
        var sb = new StringBuilder(length);
        float elapsed = 0f;

        PickRevealed(shown, target);

        while (settleAfter <= 0f || elapsed < settleAfter)
        {
            if (reshuffleRevealed) PickRevealed(shown, target);

            sb.Clear();
            for (int i = 0; i < length; i++)
            {
                char c = target[i];
                if (shown[i] || (keepSpaces && char.IsWhiteSpace(c)))
                    sb.Append(c);
                else
                    sb.Append(glyphs[Random.Range(0, glyphs.Length)]);
            }
            label.text = startText + sb.ToString();

            elapsed += scrambleInterval;
            yield return new WaitForSecondsRealtime(scrambleInterval);
        }

        label.text = target;
        routine = null;
    }

    // Randomly marks the right number of characters as "real".
    void PickRevealed(bool[] shown, string target)
    {
        int length = target.Length;
        int count = Mathf.RoundToInt(length * revealFraction[Mathf.Clamp(level, 1, 5) - 1]);

        for (int i = 0; i < length; i++) shown[i] = false;

        // Partial Fisher-Yates over indices to pick 'count' unique positions.
        int[] idx = new int[length];
        for (int i = 0; i < length; i++) idx[i] = i;
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, length);
            (idx[i], idx[j]) = (idx[j], idx[i]);
            shown[idx[i]] = true;
        }
    }
}
