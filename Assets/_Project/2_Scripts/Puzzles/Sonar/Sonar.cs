using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sonar : MonoBehaviour
{
    private bool displayingPuzzle = false;
    private bool puzzleGenerated = false;
    private bool puzzleDisplayed = false;
    private float currentDetectorAngle;
    private List<int> generatedPattern;
    private List<PatternCords> translatedPattern;


    [Header("Sonar Game Object References and Values")]
    [Tooltip("The part that spins to reveal puzzle pattern.")]
    [SerializeField] GameObject detector;
    [Tooltip("How fast the detector object moves.")]
    [SerializeField] float detectorRotationSpeed = 10f;

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

    void Update()
    {
        if (!displayingPuzzle)
        {
            StartCoroutine(Puzzle()); 
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
                Debug.Log(translatedPattern);
            }
            else if(!puzzleDisplayed && puzzleGenerated)
            {
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
                if (currentDetectorAngle >= 355f)
                {
                    puzzleDisplayed = true;
                }
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
                ring = 417;
            }
            // Middle Ring
            else if(place % 3 == 2)
            {
                ring = 250;
            }
            // Inner Ring
            else
            {
                ring = 83;
            }
            patternCords.Add(new PatternCords
            {
                angleCord = angle,
                ringDistance = ring
            });
        }
        return patternCords;
    }

    void DisplayCord(List<PatternCords> patternCord, int currentAngle)
    {
        foreach(PatternCords cord in patternCord)
        {
            if(currentAngle == cord.angleCord)
            {
                // Add half a wedge (22.5°) so the particle sits in the center
                // of the wedge instead of exactly on the boundary line
                float angleInRadians = (cord.angleCord * 45f-22.5f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians), 0f) * cord.ringDistance;

                // Spawn under the Visual object so the particle sits on top of the Radar piece
                ParticleSystem radarParticle = Instantiate(radarParticlePrefab, detector.transform.parent);

                radarParticle.transform.localPosition = offset;

                // Give the particle its own Canvas so it renders inside the UI hierarchy
                Canvas particleCanvas = radarParticle.gameObject.AddComponent<Canvas>();
                particleCanvas.overrideSorting = true;
                particleCanvas.sortingOrder = 10;

                radarParticle.Play(true);

                radarPlayer.PlayOneShot(radarSound);
            }
        }
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
} 
