using System;
using System.Collections;          // NEW (rise coroutine)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;              // NEW (reveal button)

public enum SandItemType { PuzzleStarter, JumpScare, Algae }

[Serializable]
public class SandSite
{
    public int id;
    public SandItemType itemType;
    public Vector3 center;              // point on the sand surface
    public Vector3 surfaceNormal = Vector3.up;
    public float radius;                // world units
    public Vector3 hiddenItemPosition;  // exact spot the metal detector has to find
    public bool revealed;

    [NonSerialized] public GameObject marker;
    [NonSerialized] public GameObject item;   // buried object in this circle (algae for now)

    // is this world point inside the circle?
    public bool ContainsXZ(Vector3 worldPos)
    {
        float dx = worldPos.x - center.x;
        float dz = worldPos.z - center.z;
        return dx * dx + dz * dz <= radius * radius;
    }
}

/// <summary>
/// Spawns 3-5 search circles in the sand around the robot.
/// Always exactly 1 PuzzleStarter, 1-2 JumpScares, and 1-2 Algae.
/// every circle sits completely inside the sonar range /completely outside the exclusion zone.
/// </summary>
public class SandSiteSpawner : MonoBehaviour
{
    [Header("XZ plane centered on the robot)")]
    [SerializeField] Vector3 areaCenter = Vector3.zero;
    [Tooltip("World radius the sonar covers")]
    [SerializeField] float sonarRadius = 40f;
    [Tooltip("No part of circle enter this radius around robot")]
    [SerializeField] float exclusionRadius = 5f;
    

    [Header("Sites")]
    [Tooltip("Radius of each search circle in world units")]
    [SerializeField] float siteRadius = 4f;
    [Tooltip("min empty space between the edges of two circles")]
    [SerializeField] float minGapBetweenSites = 2f;
    [Tooltip("how far from the circle's center the hidden is, fracion of siteradius.")]
    [SerializeField, Range(0f, 1f)] float hiddenItemMaxOffset = 0.7f;

    [Header("Sand surface")]
    [Tooltip("Layers that count as sand")]
    [SerializeField] LayerMask sandLayer = ~0;
    [Tooltip("World Y the downward raycast starts from. Must be above the highest terrain (mountains reach 600)")]
    [SerializeField] float raycastStartY = 650f;
    [Tooltip("How far down the ray travels from raycastStartY")]
    [SerializeField] float raycastDistance = 1000f;
    [Tooltip("Reject circle centers on slopes steeper than this")]
    [SerializeField, Range(0f, 90f)] float maxSlopeAngle = 30f;
    [Tooltip("Used when the ray hits nothing at all.")]
    [SerializeField] float fallbackSandY = 0f;

    [Header("Optional world marker")]
    [Tooltip("Something 1 unit across in XZ, scaled to the circle's diameter. On bumpy sand a URP Decal Projector works better than a flat quad. Leave empty for no visual.")]
    [SerializeField] GameObject siteMarkerPrefab;
    [SerializeField] float markerHeightOffset = 0.02f;

    [Header("Puzzle object")]
    [Tooltip("The SonarObject (tag PuzzleObject). Moved to the PuzzleStarter site.")]
    [SerializeField] Transform puzzleObject;
    [Tooltip("Raise the object above the sand if its pivot is in the middle of the sprite.")]
    [SerializeField] float itemHeightOffset = 0f;
    [Tooltip("RobotArm. When respawning, the new spot is kept away from it so the robot isn't trapped.")]
    [SerializeField] Transform robot;
    [Tooltip("Minimum gap between the robot and the edge of the new circle when respawning.")]
    [SerializeField] float minDistanceFromRobot = 8f;

    // NEW
    [Header("Buried puzzle object")]
    [Tooltip("Respawned objects start hidden in the sand. The first one starts visible.")]
    [SerializeField] bool buryOnRespawn = true;
    [Tooltip("Detect button above the metal detector. Shown while RobotArm is inside a circle that still has something to find.")]
    [SerializeField] Button revealButton;
    [Tooltip("Plays when Detect is clicked while the detector box touches the buried object's collider.")]
    [SerializeField] AudioClip successClip;
    [Tooltip("Plays when Detect is clicked anywhere else.")]
    [SerializeField] AudioClip failClip;
    [SerializeField, Range(0f, 1f)] float detectVolume = 0.8f;
    [Tooltip("How far under the sand it starts when it gets pulled up.")]
    [SerializeField] float buryDepth = 2f;
    [Tooltip("Seconds to rise out of the sand.")]
    [SerializeField] float riseTime = 0.5f;
    [Tooltip("The metal detector Box Collider on RobotArm. The object only turns solid once the box has moved off it, so the robot can't get stuck inside it.")]
    [SerializeField] BoxCollider detectorBox;

