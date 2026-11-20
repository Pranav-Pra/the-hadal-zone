using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Radar : MonoBehaviour
{
    [SerializeField] GameObject detector;
    [SerializeField] int detectorRotationSpeed;

    [SerializeField] Transform player;
    [SerializeField] RectTransform radarCenter;
    [SerializeField] GameObject enemyDetectionPrefab;
    [Tooltip("World range shown on the radar")]
    [SerializeField] float detectionRadius = 40f;

    [Header("Radar size on screen")]
    [Tooltip("The round radar background image")]
    [SerializeField] RectTransform radarDisc;
    [Tooltip("how much of the disc image's width is the actual sonar circle]")]
    [SerializeField, Range(0.3f, 1f)] float discFill = 1f;
    [SerializeField] float radarRadius = 100f;

    [Tooltip("Rotate the radar with the robot so its forward is always up")]
    [SerializeField] bool headingUp = true;

    [Tooltip("Objects with these tags show up on the radar.")]
    [SerializeField] string[] trackedTags = { "Enemy", "PuzzleObject", "Algae"};

    [SerializeField] AudioClip pingSound;
    [SerializeField] AudioSource source;

    [Header("Blink")]
    [Tooltip("Flashes per second")]
    [SerializeField] float blinkSpeed = 1.5f;

    [Header("Signal loss")]
    [Tooltip("Turn off to keep the dots on the radar all the time.")]
    [SerializeField] bool signalDrops = true;
    [Tooltip("Random time (min, max seconds) the dots stay visible before the signal drops.")]
    [SerializeField] Vector2 visibleTime = new Vector2(15f, 30f);
    [Tooltip("Random time (min, max seconds) the dots stay gone before they come back.")]
    [SerializeField] Vector2 hiddenTime = new Vector2(10f, 25f);

    private bool dotsVisible = true;
    private float signalTimer;

    private List<GameObject> enemiesList;
    private readonly Vector3[] discCorners = new Vector3[4];

    // Read by SandSiteSonar so both scripts share one center, size, range and robot.
    public float RadarRadius => radarDisc != null ? MeasureDiscRadius() : radarRadius;

    void Awake()
    {
        enemiesList = new List<GameObject>();
        signalTimer = Random.Range(visibleTime.x, visibleTime.y);
    }

    private void Update()
    {
        detector.transform.Rotate(0f, 0f, -detectorRotationSpeed * Time.deltaTime);
        UpdateSignal();
        CheckEnemy();
    }

    private void UpdateSignal()
    {
        if (!signalDrops)
        {
            dotsVisible = true;
            return;
        }

        signalTimer -= Time.deltaTime;
        if (signalTimer > 0f) return;

        dotsVisible = !dotsVisible;
        signalTimer = dotsVisible
            ? Random.Range(visibleTime.x, visibleTime.y)
            : Random.Range(hiddenTime.x, hiddenTime.y);
    }

    private float MeasureDiscRadius()
    {
        radarDisc.GetWorldCorners(discCorners); // 0 = bottom-left, 3 = bottom-right
        float worldHalfWidth = Vector3.Distance(discCorners[0], discCorners[3]) * 0.5f * discFill;
        return radarCenter.InverseTransformVector(radarDisc.right * worldHalfWidth).magnitude;
    }

    private void CheckEnemy()
    {
        foreach (GameObject enemy in enemiesList)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        enemiesList.Clear();
        if (!dotsVisible) return;

        float pixelRadius = RadarRadius;

        foreach (string tag in trackedTags)
        foreach (var enemy in GameObject.FindGameObjectsWithTag(tag))
        {
            Vector3 offset = enemy.transform.position - player.position;
            if (headingUp)
                offset = Quaternion.Euler(0f, -player.eulerAngles.y, 0f) * offset;
            Vector2 flat = new Vector2(offset.x, offset.z);
            float distance = flat.magnitude;
            if (detectionRadius > 0f && distance <= detectionRadius)
            {
                Vector2 pos = flat / detectionRadius * pixelRadius;
                GameObject enemyPrefab = Instantiate(enemyDetectionPrefab, radarCenter);
                // Don't uncomment as-is: this runs every frame, so it would ping ~60 times a second per enemy.
                //source.PlayOneShot(pingSound);

                enemyPrefab.GetComponent<RectTransform>().anchoredPosition = pos;
                enemiesList.Add(enemyPrefab);

                // fade the dot in and out over time
                float alpha = (Mathf.Sin(Time.time * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                Graphic dot = enemyPrefab.GetComponent<Graphic>();
                if (dot != null)
                {
                    Color c = dot.color;
                    c.a = alpha;
                    dot.color = c;
                }
            }
        }
    }
}
