using UnityEngine;
using UnityEngine.InputSystem;
 
// Anchored first-person look for the robot
// Mouse left/right spins the robot 360 degrees
// Mouse up/down tilts only the camera
public class RobotLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;   // the Main Camera (child of the robot)
 
    [Header("Look Settings")]
    [SerializeField] private float turnSpeed = 90f;   // degrees per second, left/right
    [SerializeField] private float tiltSpeed = 60f;   // degrees per second, up/down
    [SerializeField] private float minPitch = -60f;   // how far down you can look
    [SerializeField] private float maxPitch = 60f;    // how far up you can look

    [Header("Sounds (all optional)")]
    [SerializeField] private AudioClip moveLoopClip;  // loops while turning or tilting
    [SerializeField] private AudioClip tiltLimitClip; // plays once when tilt hits the limit
    [SerializeField, Range(0f, 1f)] private float moveVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float limitVolume = 0.7f;
    [SerializeField] private float fadeTime = 0.08f;  // seconds to fade the loop in/out (0 = instant)
 
    private float pitch;
    private bool wasAtLimit;
 
    // Created automatically, so you only need to assign the clips above
    private AudioSource moveSource;
    private AudioSource limitSource;
 
    void Awake()
    {
        moveSource  = CreateSource(moveLoopClip, true);
        limitSource = CreateSource(null, false);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
 
        // Left/Right: -1, 0, or 1
        float turn = 0f;
        if (keyboard.leftArrowKey.isPressed  || keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) turn += 1f;
 
        // Up/Down: -1, 0, or 1
        float tilt = 0f;
        if (keyboard.upArrowKey.isPressed   || keyboard.wKey.isPressed) tilt += 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) tilt -= 1f;
 
        // Rotate the whole robot around its vertical axis (full 360)
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
 
        // Tilt only the camera, clamped (negative pitch looks up in Unity)
        float newPitch = Mathf.Clamp(pitch - tilt * tiltSpeed * Time.deltaTime, minPitch, maxPitch);
        bool isTilting = !Mathf.Approximately(newPitch, pitch);  // false when stuck at a limit
        pitch = newPitch;
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
 
        // sounds
        UpdateMoveLoop(turn != 0f || isTilting);
 
        // Clunk once when the player pushes into the limit, not every frame
        bool atLimit = tilt != 0f && !isTilting;
        if (atLimit && !wasAtLimit && tiltLimitClip != null)
            limitSource.PlayOneShot(tiltLimitClip, limitVolume);
        wasAtLimit = atLimit;
    }
 
    private AudioSource CreateSource(AudioClip clip, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D: the player is the robot, so no positional audio needed
        return source;
    }
 
    private void UpdateMoveLoop(bool shouldPlay)
    {
        if (moveSource.clip == null) return;
 
        // How much volume can change this frame (instant if fadeTime is 0)
        float step = fadeTime > 0f ? Time.deltaTime / fadeTime : 1f;
 
        if (shouldPlay)
        {
            if (!moveSource.isPlaying)
            {
                // Start at a random point so each movement sounds a little different
                moveSource.timeSamples = Random.Range(0, moveSource.clip.samples);
                moveSource.volume = 0f;
                moveSource.Play();
            }
            // Quick fade in (also avoids a pop when starting mid-clip)
            moveSource.volume = Mathf.MoveTowards(moveSource.volume, moveVolume, step);
        }
        else if (moveSource.isPlaying)
        {
            // Quick fade out, then stop
            moveSource.volume = Mathf.MoveTowards(moveSource.volume, 0f, step);
            if (moveSource.volume <= 0f) moveSource.Stop();
        }
    }
}
 