using UnityEngine;

public class ViewSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject robotCamera;
    [SerializeField] private GameObject topDownCamera;
    [SerializeField] private RobotLook robotLook;
    [SerializeField] private RobotArmMovement walker;
    [SerializeField] private Transform robot;
    [SerializeField] private CoordinateDisplay coordinates;

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
    }
}