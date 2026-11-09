using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private bool topDown;
    private float drainTimer;
    private float armLightBaseIntensity;
    private AudioSource audioSource;
    private AudioSource seafloorSource;

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

        if (!topDown || chargeData == null) return;
 
        // Drain charge while in seafloor view
        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer -= drainInterval;
            chargeData.Add(-drainAmount); 
        }
    }

    public void ToggleView()
    {
        if (!topDown && !HasCharge()) return;
        if (switchClip != null) audioSource.PlayOneShot(switchClip, switchVolume);
        SetView(!topDown);
    }

    void SetView(bool toTopDown)
    {
        topDown = toTopDown;
        drainTimer = 0f;   // full interval before the first segment drains
        Apply();
        RefreshChargeVisuals();
    }
 
    void HandleChargeChanged(int current)
    {
        // Out of charge then kick back to base view
        if (topDown && current <= 0)
        {
            SetView(false);
            return;
        }
        RefreshChargeVisuals();
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