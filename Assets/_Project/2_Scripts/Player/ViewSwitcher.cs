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

    [Header("UI")]
    [SerializeField] private Image buttonImage;      // the switch view button's Image
    [SerializeField] private Sprite baseViewSprite;  // thumbnail of the base camera view
    [SerializeField] private Sprite armViewSprite;   // thumbnail of the arm camera view
    [SerializeField] private TMP_Text cameraIdText;  // the camera ID label
    [SerializeField] private string baseViewLabel = "BASE VIEW";
    [SerializeField] private string armViewLabel = "ARM VIEW";

    private bool topDown;

    void Start() => Apply();

    public void ToggleView()
    {
        topDown = !topDown;
        Apply();
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