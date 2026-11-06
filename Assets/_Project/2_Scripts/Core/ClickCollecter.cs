using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Clicking a creature in the world collects its charge
public class ClickCollector : MonoBehaviour
{
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private Camera robotCamera;   // only collect from this view

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // ignore clicks on UI, like the switch view button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Ignore clicks while the robot view is off (e.g., in top-down view)
        if (robotCamera == null || !robotCamera.isActiveAndEnabled) return;
        Camera cam = robotCamera;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            ChargeSource source = hit.collider.GetComponentInParent<ChargeSource>();
            if (source != null) source.Collect();
        }
    }
}