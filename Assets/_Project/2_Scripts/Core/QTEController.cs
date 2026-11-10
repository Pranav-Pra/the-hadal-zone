using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Runs the drain quicktime event on a clicked creature using the bottom drain bar.
// While it's running, every click is an attempt. Moving the camera is the only way to cancel.
public class QTEController : MonoBehaviour
{
    [SerializeField] private Camera robotCamera;
    [SerializeField] private DrainBar drainBar;

    [Header("Sounds")]
    [SerializeField] private AudioClip hitSound;    // a hit that doesn't finish the drain
    [SerializeField] private AudioClip missSound;   // a wrong click that isn't the last strike
    [SerializeField] private AudioClip failSound;   // the last strike
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;
    [SerializeField] private AudioClip heartbeatLoop;   // loops while a drain is running
    [SerializeField, Range(0f, 1f)] private float heartbeatVolume = 0.6f;
    [SerializeField] private AudioSource ambience;                         // the looping water sounds
    [SerializeField, Range(0f, 1f)] private float ambienceDuckAmount = 0.3f;   // volume during a drain, as a fraction of normal
    [SerializeField, Min(0.01f)] private float ambienceFadeTime = 0.5f;        // seconds to fade down or back up

    private float ambienceBaseVolume;
    private AudioSource loopSource;
    private AudioSource audioSource;

    private ChargeSource target;
    private int startFrame;
    private Vector3 lockedCamPos;
    private Quaternion lockedCamRot;
    private int misses;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D, like UI sounds

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 0f;

        if (ambience != null) ambienceBaseVolume = ambience.volume;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip, soundVolume);
    }

    public bool IsActive => target != null;

    void Start() => drainBar.Hide();

    public void Begin(ChargeSource source)
    {
        if (IsActive || source.IsDrained || source.OnCooldown) return;
        if (source.Pattern == null) { Debug.LogWarning($"{source.name} has no Drain Pattern assigned", source); return; }

        target = source;
        misses = 0;
        startFrame = Time.frameCount;   // so the click that started it doesn't count as an attempt
        target.SetFrozen(true);

        lockedCamPos = robotCamera.transform.position;
        lockedCamRot = robotCamera.transform.rotation;

        drainBar.Show(target.Pattern, target.GlowFraction);

        if (heartbeatLoop != null)
        {
            loopSource.clip = heartbeatLoop;
            loopSource.volume = heartbeatVolume;
            loopSource.Play();
        }
    }

    void Update()
    {
        if (!IsActive) return;

        // switching to the top-down view cancels the drain
        if (!robotCamera.isActiveAndEnabled) { Cancel(); return; }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Time.frameCount == startFrame) return;

        // clicks on UI buttons don't count as attempts
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // any click on the screen is an attempt to stop the spark in the zone
        if (drainBar.SparkInZone)
        {
            target.RegisterHit();
            drainBar.PlayHit(target.GlowFraction);

            if (target.IsDrained) End();
            else Play(hitSound);
        }
        else
        {
            misses++;
            if (misses >= target.Pattern.maxMisses)
            {
                Play(failSound);
                StrikeOut();
            }
            else
            {
                Play(missSound);
                drainBar.PlayMiss();
            }
        }
    }

    void LateUpdate()
    {
        if (ambience != null)
        {
            float targetVolume = IsActive ? ambienceBaseVolume * ambienceDuckAmount : ambienceBaseVolume;
            ambience.volume = Mathf.MoveTowards(ambience.volume, targetVolume, ambienceBaseVolume / ambienceFadeTime * Time.deltaTime);
        }

        if (!IsActive)
        {
            if (loopSource.isPlaying) loopSource.Stop();

            // creature was destroyed mid-drain
            if (target == null && drainBar.isActiveAndEnabled && !drainBar.IsInvoking()) drainBar.Hide();
            return;
        }

        // any camera movement or rotation cancels the drain
        bool moved = (robotCamera.transform.position - lockedCamPos).sqrMagnitude > 0.0001f;
        bool turned = Quaternion.Angle(robotCamera.transform.rotation, lockedCamRot) > 0.01f;
        if (moved || turned) Cancel();
    }

    // Drained: show the flatline, and let the now-dark creature swim off
    private void End()
    {
        target.SetFrozen(false);
        target = null;
        drainBar.Finish();
    }

    // Cancelled: hide the bar and let the creature move again; its brightness stays where it was
    private void Cancel()
    {
        target.SetFrozen(false);
        target = null;
        drainBar.Hide();
    }

    private void StrikeOut()
    {
        target.StartCooldown();
        target.SetFrozen(false);
        target = null;
        drainBar.Fail();
    }
}