    [Header("Algae")]
    [Tooltip("Prefab with a SpriteRenderer and a Collider. One is buried in each Algae circle.")]
    [SerializeField] GameObject algaePrefab;
    [Tooltip("The same ChargeData asset ViewSwitcher uses.")]
    [SerializeField] ChargeData chargeData;
    [Tooltip("Charge segments added per algae.")]
    [SerializeField] int algaeCharge = 2;
    [Tooltip("Seconds the algae stays visible before it's absorbed into the charge bar.")]
    [SerializeField] float algaeShowTime = 2f;

    [Header("Generation")]
    [SerializeField] bool generateOnStart = true;
    [Tooltip("0 = different layout every run")]
    [SerializeField] int seed = 0;
    [SerializeField] int maxAttemptsPerSite = 200;
    [SerializeField] int maxLayoutAttempts = 25;

    public IReadOnlyList<SandSite> Sites => sites;
    public float SonarRadius => sonarRadius;
    public Vector3 AreaCenter => areaCenter;

    /// <summary>Fired after a successful Generate(). The sonar can subscribe to this to build its dots.</summary>
    public event Action<IReadOnlyList<SandSite>> OnSitesGenerated;

    readonly List<SandSite> sites = new List<SandSite>();
    System.Random rng;

    // NEW: buried state
    bool buried;
    bool detectUnlocked;          // false until the first puzzle is finished
    Coroutine puzzleRise;         // so stopping the puzzle's rise never cancels an algae pickup
    bool waitingForClear;
    Renderer[] puzzleRenderers;
    Collider[] puzzleColliders;
    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D, like the other UI sounds

