using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class DescentController : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        public string name;
        public string subtitle;
        public float startMeters;
        public float endMeters;
        public float descentSeconds;

        public Zone() { }
        public Zone(string n, string s, float a, float b, float secs)
        { name = n; subtitle = s; startMeters = a; endMeters = b; descentSeconds = secs; }
    }

    [Header("References")]
    public Transform robot;
    public TMP_Text zoneText;       // big centered title shown during each pause

    [Header("Descent")]
    public float unitsPerZone = 100f;          // world distance per zone
    public float pauseSeconds = 3f;            // how long to hold at each zone
    public bool hold;                          // a tutorial script can set this to keep the robot paused
    public CoordinateDisplay coords;

    public Zone[] zones =
    {
        new Zone("SUNLIGHT ZONE", "Epipelagic",       0,   200,  8),
        new Zone("TWILIGHT ZONE", "Mesopelagic",    200,  1000, 10),
        new Zone("MIDNIGHT ZONE", "Bathypelagic",  1000,  4000, 12),
        new Zone("ABYSSAL ZONE",  "Abyssopelagic", 4000,  6000, 10),
        new Zone("HADAL ZONE",    "Hadalpelagic",  6000, 11000, 14),
    };

    public event Action<int> OnZoneReached;
    public event Action OnDescentComplete;

    public int CurrentZone { get; private set; }
    public float CurrentMeters { get; private set; }

    void Start()
    {
        if (zoneText) zoneText.alpha = 0f;
        SetDepth(0, 0f);
        StartCoroutine(Descend());
    }

    IEnumerator Descend()
    {
        // Show the Sunlight Zone title as the dive begins, without stopping
        if (zoneText) StartCoroutine(FlashTitle(0));

        for (int i = 0; i < zones.Length; i++)
        {
            CurrentZone = i;
            float duration = Mathf.Max(0.01f, zones[i].descentSeconds);
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                SetDepth(i, Mathf.SmoothStep(0f, 1f, e / duration));
                yield return null;
            }
            SetDepth(i, 1f);

            bool last = i == zones.Length - 1;
            yield return PauseAt(last ? -1 : i + 1);   // -1 = final stop at 7000, no title
        }

        OnDescentComplete?.Invoke();
    }

    IEnumerator PauseAt(int enteringZone)
    {
        bool hasTitle = enteringZone >= 0 && zoneText;
        if (enteringZone >= 0) OnZoneReached?.Invoke(enteringZone);

        if (hasTitle) { SetTitle(enteringZone); yield return Fade(0f, 1f, 0.6f); }

        yield return new WaitForSeconds(pauseSeconds);
        yield return new WaitUntil(() => !hold);

        if (hasTitle) yield return Fade(1f, 0f, 0.6f);
    }

    IEnumerator FlashTitle(int i)
    {
        SetTitle(i);
        yield return Fade(0f, 1f, 0.6f);
        yield return new WaitForSeconds(pauseSeconds);
        yield return Fade(1f, 0f, 0.6f);
    }

    void SetTitle(int i)
    {
        Zone z = zones[i];
        zoneText.text = $"{z.name}\n<size=60%>{z.subtitle}  |  {z.startMeters:N0} - {z.endMeters:N0} m</size>";
    }

        IEnumerator Fade(float from, float to, float time)
        {
            for (float e = 0f; e < time; e += Time.deltaTime)
            {
                zoneText.alpha = Mathf.Lerp(from, to, e / time);
                yield return null;
            }
            zoneText.alpha = to;
        }

    void SetDepth(int zoneIndex, float progress)
    {
        Zone z = zones[zoneIndex];
        CurrentMeters = Mathf.Lerp(z.startMeters, z.endMeters, progress);

        Vector3 p = robot.position;
        p.y = -(zoneIndex + progress) * unitsPerZone;
        robot.position = p;

        if (coords) coords.DepthOverride = CurrentMeters;
    }
}