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
        float distance = Vector3.Distance(transform.position, bodyLocation.position);

        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (distance < 5f && ePressed && !mainUI.activeSelf)
        {
            mainUI.SetActive(true);
        }
        else if (distance < 5f && ePressed && mainUI.activeSelf)
        {
            mainUI.SetActive(false);
        }

    }
    
}
