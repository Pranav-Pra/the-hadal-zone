using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroDirector : MonoBehaviour
{
    public DescentController descent;
    public TransmissionBox box;
    public Behaviour pivotControls;   // your camera turn/tilt script

    public GameObject settingsArrow;
    public GameObject gpsArrow;
    public GameObject chargeArrow;

    [Header("Midnight Zone")]
    public GameObject jellyfishPrefab;
    public float jellyDistance = 6f;     // how far in front of the camera
    public float jellyHeight = 0.5f;     // slightly above eye level
    GameObject jellyfish;

    [Header("Abyssal Zone")]
    public Button viewButton;          // your Traversal/Recall View toggle
    public GameObject viewArrow;       // arrow pointing at it
    public float recallSettle = 1.5f;  // wait for the recall camera move to finish
    bool viewClicked;

    [Header("Hadal Zone")]
    public Transform shakeTarget;          // your first-person camera
    public Image blackout;                 // full-screen black Image
    public AudioSource sfx;
    public AudioClip crashClip;
    public string gameplayScene = "Hadal";
    bool crashed;


    IEnumerator Start()
    {
        blackout.color = new Color(0f, 0f, 0f, 0f);

        settingsArrow.SetActive(false);
        gpsArrow.SetActive(false);
        chargeArrow.SetActive(false);

        viewArrow.SetActive(false);
        viewButton.interactable = false;                       // no switching views mid-dive
        viewButton.onClick.AddListener(() => viewClicked = true);

        if (pivotControls) pivotControls.enabled = false;

        yield return SunlightZone();
        yield return TwilightZone();
        yield return MidnightZone();
        yield return AbyssalZone();
        yield return HadalZone();
    }

    IEnumerator SunlightZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(0));

        yield return new WaitForSeconds(1f);
        yield return box.Type("DEPLOYED HADAL AUTONOMOUS DEEP-EARTH SURVEY, <b>HADES</b>.");
        yield return box.Type("ENTERING THE SUNLIGHT ZONE.");

        if (pivotControls) pivotControls.enabled = true;
        yield return box.Type(
            "USE <b>LEFT/RIGHT ARROW</b> OR <b>A/D</b> TO ROTATE THE CAMERA.\n" + "\n" + 
            "USE <b>UP/DOWN ARROW</b> OR <b>W/S</b> TO TILT THE CAMERA.", 0f);   // stays up with the button

        yield return sinking;
        yield return box.WaitForButton("DESCEND");
    }

    IEnumerator TwilightZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(1));

        yield return box.Type("ENTERING THE TWILIGHT ZONE.");
        yield return box.Type("VERIFYING HADES FUNCTIONALITY.");

        settingsArrow.SetActive(true);
        yield return box.Type("SETTINGS...", 0f);
        yield return box.WaitForButton("NEXT");
        settingsArrow.SetActive(false);

        yield return box.Type("SETTINGS CHECK.");
        gpsArrow.SetActive(true);
        yield return box.Type("GPS...", 0f);
        yield return box.WaitForButton("NEXT");
        gpsArrow.SetActive(false);

        yield return box.Type("GPS CHECK.");
        chargeArrow.SetActive(true);
        yield return box.Type("CHARGE...", 0f);
        yield return box.WaitForButton("NEXT");
        chargeArrow.SetActive(false);

        yield return box.Type("CHARGE DEPLETED. LOCOMOTION OFFLINE.", 0f);

        yield return sinking;
        yield return box.WaitForButton("DESCEND");
    }

    IEnumerator MidnightZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(2));   // 1,000 m -> 4,000 m

        yield return box.Type("ENTERING THE MIDNIGHT ZONE.");
        yield return sinking;                     // jellyfish only appears once we've stopped

        SpawnJellyfish();
        TutorialEvents.Clear();                                  // only cleared once, here
        Coroutine respawner = StartCoroutine(RespawnJellyOnFail());

        yield return box.Type("CLICK ON FAUNA TO ENGAGE SIPHON.", 0f);
        yield return new WaitUntil(() => TutorialEvents.DrainStarted || TutorialEvents.DrainSucceeded);

        if (!TutorialEvents.DrainSucceeded)
            yield return box.Type("CLICK ANYWHERE WHEN THE SPARK IS WITHIN THE WINDOW.", 0f);

        yield return new WaitUntil(() => TutorialEvents.DrainSucceeded);   // catches success even mid-typing
        StopCoroutine(respawner);

        yield return box.Type("PARTIAL CHARGE RESTORED. LOCOMOTION ONLINE.", 0f);
        yield return box.WaitForButton("DESCEND");
    }

    // Runs in the background: every failed drain swaps in a fresh jellyfish, with no text
    IEnumerator RespawnJellyOnFail()
    {
        while (true)
        {
            yield return new WaitUntil(() => TutorialEvents.DrainFailed);
            TutorialEvents.DrainFailed = false;                  // reset only the fail flag
            yield return new WaitForSeconds(0.6f);               // let the bar's fail animation finish
            if (TutorialEvents.DrainSucceeded) yield break;

            if (jellyfish) Destroy(jellyfish);
            SpawnJellyfish();
        }
    }

    void SpawnJellyfish()
    {
        Transform cam = Camera.main.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

        Vector3 pos = cam.position + forward * jellyDistance + Vector3.up * jellyHeight;
        jellyfish = Instantiate(jellyfishPrefab, pos, Quaternion.LookRotation(forward));
        Debug.Log($"Spawned {jellyfish.name} at {pos}", jellyfish);
    }

    IEnumerator AbyssalZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(3));   // 4,000 m -> 6,000 m

        yield return box.Type("ENTERING THE ABYSSAL ZONE.");
        yield return sinking;                     // must be stopped before traversal

        // Traversal
        viewClicked = false;
        viewButton.interactable = true;
        viewArrow.SetActive(true);
        yield return box.Type("CLICK <b>BUTTON BELOW TRAVERSE</b> TO MOVE.", 0f);
        yield return new WaitUntil(() => viewClicked);
        viewArrow.SetActive(false);
        viewButton.interactable = false;          // can't switch back until told to

        yield return box.Type("USE ARROW KEYS OR <b>WASD</b> TO ROTATE AND MOVE. \n \n COORDINATE VALUES SHOULD CHANGE.", 0f);
        yield return new WaitUntil(MovementKeyPressed);
        yield return box.Type("SONAR ON BOTTOM RIGHT DISPLAYS CHARGE HOTSPOTS ON SEAFLOOR.");
        yield return box.Type("DETECTOR ON BOTTOM DETECTS SEAFLOOR ACTIVITY.");
        yield return box.Type("CLICK DETECT TO REVEAL WHAT IS BELOW THE SEAFLOOR");
        yield return box.WaitForButton("NEXT");
        yield return box.Type("TRAVERSAL TIME IS LIMITED BY CHARGE.\n" +  "\nCHARGE DROPS OVER TIME. AT 0, RECALL IS FORCED.");

        // Recall
        viewClicked = false;
        viewButton.interactable = true;
        viewArrow.SetActive(true);
        yield return box.Type("TESTING RECALL. CLICK ON <b>BUTTON BELOW RECALL</b> TO RETURN TO ORIGINAL COORDINATES.", 0f);
        yield return new WaitUntil(() => viewClicked);
        viewArrow.SetActive(false);
        viewButton.interactable = false;

        yield return new WaitForSeconds(recallSettle);
        yield return box.WaitForButton("DESCEND");
    }

    bool MovementKeyPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        return kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
            kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
            kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
            kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
    }

    IEnumerator HadalZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(4, true));   // speeds up into the crash
        StartCoroutine(ShakeWithGlitch());

        yield return box.Type("ENTERING THE HADAL ZONE.");

        StartCoroutine(RampGlitch(0f, 0.3f, 2f));
        yield return box.Type("DETECTING UNUSUAL ACTIVITY.");

        StartCoroutine(RampGlitch(0.3f, 1f, 3f));
        yield return box.Type("SYSTEM...SYS", 0f);           // cut off mid-word

        yield return sinking;                                  // hit the bottom
        yield return Crash();
    }

    IEnumerator RampGlitch(float from, float to, float time)
    {
        for (float e = 0f; e < time; e += Time.deltaTime)
        {
            box.glitch = Mathf.Lerp(from, to, e / time);
            yield return null;
        }
        box.glitch = to;
    }

    IEnumerator ShakeWithGlitch()
    {
        Vector3 home = shakeTarget.localPosition;
        while (!crashed)
        {
            shakeTarget.localPosition = home + Random.insideUnitSphere * 0.08f * box.glitch;
            yield return null;
        }
        shakeTarget.localPosition = home;
    }

    IEnumerator Crash()
    {
        crashed = true;
        blackout.color = Color.black;                          // hard cut
        if (crashClip) sfx.PlayOneShot(crashClip);

        yield return new WaitForSeconds(0.8f);                 // static still roaring in the dark
        box.staticSource.Stop();

        yield return new WaitForSeconds(2f);                   // silence
        SceneManager.LoadScene(gameplayScene);
    }
}