using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Sonar : MonoBehaviour
{
    private bool displayingPuzzle = false;
    private bool puzzleGenerated = false;
    private bool puzzleDisplayed = false;
    private bool firstDisplay = false;
    private Coroutine puzzleCoroutine;
    private Quaternion initialDetectorRotation;
    private float currentDetectorAngle;
    private List<int> generatedPattern;
    private List<PatternCords> translatedPattern;
    private List<GameObject> spawnedIndicators = new List<GameObject>();


    [Header("Sonar Game Object References and Values")]
    [Tooltip("The part that spins to reveal puzzle pattern.")]
    [SerializeField] GameObject detector;
    [Tooltip("How fast the detector object moves.")]
    [SerializeField] float detectorRotationSpeed = 10f;
    [Tooltip("Rescan Text Reference")]
    [SerializeField] TextMeshProUGUI rescanText;
    [Tooltip("How many Rescans are Available")]
    [SerializeField] private int rescanCount = 3;

    [Header("Sonar Effect Elements")]
    [Tooltip("ParticleSystem for displaying where to click.")]
    [SerializeField] ParticleSystem radarParticlePrefab;
    [Tooltip("Sound played when displaying pattern.")]
    [SerializeField] AudioClip radarSound;
    [Tooltip("Sound Maker")]
    [SerializeField] AudioSource radarPlayer;

    [Header("Game References")]
    [Tooltip("Reference to Scriptable Object.")]
    [SerializeField] PuzzleCompletion puzzleCompletion;

    [SerializeField] GameObject testPrefab;

    void Awake()
    {
        initialDetectorRotation = detector.transform.localRotation;
    }

    void Update()
    {
        if (!firstDisplay)
        {
            puzzleCoroutine = StartCoroutine(Puzzle()); 
            firstDisplay = true;
        }
    }

    public void Rescan()
    {
        if (firstDisplay && rescanCount > 0)
        {
            StopAllCoroutines();
            ClearIndicators();

            if (translatedPattern != null)
            {
                for (int i = 0; i < translatedPattern.Count; i++)
                {
                    PatternCords cord = translatedPattern[i];
                    cord.displayed = false;
                    translatedPattern[i] = cord;
                }
            }
            displayingPuzzle = false;
            puzzleDisplayed = false;
            currentDetectorAngle = 0f;

            detector.transform.localRotation = initialDetectorRotation;

            puzzleCoroutine = StartCoroutine(Puzzle());
            rescanCount -= 1;
            rescanText.text = "Rescans: " + rescanCount;
        }
        if(rescanCount == 0)
        {
            rescanText.text = "Out of Rescans.";
        }
    }

    private IEnumerator Puzzle()
    {
        displayingPuzzle = true;
        int previousAngle = -1;
        while (displayingPuzzle)
        {
            if (!puzzleGenerated)
            {
                generatedPattern = GeneratePattern(5);
                translatedPattern = TranslatePattern(generatedPattern);
                puzzleGenerated = true;
            }
            else if(!puzzleDisplayed && puzzleGenerated)
            {
                if (!detector.activeSelf)
                {
                    detector.SetActive(true);
                }
                detector.transform.Rotate(0f, 0f, -detectorRotationSpeed * Time.deltaTime);
                currentDetectorAngle += detectorRotationSpeed * Time.deltaTime;

                // The detector rotates clockwise (negative Z), so the wedge it
                // actually points at is the mirror of currentDetectorAngle.
                // Use its real facing angle or spawns won't line up with the line.
                float detectorFacing = Mathf.Repeat(detector.transform.eulerAngles.z, 360f);
                int currentAngle = Mathf.FloorToInt(detectorFacing / 45f) % 8;

                // Only spawn once when the detector enters a new wedge
                if (currentAngle != previousAngle)
                {
                    DisplayCord(translatedPattern, currentAngle);
                }
                previousAngle = currentAngle;

                // Stop displaying after one full sweep
                if (currentDetectorAngle >= 360f)
                {
                    puzzleDisplayed = true;
                    StartCoroutine(DeleteIndicators(2f));
                }
                
            }
            if (puzzleDisplayed)
            {
                detector.SetActive(false);
            }
            yield return null;
        }
        
    }

    // Generates patterns for the simon says like gameplay of the puzzle
    List<int> GeneratePattern(int amount)
    {
        List<int> pattern = new List<int>();
        while (pattern.Count < amount)
        {
            int number = Random.Range(1, 25);
            if (!pattern.Contains(number))
            {
                pattern.Add(number);
            }
        }
        pattern.Sort();
        return pattern;
    }

    // Creates a list translating the List of Ints to place on the sonar
    List<PatternCords> TranslatePattern(List<int> pattern)
    { 
        List<PatternCords> patternCords = new List<PatternCords>();
        foreach(int place in pattern)
        {
            int angle = (place - 1) / 3; // 0 -> 7
            int ring;
            // Outer Ring
            if(place % 3 == 1)
            {
                ring = 250;
            }
            // Middle Ring
            else if(place % 3 == 2)
            {
                ring = 150;
            }
            // Inner Ring
            else
            {
                ring = 75;
            }
            patternCords.Add(new PatternCords
            {
                angleCord = angle,
                ringDistance = ring,
                displayed = false
            });
        }
        return patternCords;
    }

    void DisplayCord(List<PatternCords> patternCord, int currentAngle)
    {
        for (int i = 0; i < patternCord.Count; i++)
        {
            PatternCords cord = patternCord[i];
            if((currentAngle + 7) % 8 == cord.angleCord && !cord.displayed)
            {
                // PatternCords is a struct, so mark the list entry before starting
                // the delayed coroutine to prevent another spawn when the sweep wraps.
                cord.displayed = true;
                patternCord[i] = cord;
                StartCoroutine(InstantiateObjects(cord));
            }
        }
    }

    private IEnumerator InstantiateObjects(PatternCords cord)
    {
        yield return new WaitForSeconds(0.25f);
        float angleInRadians = (cord.angleCord * 45f - 22.5f + 180f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians), 0f) * cord.ringDistance;

        GameObject test = Instantiate(testPrefab, detector.transform.parent);
        test.transform.localPosition = offset;
        spawnedIndicators.Add(test);

                /*
                // Spawn under the Visual object so the particle sits on top of the Radar piece
                ParticleSystem radarParticle = Instantiate(radarParticlePrefab, detector.transform.parent);

                radarParticle.transform.localPosition = offset;

                // Give the particle its own Canvas so it renders inside the UI hierarchy
                Canvas particleCanvas = radarParticle.gameObject.AddComponent<Canvas>();
                particleCanvas.overrideSorting = true;
                particleCanvas.sortingOrder = 10;

                radarParticle.Play(true);
                */

        radarPlayer.PlayOneShot(radarSound);
    }

    private IEnumerator DeleteIndicators(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ClearIndicators();
    }

    private void ClearIndicators()
    {
        foreach (GameObject indicator in spawnedIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        spawnedIndicators.Clear();
    }

    // Updates Scriptable Object
    void CompletePuzzle()
    {
        puzzleCompletion.sonar = true;
    }
}


struct PatternCords
{
    public int angleCord;
    public int ringDistance;
    public bool displayed;
} 
