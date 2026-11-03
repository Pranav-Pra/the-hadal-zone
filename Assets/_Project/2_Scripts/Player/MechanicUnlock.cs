using UnityEngine;

public class MechanicUnlock : MonoBehaviour
{
    [SerializeField] PuzzleCompletion puzzleCompletion;
    [SerializeField] GameObject sonarMechanic;

    public void CheckUnlocks()
    {
        if (puzzleCompletion.sonar && !sonarMechanic.activeSelf)
        {
            sonarMechanic.SetActive(true);
        }
    }
}
