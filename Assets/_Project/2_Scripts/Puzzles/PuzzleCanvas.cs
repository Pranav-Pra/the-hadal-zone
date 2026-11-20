using UnityEngine;

public class PuzzleCanvas : MonoBehaviour
{
    public void Exit()
    {
        FindFirstObjectByType<Sonar>().ResetPuzzle();
        FindFirstObjectByType<GPSPuzzle>().ResetPuzzle();
        FindFirstObjectByType<Blackbox>().ResetGame();
        gameObject.SetActive(false);
    }
}
