using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The general puzzle UI panel.")]
    [SerializeField] GameObject mainUI;
    [Tooltip("The GameObject in the canvas that shows the puzzle.")]
    [SerializeField] GameObject puzzleUI;
    
    void OnMouseDown()
    {
        mainUI.SetActive(true);
        puzzleUI.SetActive(true);
    }

    void Close()
    {
        puzzleUI.SetActive(false);
        mainUI.SetActive(false);
    }
}
