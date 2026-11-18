using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Satelite : MonoBehaviour
{
    public enum SignalState { None, Faint, Medium, Strong, Locked }

    [Header("Slider References")]
    [SerializeField] Slider rotationSlider;   // not used yet (kept from the original)
    [SerializeField] Slider positionSlider;   // 0..1 = one full lap of the ring

    [Header("Satelite Starting Values")]
    [SerializeField] float radius;
    [SerializeField] Vector3 startingLocation; // ring centre, in the parent's local space
    [SerializeField] float rotationOffset;     // sprite art correction only

    [Header("Beam")]
    [Tooltip("Fixed tilt of the beam away from the line to the ring centre (degrees, counter-clockwise).")]
    [SerializeField] float beamOffset = 0f;
    [Tooltip("Full cone width in degrees. Locks when aim error <= width / 2.")]
    [SerializeField] float beamWidth = 6f;
    [Tooltip("Optional. A UI Image used to draw the beam. Its pivot is set to the left-centre.")]
    [SerializeField] Image beamImage;
    [SerializeField] float beamLength = 160f;
    [SerializeField] float beamThickness = 4f;

    [Header("Signal Thresholds (aim error in degrees)")]
    [SerializeField] float strongError = 8f;
    [SerializeField] float mediumError = 16f;
    [SerializeField] float faintError = 35f;

    [Header("Beam Colours")]
    [SerializeField] Color offTargetColor = new Color(0.94f, 0.62f, 0.15f);
    [SerializeField] Color lockedColor = new Color(0.36f, 0.79f, 0.65f);

    [Header("Allignment Text")]
    [SerializeField] TextMeshProUGUI alignmentText;

    [Header("Gear Linking")]
    [Tooltip("Satellites that turn when this one is moved (like meshed gears).")]
    [SerializeField] Satelite[] gearedSatellites;
    [Tooltip("Degrees the linked satellites move per degree this one moves. Negative = opposite direction.")]
    [SerializeField] float gearRatio = -1f;

    private float gearOffset;       // degrees added by gear-linked satellites
    private float startSliderValue;
    private float couplingOffset;   // degrees added by other rings (set by GPSPuzzle)
    private float beamAngle;

    public SignalState State { get; private set; } = SignalState.None;
    public float AimError { get; private set; } = 180f;
    public bool IsLocked => State == SignalState.Locked;

    /// <summary>How far the player has turned this ring from its starting position, in degrees.</summary>
    public float SliderDeltaDegrees => (positionSlider.value - startSliderValue) * 360f;

    void Awake()
    {
        positionSlider.value = Random.value;
        startSliderValue = positionSlider.value;
        positionSlider.onValueChanged.AddListener(OnPositionChanged);

        if (beamImage != null)
        {
            RectTransform rt = beamImage.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
        }
    }

    void OnPositionChanged(float _)
    {
        if (gearedSatellites == null) return;
        foreach (Satelite s in gearedSatellites)
        {
            if (s != null && s != this) s.gearOffset = SliderDeltaDegrees * gearRatio;
        }
    }

    public void SetCouplingOffset(float degrees) => couplingOffset = degrees;

    public void SetInteractable(bool value)
    {
        positionSlider.interactable = value;
        if (rotationSlider != null) rotationSlider.interactable = value;
    }

    public void Refresh(Vector3 robotWorldPosition)
    {
        float angleDeg = positionSlider.value * 360f + couplingOffset + gearOffset;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 localPos = startingLocation + new Vector3(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad), 0f);
        transform.localPosition = localPos;

        beamAngle = angleDeg + 180f + beamOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, beamAngle + rotationOffset);

        Vector3 robotLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(robotWorldPosition)
            : robotWorldPosition;
        Vector2 toRobot = robotLocal - localPos;
        float bearing = Mathf.Atan2(toRobot.y, toRobot.x) * Mathf.Rad2Deg;
        AimError = Mathf.Abs(Mathf.DeltaAngle(beamAngle, bearing));

        if (AimError <= beamWidth * 0.5f)
        {
            State = SignalState.Locked;
        } 
        else if (AimError <= strongError)
        { 
            State = SignalState.Strong;
        }
        else if (AimError <= mediumError) 
        {
            State = SignalState.Medium;
        }
        else if (AimError <= faintError) 
        {
            State = SignalState.Faint;
        }
        else 
        {
            State = SignalState.None;
        }

        if (alignmentText != null)
            alignmentText.text = IsLocked ? "Aligned" : "Not Aligned...";

        UpdateBeam();
    }

    void UpdateBeam()
    {
        if (beamImage == null) return;

        RectTransform rt = beamImage.rectTransform;
        rt.position = transform.position;                      // world space, so it works as child or sibling
        rt.rotation = Quaternion.Euler(0f, 0f, beamAngle);
        rt.sizeDelta = new Vector2(beamLength, beamThickness);

        Color c = State == SignalState.Locked ? lockedColor : offTargetColor;
        switch (State)
        {
            case SignalState.Locked: c.a = 1f; break;
            case SignalState.Strong: c.a = 0.85f; break;
            case SignalState.Medium: c.a = 0.55f; break;
            case SignalState.Faint: c.a = 0.3f; break;
            default: c.a = 0.12f; break;
        }
        beamImage.color = c;
    }
}
