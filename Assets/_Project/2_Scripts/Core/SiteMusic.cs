using UnityEngine;

// Plays music while the player's box touches any sand site circle (revealed or not).
// Put this on any object, e.g. the same object as SandSiteSpawner.
public class SiteMusic : MonoBehaviour
{
    [SerializeField] SandSiteSpawner spawner;
    [SerializeField] BoxCollider playerBox;      // the box collider on RobotArm
    [SerializeField] GameObject puzzleObject;    // NEW: the SonarObject (tag PuzzleObject). Optional
    [SerializeField] AudioSource musicSource;    // set its Clip and tick Loop in the Inspector
    [SerializeField, Range(0f, 1f)] float musicVolume = 1f;
    [SerializeField] float fadeInTime = 1f;      // seconds (0 = instant)
    [SerializeField] float fadeOutTime = 1f;     // seconds (0 = instant)
    [Tooltip("Also scale by the saved volume slider value.")]
    [SerializeField] bool useVolumeSlider = true;

    void Update()
    {
        bool inside = IsTouchingAnySite();

        float target = inside ? musicVolume : 0f;
        if (useVolumeSlider) target *= PlayerPrefs.GetFloat(VolumeSlider.SaveKey, 1f);

        // Entered a circle: start from silence and fade in
        if (inside && !musicSource.isPlaying)
        {
            musicSource.volume = 0f;
            musicSource.Play();
        }

        float fade = inside ? fadeInTime : fadeOutTime;
        float step = fade > 0f ? Time.deltaTime / fade : 1f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, step);

        // Left every circle and fully faded out: stop
        if (!inside && musicSource.isPlaying && musicSource.volume <= 0f)
            musicSource.Stop();
    }

    // CHANGED: True if any part of the box is inside the puzzle circle
    bool IsTouchingAnySite()
    {
        if (spawner == null || playerBox == null) return false;
        if (puzzleObject != null && !puzzleObject.activeInHierarchy) return false; // NEW: puzzle object removed, no music

        foreach (SandSite site in spawner.Sites)
        {
            if (site.itemType != SandItemType.PuzzleStarter) continue; // NEW: only the puzzle circle

            // the point on the box closest to the circle's center
            Vector3 closest = playerBox.bounds.ClosestPoint(site.center);
            if (site.ContainsXZ(closest)) return true;
        }
        return false;
    }
}