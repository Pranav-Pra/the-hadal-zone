using UnityEngine;

public class PuzzleCanvas : MonoBehaviour
{
    public void Exit()
    {
        Sonar sonar = FindFirstObjectByType<Sonar>();
        if (sonar != null) sonar.ResetPuzzle();

        gameObject.SetActive(false);
    }
}
