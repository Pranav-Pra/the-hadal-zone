using UnityEngine;

// Magnapinna squid: stays roughly where it spawned and drifts
// left/right, up/down, and closer/farther from the player.
// Never despawns on its own; the attack/QTE code destroys it.
public class Creature2 : Creature
{
    [Header("Drift Amounts (meters)")]
    [SerializeField] private float sideAmount = 1.5f;      // left/right
    [SerializeField] private float upDownAmount = 1f;      // up/down
    [SerializeField] private float depthAmount = 6f;       // closer/farther
    [SerializeField] private float minDistance = 8f;       // never closer than this to the player

    [Header("Drift Speeds (lower = slower, smoother)")]
    [SerializeField] private float sideSpeed = 0.15f;
    [SerializeField] private float upDownSpeed = 0.25f;
    [SerializeField] private float depthSpeed = 0.05f;

    private Vector3 anchorCenter;
    private Vector3 outward;     // direction from player toward the squid
    private Vector3 sideways;    // left/right as seen from the player
    private float baseRadius;
    private float baseHeight;
    private float seed;          // makes each squid drift differently
    private float animOffset;    // keeps squids from flapping in sync
    private SpriteRenderer squidSprite;
    private Animator anim;

    private float squidDespawnTime;
    private Renderer body;

    public override void Init(Vector3 orbitCenter, float orbitRadius, float startAngle, float height)
    {
        // base.Init is not called: the squid doesn't orbit or despawn
        anchorCenter = orbitCenter;
        baseRadius = orbitRadius;
        outward = new Vector3(Mathf.Cos(startAngle), 0f, Mathf.Sin(startAngle));
        sideways = new Vector3(-Mathf.Sin(startAngle), 0f, Mathf.Cos(startAngle));
        baseHeight = height;
        seed = Random.Range(0f, 1000f);
        animOffset = Random.Range(0f, 10f);
        squidSprite = GetComponentInChildren<SpriteRenderer>();
        Drift();
        squidDespawnTime = Time.time + Random.Range(lifetime.x, lifetime.y);
        body = GetComponentInChildren<Renderer>();  
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.Play(0, 0, Random.value);   // start the loop at a random point
    }

    protected override void Update()
    {
        Drift();

        // lifetime up and no part of the squid on screen
        if (Time.time >= squidDespawnTime && !AnyPartOnCamera())
            Destroy(gameObject);
    }

    bool AnyPartOnCamera()
    {
        if (body == null) return IsOnCamera();

        Bounds b = body.bounds;
        for (int i = 0; i <= 4; i++)
        {
            float y = Mathf.Lerp(b.min.y, b.max.y, i / 4f);
            if (IsPointOnCamera(new Vector3(b.center.x, y, b.center.z)))
                return true;
        }
        return false;
    }

    void Drift()
    {
        float dist = Mathf.Max(minDistance, baseRadius + Noise(depthSpeed, 0f) * depthAmount);

        Vector3 pos = anchorCenter + outward * dist + sideways * (Noise(sideSpeed, 100f) * sideAmount);
        pos.y = GroundHeight(pos) + baseHeight + Noise(upDownSpeed, 200f) * upDownAmount;
        transform.position = pos;
    }

    // smooth random value between about -1 and 1
    float Noise(float speed, float channel)
    {
        return Mathf.PerlinNoise(seed + channel, Time.time * speed) * 2f - 1f;
    }
}