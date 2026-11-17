using UnityEngine;
using UnityEngine.InputSystem;

// Arrow-key walking for the top-down view
[RequireComponent(typeof(CharacterController))]
public class RobotArmMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 90f;   // degrees per second
    [SerializeField] private float gravity = -20f;

    [Header("Metal detector footprint")]
    [Tooltip("Box on RobotArm the size of the metal detector")]
    [SerializeField] private BoxCollider detectorBox;
    [Tooltip("What the detector can't pass through")]
    [SerializeField] private LayerMask blockingLayers;

    [Header("Bump sound")]
    [SerializeField] private AudioClip bumpClip;
    [SerializeField, Range(0f, 1f)] private float bumpVolume = 0.7f;

    private AudioSource bumpSource;
    private bool wasBlocked;

    private CharacterController controller;
    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = enabled;

        bumpSource = gameObject.AddComponent<AudioSource>();
        bumpSource.playOnAwake = false;
        bumpSource.spatialBlend = 0f;
    }

    void OnEnable()
    {
        if (controller == null) return;
        controller.enabled = true;
        verticalVelocity = 0f;
    }

    void OnDisable()
    {
        if (controller != null) controller.enabled = false;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool blocked = false;

        // Turning (left/right arrows)
        float turn = 0f;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) turn -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) turn += 1f;

        Vector3 pivotShift = Vector3.zero;
        float angle = turn * turnSpeed * Time.deltaTime;
        if (angle != 0f)
        {
            Quaternion spin = Quaternion.Euler(0f, angle, 0f);
            Quaternion newRotation = spin * transform.rotation;

            // box stuff
            if (BoxBlocked(transform.position + pivotShift, newRotation))
            {
                pivotShift = Vector3.zero;            
                blocked = true;
            }
            else
                transform.rotation = newRotation;
        }

        // Driving (up/down arrows or W/S) along the facing direction
        float drive = 0f;
        if (kb.upArrowKey.isPressed   || kb.wKey.isPressed) drive += 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) drive -= 1f;
        Vector3 direction = transform.forward * drive;

        // NEW: stop the box at obstacles, sliding along them when possible
        Vector3 horizontal = direction * moveSpeed * Time.deltaTime + pivotShift;
        horizontal.y = 0f;
        if (BoxBlocked(transform.position + horizontal, transform.rotation))
        {
            horizontal = Vector3.zero;
            blocked = true;
        }

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = horizontal + Vector3.up * verticalVelocity * Time.deltaTime;
        controller.Move(move);

        if (blocked && !wasBlocked && bumpClip != null) bumpSource.PlayOneShot(bumpClip, bumpVolume);
        wasBlocked = blocked;
    }

    // try the full move, then each axis on its own (slide along the obstacle), else don't move
    Vector3 LimitByBox(Vector3 horizontal)
    {
        if (detectorBox == null || horizontal == Vector3.zero) return horizontal;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        if (!BoxBlocked(pos + horizontal, rot)) return horizontal;

        Vector3 xOnly = new Vector3(horizontal.x, 0f, 0f);
        if (xOnly != Vector3.zero && !BoxBlocked(pos + xOnly, rot)) return xOnly;

        Vector3 zOnly = new Vector3(0f, 0f, horizontal.z);
        if (zOnly != Vector3.zero && !BoxBlocked(pos + zOnly, rot)) return zOnly;

        return Vector3.zero;
    }

    // NEW: would the detector box overlap a blocking collider if RobotArm were at this position/rotation?
    bool BoxBlocked(Vector3 position, Quaternion rotation)
    {
        if (detectorBox == null) return false;

        Vector3 scale = transform.lossyScale;
        Vector3 center = position + rotation * Vector3.Scale(detectorBox.center, scale);
        Vector3 halfExtents = Vector3.Scale(detectorBox.size, scale) * 0.5f;
        return Physics.CheckBox(center, halfExtents, rotation, blockingLayers, QueryTriggerInteraction.Ignore);
    }


}