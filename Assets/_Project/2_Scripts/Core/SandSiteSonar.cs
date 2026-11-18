using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the sand search circles and the metal detector's own dot on the existing Radar.
/// </summary>
public class SandSiteSonar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Radar radar;
    [SerializeField] SandSiteSpawner spawner;
    [Tooltip("UI Image prefab, Anchors and pivot must be (0.5, 0.5).")]
    [SerializeField] RectTransform siteDotPrefab;
    [Tooltip("The player's own dot, Child of the radar center, anchors and pivot (0.5, 0.5)]")]
    [SerializeField] RectTransform detectorDot;

    [Header("Detector position")]
    [Tooltip("Sea-floor camera")]
    [SerializeField] Camera seaFloorCamera;
    [SerializeField] LayerMask sandLayer = ~0;
    [SerializeField] float maxRayDistance = 700f;

    [Header("Visibility")]
    [Tooltip("leave off and call SetVisible(true) when the sonar puzzle is solved.")]
    [SerializeField] bool showOnStart = false;
    [Tooltip("hide a site's dot once its item has been pulled out of the sand.")]
    [SerializeField] bool hideRevealedSites = true;
    [Tooltip("On: dots partly over the rim stay visible and Off: a dot only shows when it's fully inside the disc.")]
    [SerializeField] bool clipAtEdge = true;

    readonly List<SandSite> siteList = new List<SandSite>();
    readonly List<RectTransform> siteDots = new List<RectTransform>();
    bool visible;

    /// <summary>World point currently under the metal detector (center of the screen).</summary>
    public Vector3 DetectorWorldPosition { get; private set; }
    public bool HasDetectorPosition { get; private set; }
    /// <summary>The site the detector is inside right now, or null.</summary>
    public SandSite CurrentSite { get; private set; }

    // One scale for everything on the disc: Radar's range maps to the disc's measured radius.
    float PixelsPerUnit => radar.RadarRadius / radar.DetectionRadius;

    void OnEnable()  { if (spawner != null) spawner.OnSitesGenerated += BuildDots; }
    void OnDisable() { if (spawner != null) spawner.OnSitesGenerated -= BuildDots; }

    void Start()
    {
        if (radar.DetectionRadius < spawner.SonarRadius)
            Debug.LogWarning($"[SandSiteSonar] Radar range ({radar.DetectionRadius}) is smaller than the spawn range ({spawner.SonarRadius}); some circles won't show even from (0,0,0).");

        // The spawner may have already generated before we subscribed.
        if (spawner.Sites.Count > 0) BuildDots(spawner.Sites);
        SetVisible(showOnStart);
    }

    public void SetVisible(bool value)
    {
        visible = value;
        if (detectorDot != null) detectorDot.gameObject.SetActive(value);
        RefreshDotVisibility();
    }

    void BuildDots(IReadOnlyList<SandSite> sites)
    {
        foreach (var d in siteDots) if (d != null) Destroy(d.gameObject);
        siteDots.Clear();
        siteList.Clear();

        float ppu = PixelsPerUnit;
        foreach (var site in sites)
        {
            RectTransform dot = Instantiate(siteDotPrefab, radar.RadarCenter);
            dot.name = $"SiteDot_{site.id}";
            float diameterPx = site.radius * 2f * ppu;
            dot.sizeDelta = new Vector2(diameterPx, diameterPx);
            dot.anchoredPosition = WorldToRadar(site.center);
            siteList.Add(site);
            siteDots.Add(dot);
        }

        // Keep the player's dot drawn on top of the circles.
        if (detectorDot != null) detectorDot.SetAsLastSibling();
        RefreshDotVisibility();
    }

    void Update()
    {
        UpdateDetectorPosition();

        if (HasDetectorPosition)
        {
            CurrentSite = spawner.GetSiteContaining(DetectorWorldPosition);
            if (detectorDot != null)
                detectorDot.anchoredPosition = Vector2.ClampMagnitude(WorldToRadar(DetectorWorldPosition), radar.RadarRadius);
        }
        else
        {
            CurrentSite = null;
        }

        // The robot moves, so every site dot is re-placed relative to it each frame.
        // Size is refreshed too, in case the canvas scale or screen size changes.
        float ppu = PixelsPerUnit;
        for (int i = 0; i < siteDots.Count; i++)
        {
            float d = siteList[i].radius * 2f * ppu;
            siteDots[i].sizeDelta = new Vector2(d, d);
            siteDots[i].anchoredPosition = WorldToRadar(siteList[i].center);
        }

        RefreshDotVisibility();
    }

    void UpdateDetectorPosition()
    {
        if (seaFloorCamera == null) { HasDetectorPosition = false; return; }

        Ray ray = seaFloorCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, sandLayer, QueryTriggerInteraction.Ignore))
        {
            DetectorWorldPosition = hit.point;
            HasDetectorPosition = true;
        }
        else
        {
            HasDetectorPosition = false; // keeps the last good dot position on screen
        }
    }

    void RefreshDotVisibility()
    {
        for (int i = 0; i < siteDots.Count; i++)
        {
            bool show = visible
                        && !(hideRevealedSites && siteList[i].revealed)
                        && IsOnDisc(siteDots[i]);
            if (siteDots[i].gameObject.activeSelf != show) siteDots[i].gameObject.SetActive(show);
        }
    }

    // Is the dot on the radar disc at the robot's current position?
    bool IsOnDisc(RectTransform dot)
    {
        float centerDist = dot.anchoredPosition.magnitude;
        float dotRadius = dot.sizeDelta.x * 0.5f;
        return clipAtEdge
            ? centerDist - dotRadius < radar.RadarRadius    // any part touching the disc
            : centerDist + dotRadius <= radar.RadarRadius;  // completely inside
    }

    // Offset from the robot, flat XZ. With headingUp, rotated so the robot's forward is up on the sonar.
    Vector2 WorldToRadar(Vector3 worldPos)
    {
        Vector3 offset = worldPos - radar.Player.position;
        if (radar.HeadingUp)
            offset = Quaternion.Euler(0f, -radar.Player.eulerAngles.y, 0f) * offset;
        return new Vector2(offset.x, offset.z) * PixelsPerUnit;
    }
}