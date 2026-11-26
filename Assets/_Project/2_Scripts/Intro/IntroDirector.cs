using System.Collections;
using UnityEngine;

public class IntroDirector : MonoBehaviour
{
    public DescentController descent;
    public TransmissionBox box;
    public Behaviour pivotControls;   // your camera turn/tilt script

    IEnumerator Start()
    {
        if (pivotControls) pivotControls.enabled = false;

        yield return SunlightZone();
        yield return TwilightZone();

        // TEMP: remaining zones with no text
        for (int i = 2; i < descent.zones.Length; i++)
            yield return descent.DescendZone(i);
    }

    IEnumerator SunlightZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(0));

        yield return new WaitForSeconds(1f);
        yield return box.Type("Deployed Hadal Autonomous Deep-Earth Survey, <b>HADES</b>.");
        yield return box.Type("Entering the Sunlight Zone.");
        yield return box.Type("Testing pivot movement.");

        if (pivotControls) pivotControls.enabled = true;
        yield return box.Type(
            "Use <b>LEFT/RIGHT ARROW</b> or <b>A/D</b> to rotate the camera.\n" + "\n" + 
            "Use <b>UP/DOWN ARROW</b> or <b>W/S</b> to tilt the camera.", 0f);   // stays up with the button

        yield return sinking;
        yield return box.WaitForButton("DESCEND");
    }

    IEnumerator TwilightZone()
    {
        box.Clear();
        Coroutine sinking = StartCoroutine(descent.DescendZone(1));   // 200 m -> 1,000 m

        yield return box.Type("Entering the Twilight Zone.");
        yield return box.Type("Verifying HADES functionality.");

        // arrows.Show("Settings");
        yield return box.Type("Settings...", 0f);
        yield return box.WaitForButton("NEXT");
        // arrows.HideAll();

        yield return box.Type("Settings check.");
        // arrows.Show("Coordinates");
        yield return box.Type("GPS...", 0f);
        yield return box.WaitForButton("NEXT");
        // arrows.HideAll();

        yield return box.Type("GPS check.");
        // arrows.Show("Charge");
        yield return box.Type("Charge...", 0f);
        yield return box.WaitForButton("NEXT");
        // arrows.HideAll();

        yield return box.Type("Charge depleted. Locomotion offline.", 0f);

        yield return sinking;                     // make sure we've reached 1,000 m
        yield return box.WaitForButton("DESCEND");
    }
}