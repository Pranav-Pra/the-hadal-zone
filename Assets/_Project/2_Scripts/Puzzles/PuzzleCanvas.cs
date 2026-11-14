using UnityEngine;

public class PuzzleCanvas : MonoBehaviour
{
    public void Exit()
    {
        FindFirstObjectByType<Sonar>().ResetPuzzle();
        gameObject.SetActive(false);
    }
}
