using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class ViewSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject robotCamera;
    [SerializeField] private GameObject topDownCamera;
    [SerializeField] private RobotLook robotLook;
    [SerializeField] private RobotArmMovement walker;
    [SerializeField] private Transform robot;
    [SerializeField] private CoordinateDisplay coordinates;

    [Header("Charge")]
    [SerializeField] private ChargeData chargeData;      // must be full to enter seafloor view
    [SerializeField] private float drainInterval = 10f;  // seconds per segment lost in seafloor view
    [SerializeField] private int drainAmount = 1;        // segments lost each interval
    
 
    [Header("Arm Light")]
    [SerializeField] private Light armLight;             // dims as charge drains

    // new camera movement
    [Header("Transitions")]
    [Tooltip("time camera switches to the top-down view")]
    [SerializeField] private float diveTime = 0.35f;
    [Tooltip("time for magnet pull back to the base camera")]
    [SerializeField] private float recallTime = 0.8f;
    [Tooltip("when on, base camera faces the way the robot faced on the sea floor.")]
    [SerializeField] private bool keepSeafloorHeadingOnReturn = false;

    [Header("Sound (optional)")]
    [SerializeField] private AudioClip switchClip;            // plays when the button switches views
    [SerializeField, Range(0f, 1f)] private float switchVolume = 0.7f;
    [SerializeField] private AudioClip seafloorLoopClip;      // loops while in seafloor view
    [SerializeField, Range(0f, 1f)] private float seafloorVolume = 0.5f;
    [SerializeField] private float seafloorFadeInTime = 1f;   // seconds (0 = instant)
    [SerializeField] private float seafloorFadeOutTime = 1f;  // seconds (0 = instant)

    [Header("UI")]
    [SerializeField] private Button switchButton;    // the SwitchView Button component
    [SerializeField] private Image buttonImage;      // the switch view button's Image
    [SerializeField] private Sprite baseViewSprite;  // thumbnail of the base camera view
    [SerializeField] private Sprite armViewSprite;   // thumbnail of the arm camera view
    [SerializeField] private TMP_Text cameraIdText;  // the camera ID label
    [SerializeField] private string baseViewLabel = "BASE VIEW";
    [SerializeField] private string armViewLabel = "ARM VIEW";
    [SerializeField] private GameObject metalDetectorUI;

    private bool topDown;
    private float drainTimer;
    private float armLightBaseIntensity;
    private AudioSource audioSource;
    private AudioSource seafloorSource;


    // new camera movement
    private bool transitioning;
    private Vector3 armHomePosition;      // where RobotArm sits at the base
    private float armYawOffset;           // facing difference between Robot and RobotArm at start
    private Vector3 camDownLocalPos;      // TopDownCam's normal looking-down pose
    private Quaternion camDownLocalRot;

    void Awake()
    {
        if (armLight != null) armLightBaseIntensity = armLight.intensity;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D UI sound
 
        seafloorSource = gameObject.AddComponent<AudioSource>();
        seafloorSource.clip = seafloorLoopClip;
        seafloorSource.loop = true;
        seafloorSource.playOnAwake = false;
        seafloorSource.spatialBlend = 0f;

        // remember the starting setup
        Transform arm = walker.transform;
        armHomePosition = arm.position;
        armYawOffset = Mathf.DeltaAngle(robot.eulerAngles.y, arm.eulerAngles.y);
        camDownLocalPos = topDownCamera.transform.localPosition;
        camDownLocalRot = topDownCamera.transform.localRotation;
    }
    
    void OnEnable()
    {
        if (chargeData != null) chargeData.OnChanged += HandleChargeChanged;
        RefreshChargeVisuals();
    }
 
    void OnDisable()
    {
        if (chargeData != null) chargeData.OnChanged -= HandleChargeChanged;
    }

    void Start() => Apply();

    void Update()
    {
        UpdateSeafloorLoop();

        if (!topDown && !transitioning) SyncArmToBase();

        if (!topDown || chargeData == null) return;
 
        // Drain charge while in seafloor view
        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer -= drainInterval;
            chargeData.Add(-drainAmount); 
        }
    }

    public void RemoveCharge()
    {
        chargeData.Add(-drainAmount);
    }

    public int GetCharge()
    {
        return chargeData.current;
    }

    public void ToggleView()
    {
        if (transitioning) return;
        if (!topDown && !HasCharge()) return;
        if (switchClip != null) audioSource.PlayOneShot(switchClip, switchVolume);
        SetView(!topDown);
    }

    void SetView(bool toTopDown)
    {
        topDown = toTopDown;
        drainTimer = 0f;  // full interval before the first segment drains
        StopAllCoroutines();
        StartCoroutine(toTopDown ? DiveDown() : RecallToBase());
        RefreshChargeVisuals();
    }
 
    void HandleChargeChanged(int current)
    {
        // Out of charge then kick back to base view
        if (topDown && current <= 0 && !transitioning)
        {
            SetView(false);
            return;
        }
        RefreshChargeVisuals();
    }

    // Base -> top-down
    IEnumerator DiveDown()
    {
        transitioning = true;
        robotLook.enabled = false;
        walker.enabled = false;
        SyncArmToBase();

        Transform cam = topDownCamera.transform;
        Vector3 fromPos = robotCamera.transform.position;
        Quaternion fromRot = robotCamera.transform.rotation;
        Vector3 toPos = cam.parent.TransformPoint(camDownLocalPos);
        Quaternion toRot = cam.parent.rotation * camDownLocalRot;

        robotCamera.SetActive(false);
        topDownCamera.SetActive(true);
        yield return MoveCamera(cam, fromPos, fromRot, toPos, toRot, diveTime, false);

        transitioning = false;
        Apply();
    }

    // Top-down -> base, magnet pull
    IEnumerator RecallToBase()
    {
        transitioning = true;
        walker.enabled = false;
        if (metalDetectorUI != null) metalDetectorUI.SetActive(false);

        if (keepSeafloorHeadingOnReturn)
            robot.rotation = Quaternion.Euler(0f, walker.transform.eulerAngles.y - armYawOffset, 0f);

        Transform cam = topDownCamera.transform;
        yield return MoveCamera(cam, cam.position, cam.rotation,
                                robotCamera.transform.position, robotCamera.transform.rotation,
                                recallTime, true);

        cam.localPosition = camDownLocalPos;   // put TopDownCam back in its normal spot on the arm
        cam.localRotation = camDownLocalRot;
        SyncArmToBase();                       // robot is back at the base

        transitioning = false;
        Apply();
    }

     // accelerate = true: starts slow and speeds up (magnet pull). false: smooth in and out.
    IEnumerator MoveCamera(Transform cam, Vector3 fromPos, Quaternion fromRot,
                           Vector3 toPos, Quaternion toRot, float duration, bool accelerate)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            k = accelerate ? k * k * k : Mathf.SmoothStep(0f, 1f, k);
            cam.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
            yield return null;
        }
        cam.SetPositionAndRotation(toPos, toRot);
    }

    void SyncArmToBase()
    {
        walker.transform.SetPositionAndRotation(
            armHomePosition,
            Quaternion.Euler(0f, robot.eulerAngles.y + armYawOffset, 0f));
    }

    void UpdateSeafloorLoop()
    {
        if (seafloorSource.clip == null) return;
 
        if (topDown)
        {
            if (!seafloorSource.isPlaying)
            {
                seafloorSource.volume = 0f;
                seafloorSource.Play();
            }
            float step = seafloorFadeInTime > 0f ? Time.deltaTime / seafloorFadeInTime : 1f;
            seafloorSource.volume = Mathf.MoveTowards(seafloorSource.volume, seafloorVolume, step);
        }
        else if (seafloorSource.isPlaying)
        {
            float step = seafloorFadeOutTime > 0f ? Time.deltaTime / seafloorFadeOutTime : 1f;
            seafloorSource.volume = Mathf.MoveTowards(seafloorSource.volume, 0f, step);
            if (seafloorSource.volume <= 0f) seafloorSource.Stop();
        }
    }

 
    bool HasCharge() => chargeData == null || chargeData.current > 0;
 
    void RefreshChargeVisuals()
    {
        // Clickable when fully charged, or while in seafloor view so you can always go back
        if (switchButton != null) switchButton.interactable = topDown || HasCharge();
 
        // Arm light brightness matches the charge left (10/10 = full, 5/10 = half)
        if (armLight != null && chargeData != null && chargeData.max > 0)
            armLight.intensity = armLightBaseIntensity * chargeData.current / (float)chargeData.max;
    }

    void Apply()
    {
        robotCamera.SetActive(!topDown);
        topDownCamera.SetActive(topDown);
         if (metalDetectorUI != null) metalDetectorUI.SetActive(topDown);
        robotLook.enabled = !topDown;
        walker.enabled = topDown;

        // coordinates follow whichever view is active
        if (coordinates != null) coordinates.target = topDown ? walker.transform : robot;

        // Button shows a preview of the view you'll switch TO
        if (buttonImage != null) buttonImage.sprite = topDown ? baseViewSprite : armViewSprite;

        // Label shows the view you're currently IN
        if (cameraIdText != null) cameraIdText.text = topDown ? armViewLabel : baseViewLabel;
    }
}
