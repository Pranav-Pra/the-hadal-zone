using UnityEngine;

// A creature that gives charge after a number of successful quicktime hits
public class ChargeSource : MonoBehaviour
{
    [SerializeField] private ChargeData chargeData;
    [SerializeField] private int amount = 1;
    [SerializeField] private int hitsToDrain = 2;              
    [SerializeField] private DrainPattern drainPattern;        // how the drain bar behaves for this creature
    [SerializeField] private SpriteRenderer spriteRenderer;    // leave empty to find it automatically
    [SerializeField] private Behaviour[] stopWhenFrozen;       // the creature's movement script
    [SerializeField] private AudioClip drainSound;
    [SerializeField, Range(0f, 1f)] private float drainVolume = 0.8f;

    private float startGlow;
    private int hits;

    public bool IsDrained => hits >= hitsToDrain;
    public float GlowFraction => 1f - (float)hits / hitsToDrain;   // 1 = full, 0 = drained
    public DrainPattern Pattern => drainPattern;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        startGlow = spriteRenderer.material.GetFloat("_GlowStrength");
    }

    public void SetFrozen(bool frozen)
    {
        foreach (Behaviour b in stopWhenFrozen)
            if (b != null) b.enabled = !frozen;
    }

    // Call this once per successful quicktime hit
    public void RegisterHit()
    {
        if (IsDrained) return;

        hits++;
        spriteRenderer.material.SetFloat("_GlowStrength", startGlow * GlowFraction);

        if (IsDrained)
        {
            chargeData.Add(amount);
            if (drainSound != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(drainSound, Camera.main.transform.position, drainVolume);
        }
    }
}