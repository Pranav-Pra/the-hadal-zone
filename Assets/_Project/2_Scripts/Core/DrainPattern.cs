using UnityEngine;

[CreateAssetMenu(menuName = "Hadal Zone/Drain Pattern")]
public class DrainPattern : ScriptableObject
{
    [Header("Look")]
    public Color color = new Color(0.36f, 0.79f, 0.65f);
    public float waveScrollSpeed = 3f;                      // how fast the waveform scrolls
    public float waveTiling = 2f;                           // how many wave images fit across the bar

    [Header("Spark")]
    public float sparkSpeed = 0.85f;        // bar lengths per second
    public float speedUpPerHit = 1.18f;     // creature panics after each hit
    public float burstMultiplier = 2.6f;    // speed during a burst
    public float burstDuration = 0.25f;
    public Vector2 burstInterval = new Vector2(0.7f, 1.6f);   // random time between bursts

    [Header("Zone")]
    [Range(0.05f, 0.5f)] public float zoneWidth = 0.13f;   // fraction of the bar

    [Header("Strikes")]
    [Min(1)] public int maxMisses = 3; // wrong clicks before drqin fails
}