        if (revealButton != null)
        {
            revealButton.onClick.AddListener(OnRevealClicked);
            revealButton.gameObject.SetActive(false);
        }
    }

    // Placement limits, shared by first generation and respawns
    float MinCenterR => exclusionRadius + siteRadius;          // whole circle stays out of the exclusion zone
    float MaxCenterR => sonarRadius - siteRadius;              // whole circle stays on the sonar
    float MinCenterDist => 2f * siteRadius + minGapBetweenSites;

    void Start()
    {
        if (generateOnStart) Generate();
    }

    // NEW: show the reveal button while the detector is over the buried object,
    // and make a revealed object solid once the detector has moved off it
    void Update()
    {
        if (puzzleObject == null) return;

        // Button shows anywhere inside a circle that hasn't been dug up yet
        SandSite here = robot != null ? GetSiteContaining(robot.position) : null;
        bool showButton = detectUnlocked && here != null && !here.revealed;
        if (revealButton != null && revealButton.gameObject.activeSelf != showButton)
            revealButton.gameObject.SetActive(showButton);

        if (waitingForClear && !CollidersOverlapDetector())
        {
            SetPuzzleSolid(true);
            waitingForClear = false;
        }
    }

    // Success only if the detector box is actually touching the buried object's collider
    void OnRevealClicked()
    {
        if (buried && CollidersOverlapDetector())
        {
            PlayClip(successClip);
            puzzleRise = StartCoroutine(RisePuzzleObject());
            return;
        }

        SandSite algae = FindTouchedAlgae();
        if (algae != null)
        {
            PlayClip(successClip);
            StartCoroutine(CollectAlgae(algae));
            return;
        }

        PlayClip(failClip);
    }

    void PlayClip(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip, detectVolume);
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        Clear();

        if (MinCenterR >= MaxCenterR)
        {
            Debug.LogError($"[SandSiteSpawner] No room: siteRadius {siteRadius} is too big for the ring between {exclusionRadius} and {sonarRadius}.");
            return;
        }

        rng = seed == 0 ? new System.Random() : new System.Random(seed);
        List<SandItemType> types = BuildItemList(rng);

        // If one site can't fit, throw the whole layout away and try again,
        // so the PuzzleStarter is never the one that gets dropped.
        for (int layout = 0; layout < maxLayoutAttempts; layout++)
        {
            if (TryBuildLayout(types))
            {
                SpawnMarkers();
                SpawnAlgae();
                PlacePuzzleObject();
                OnSitesGenerated?.Invoke(sites);
                return;
            }
            sites.Clear();
        }

        Debug.LogError("[SandSiteSpawner] Could not fit all sites. Lower siteRadius or minGapBetweenSites, or check the sand layer mask.");
    }

    public void Clear()
    {
        foreach (var s in sites)
        {
            if (s.marker != null) Destroy(s.marker);
            if (s.item != null) Destroy(s.item);
        }
        sites.Clear();
    }

    /// <summary>Returns the site whose circle contains this world point.</summary>
    public SandSite GetSiteContaining(Vector3 worldPos)
    {
        foreach (var s in sites)
            if (s.ContainsXZ(worldPos)) return s;
        return null;
    }

    // ---------------------------------------------------------------- NEW: puzzle object

    public SandSite GetSite(SandItemType type)
    {
        foreach (var s in sites)
            if (s.itemType == type) return s;
        return null;
    }

    /// <summary>Moves the PuzzleStarter circle (and the SonarObject) to a new random spot.</summary>
    public bool RespawnPuzzleObject()
    {
        detectUnlocked = true;   // the first respawn happens when the first puzzle is finished
        SandSite site = GetSite(SandItemType.PuzzleStarter);
        if (site == null) return false;

        for (int attempt = 0; attempt < maxAttemptsPerSite; attempt++)
        {
            // The site is still at its old position in the list, so TryFindSpot already
            // rejects anywhere too close to where it was: the new spot is always "elsewhere".
            if (!TryFindSpot(out Vector3 surfacePoint, out Vector3 normal, out Vector3 hidden)) continue;
            if (robot != null && FlatDistance(surfacePoint, robot.position) < siteRadius + minDistanceFromRobot) continue;

            site.center = surfacePoint;
            site.surfaceNormal = normal;
            site.hiddenItemPosition = hidden;
            site.revealed = false;
            if (site.marker != null)
                site.marker.transform.SetPositionAndRotation(
                    surfacePoint + normal * markerHeightOffset,
                    Quaternion.FromToRotation(Vector3.up, normal));

            PlacePuzzleObject();
            if (buryOnRespawn) BuryPuzzleObject();   // NEW
            return true;
        }

        Debug.LogWarning("[SandSiteSpawner] Couldn't find a new spot for the puzzle object.");
        return false;
    }

    /// <summary>After the last puzzle: hide the object and its sonar dot.</summary>
    public void RemovePuzzleObject()
    {
        SandSite site = GetSite(SandItemType.PuzzleStarter);
        if (site != null) site.revealed = true;
        if (puzzleRise != null) StopCoroutine(puzzleRise);
        buried = waitingForClear = false;          // NEW
        if (revealButton != null) revealButton.gameObject.SetActive(false);   // NEW
        if (puzzleObject != null) puzzleObject.gameObject.SetActive(false);
    }

    void PlacePuzzleObject()
    {
        if (puzzleObject == null) return;
        SandSite site = GetSite(SandItemType.PuzzleStarter);
        if (site == null) return;
        puzzleObject.position = site.hiddenItemPosition + Vector3.up * itemHeightOffset;
        puzzleObject.gameObject.SetActive(true);

        // NEW: start from a normal, visible, solid object
        if (puzzleRise != null) StopCoroutine(puzzleRise);
        buried = waitingForClear = false;
        SetPuzzleVisible(true);
        SetPuzzleSolid(true);
        site.revealed = true;   // visible on the sand, nothing to dig here
    }

    // ---------------------------------------------------------------- NEW: burying and rising

    // Hidden and not solid, but still active, so the Radar keeps showing its dot.
    void BuryPuzzleObject()
    {
        SandSite site = GetSite(SandItemType.PuzzleStarter);
        if (site != null) site.revealed = false;   // something to dig here now
        buried = true;
        waitingForClear = false;
        SetPuzzleVisible(false);
        SetPuzzleSolid(false);
    }

    // Shoots up out of the sand quickly and settles, like it's been yanked up by a magnet.
    IEnumerator RisePuzzleObject()
    {
        buried = false;
        SandSite site = GetSite(SandItemType.PuzzleStarter);
        if (site != null) site.revealed = true;    // dug up: the button stops showing in this circle
        SetPuzzleVisible(true);
        yield return RiseOutOfSand(puzzleObject);
        waitingForClear = true;   // Update turns it solid once the detector box has moved off it
    }

    // Does the detector box touch the object's own collider (the capsule)?
    // Used for a successful Detect, and after it rises to turn it solid only once the robot's off it.
    bool CollidersOverlapDetector()
    {
        if (detectorBox == null) return false;
        CachePuzzleParts();
        Transform bt = detectorBox.transform;

        foreach (var c in puzzleColliders)
            if (DetectorOverlaps(c)) return true;
        return false;
    }

    bool DetectorOverlaps(Collider c)
    {
        Transform bt = detectorBox.transform;
        bool wasEnabled = c.enabled;
        c.enabled = true;   // the overlap test needs the collider switched on
        bool overlap = Physics.ComputePenetration(
            detectorBox, bt.position, bt.rotation,
            c, c.transform.position, c.transform.rotation,
            out _, out _);
        c.enabled = wasEnabled;
        return overlap;
    }

    // Shoots up out of the sand quickly and settles, like it's been yanked up by a magnet.
    IEnumerator RiseOutOfSand(Transform t)
    {
        Vector3 end = t.position;
        Vector3 start = end - Vector3.up * buryDepth;
        for (float time = 0f; time < riseTime; time += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - time / riseTime, 3f);   // fast start, soft landing
            t.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        t.position = end;
    }

    // ---------------------------------------------------------------- algae

    // One buried algae per Algae circle: hidden, and its collider is a trigger so it never blocks the robot.
    void SpawnAlgae()
    {
        if (algaePrefab == null) return;
        foreach (var s in sites)
        {
            if (s.itemType != SandItemType.Algae) continue;
            GameObject a = Instantiate(algaePrefab, s.hiddenItemPosition + Vector3.up * itemHeightOffset, Quaternion.identity, transform);
            a.name = $"Algae_{s.id}";
            foreach (var r in a.GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in a.GetComponentsInChildren<Collider>()) c.isTrigger = true;
            s.item = a;
        }
    }

    // The Algae circle whose buried algae the detector box is touching, or null
    SandSite FindTouchedAlgae()
    {
        if (detectorBox == null) return null;
        foreach (var s in sites)
        {
            if (s.itemType != SandItemType.Algae || s.revealed || s.item == null) continue;
            foreach (var c in s.item.GetComponentsInChildren<Collider>())
                if (DetectorOverlaps(c)) return s;
        }
        return null;
    }

    // Rise, stay visible for a moment, shrink away and add to the charge bar
    IEnumerator CollectAlgae(SandSite site)
    {
        site.revealed = true;   // the button stops showing in this circle
        GameObject a = site.item;
        foreach (var r in a.GetComponentsInChildren<Renderer>()) r.enabled = true;

        yield return RiseOutOfSand(a.transform);
        yield return new WaitForSeconds(algaeShowTime);

        Vector3 startScale = a.transform.localScale;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            a.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.25f);
            yield return null;
        }

        if (chargeData != null) chargeData.Add(algaeCharge);
        Destroy(a);
        site.item = null;
    }

    void SetPuzzleVisible(bool on)
    {
        CachePuzzleParts();
        foreach (var r in puzzleRenderers) r.enabled = on;
    }

    void SetPuzzleSolid(bool on)
    {
        CachePuzzleParts();
        foreach (var c in puzzleColliders) c.enabled = on;
    }

    void CachePuzzleParts()
    {
        if (puzzleRenderers != null || puzzleObject == null) return;
        puzzleRenderers = puzzleObject.GetComponentsInChildren<Renderer>(true);
        puzzleColliders = puzzleObject.GetComponentsInChildren<Collider>(true);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // 1 PuzzleStarter + 1-2 JumpScare + 1-2 Algae. Total is picked uniformly from 3, 4, 5.
    List<SandItemType> BuildItemList(System.Random rng)
    {
        int total = rng.Next(3, 6); // 3..5
        int jumpScares = 1, algae = 1;
        int extras = total - 3;

        if (extras == 2) { jumpScares = 2; algae = 2; }
        else if (extras == 1) { if (rng.Next(2) == 0) jumpScares = 2; else algae = 2; }

        var list = new List<SandItemType> { SandItemType.PuzzleStarter };
        for (int i = 0; i < jumpScares; i++) list.Add(SandItemType.JumpScare);
        for (int i = 0; i < algae; i++) list.Add(SandItemType.Algae);

        // Shuffle so placement order doesn't favour any type.
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    bool TryBuildLayout(List<SandItemType> types)
    {
        for (int t = 0; t < types.Count; t++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttemptsPerSite && !placed; attempt++)
            {
                if (!TryFindSpot(out Vector3 surfacePoint, out Vector3 normal, out Vector3 hidden)) continue;

                sites.Add(new SandSite
                {
                    id = sites.Count,
                    itemType = types[t],
                    center = surfacePoint,
                    surfaceNormal = normal,
                    radius = siteRadius,
                    hiddenItemPosition = hidden,
                    revealed = false
                });
                placed = true;
            }

            if (!placed) return false;
        }
        return true;
    }

    // One random try at a valid circle: inside the ring, clear of every circle already in the list,
    // on sand that isn't too steep. Also picks the hidden item's spot (its own raycast, since the sand is bumpy).
    bool TryFindSpot(out Vector3 center, out Vector3 normal, out Vector3 hidden)
    {
        center = normal = hidden = default;

        Vector2 p = RandomPointInRing(rng, MinCenterR, MaxCenterR);
        Vector3 flat = new Vector3(areaCenter.x + p.x, 0f, areaCenter.z + p.y);
        foreach (var s in sites)
            if (FlatDistance(flat, s.center) < MinCenterDist) return false;

        if (!TryGetSandPoint(p, out center, out normal)) return false;
        if (Vector3.Angle(normal, Vector3.up) > maxSlopeAngle) return false;

        Vector2 offset = RandomPointInDisc(rng, siteRadius * hiddenItemMaxOffset);
        return TryGetSandPoint(p + offset, out hidden, out _);
    }

    // Uniform by area across the ring (sqrt keeps points from bunching toward the middle).
    static Vector2 RandomPointInRing(System.Random rng, float minR, float maxR)
    {
        float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
        float r = Mathf.Sqrt(Mathf.Lerp(minR * minR, maxR * maxR, (float)rng.NextDouble()));
        return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
    }

    static Vector2 RandomPointInDisc(System.Random rng, float radius)
    {
        float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
        float r = Mathf.Sqrt((float)rng.NextDouble()) * radius;
        return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
    }

    bool TryGetSandPoint(Vector2 localXZ, out Vector3 point, out Vector3 normal)
    {
        Vector3 origin = new Vector3(areaCenter.x + localXZ.x, raycastStartY, areaCenter.z + localXZ.y);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // First thing hit must be sand
            if ((sandLayer.value & (1 << hit.collider.gameObject.layer)) == 0)
            {
                point = default; normal = Vector3.up;
                return false;
            }
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = new Vector3(origin.x, fallbackSandY, origin.z);
        normal = Vector3.up;
        return true;
    }

    void SpawnMarkers()
    {
        if (siteMarkerPrefab == null) return;

        foreach (var s in sites)
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, s.surfaceNormal);
            Vector3 pos = s.center + s.surfaceNormal * markerHeightOffset;
            GameObject m = Instantiate(siteMarkerPrefab, pos, rot, transform);
            float d = s.radius * 2f;
            m.transform.localScale = Vector3.Scale(m.transform.localScale, new Vector3(d, 1f, d));
            m.name = $"SandSite_{s.id}";
            s.marker = m;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.6f, 0.6f);
        DrawCircle(areaCenter, sonarRadius);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        DrawCircle(areaCenter, exclusionRadius);

        foreach (var s in sites)
        {
            Gizmos.color = s.itemType switch
            {
                SandItemType.PuzzleStarter => Color.yellow,
                SandItemType.JumpScare => Color.red,
                _ => Color.cyan
            };
            DrawCircle(s.center, s.radius);
            Gizmos.DrawSphere(s.hiddenItemPosition, 0.3f);
        }
    }

    static void DrawCircle(Vector3 c, float r, int segments = 48)
    {
        Vector3 prev = c + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}