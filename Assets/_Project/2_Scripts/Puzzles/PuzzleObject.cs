using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleObject : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The general puzzle UI panel.")]
    [SerializeField] GameObject mainUI;
    [Tooltip("The GameObject in the canvas that shows the puzzle.")]
    [SerializeField] GameObject puzzleUI;

    [SerializeField] Transform bodyLocation;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Camera currentCamera = Camera.current != null ? Camera.current : Camera.main;

            if (currentCamera.name == "TopDownCam")
            {
                Ray ray = currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.GetComponent<SpriteRenderer>() != null && hit.transform.CompareTag("PuzzleObject"))
                    {
                        mainUI.SetActive(true);
                        puzzleUI.SetActive(true);
                    }
                }
            }
            
        }
    }
}
