using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleObject : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The general puzzle UI panel.")]
    [SerializeField] GameObject mainUI;
    [Tooltip("Puzzle opened at each stage. Element 0 = Sonar, 1 = GPS, 2 = third puzzle.")]
    [SerializeField] GameObject[] stagePuzzleUIs;

    [SerializeField] Transform bodyLocation;

    [Header("Stages")]
    [SerializeField] SandSiteSpawner spawner;
    [Tooltip("The SonarObject's Sprite Renderer. Its sprite changes after each finished puzzle.")]
    [SerializeField] SpriteRenderer objectRenderer;
    [Tooltip("Element 0 = at the start, 1 = after the 1st puzzle, 2 = after the 2nd puzzle.")]
    [SerializeField] Sprite[] stageSprites = new Sprite[3];
    [Tooltip("How many puzzles before the object is removed for good.")]
    [SerializeField] int totalPuzzles = 3;

    int puzzlesFinished;
    public int PuzzlesFinished => puzzlesFinished;

    void Start() => ShowStageSprite();

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Camera currentCamera = Camera.current != null ? Camera.current : Camera.main;

            if (currentCamera.name == "TopDownCam")
            {
                Ray ray = currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                // Ignore triggers, so the detector box on RobotArm can't block the click
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponent<SpriteRenderer>() != null && hit.transform.CompareTag("PuzzleObject"))
                    {
                        mainUI.SetActive(true);
                        int i = Mathf.Min(puzzlesFinished, stagePuzzleUIs.Length - 1);
                        stagePuzzleUIs[i].SetActive(true);
                    }
                }
            }
        }
    }

    // Call this when any puzzle is completed
    public void PuzzleFinished()
    {
        puzzlesFinished++;
        ShowStageSprite();

        if (puzzlesFinished < totalPuzzles) spawner.RespawnPuzzleObject();
        else spawner.RemovePuzzleObject();
    }

    // Puts the sprite for the current stage on the object (stays on the last one once they run out)
    void ShowStageSprite()
    {
        if (objectRenderer == null || stageSprites == null || stageSprites.Length == 0) return;
        int i = Mathf.Min(puzzlesFinished, stageSprites.Length - 1);
        if (stageSprites[i] != null) objectRenderer.sprite = stageSprites[i];
    }
}