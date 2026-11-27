using System;
using System.Collections;
using UnityEngine;

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

    public Transform robot;
    public CoordinateDisplay coords;
    public float unitsPerZone = 100f;

    public Zone[] zones =
    {
        new Zone("SUNLIGHT ZONE", "Epipelagic",       0,  200, 15),
        new Zone("TWILIGHT ZONE", "Mesopelagic",    200, 1000, 10),
        new Zone("MIDNIGHT ZONE", "Bathypelagic",  1000, 4000, 12),
        new Zone("ABYSSAL ZONE",  "Abyssopelagic", 4000, 6000, 10),
        new Zone("HADAL ZONE",    "Hadalpelagic",  6000, 7000, 14),
    };

    public int CurrentZone { get; private set; }
    public float CurrentMeters { get; private set; }

    void Start() => SetDepth(0, 0f);

    // Sinks from the top of zone i to the bottom of zone i, easing in and out
    public IEnumerator DescendZone(int i, bool accelerate = false)
    {
        CurrentZone = i;
        float duration = Mathf.Max(0.01f, zones[i].descentSeconds);
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float p = e / duration;
            SetDepth(i, accelerate ? p * p : Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }
        SetDepth(i, 1f);
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