using UnityEngine;
using System.Collections;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Resources;

public class GPSPuzzle : MonoBehaviour
{

    private bool puzzleActive;

    [Header("Satelite References")]
    [SerializeField] GameObject sateliteRef1;
    [SerializeField] GameObject sateliteRef2;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI searchText;

    [Header("Game Manager References")]
    [SerializeField] PuzzleCompletion puzzleComplete;

    void Start()
    {
        puzzleActive = true;
        StartCoroutine(SearchText());
    }

    void MakePoints()
    {
        //int maxRadius = 45;
        
    }

    private IEnumerator SearchText()
    {
        int dots = 1;
        while (puzzleActive){
            if(dots == 1)
            {
                searchText.text = "Searching.";
                dots += 1;
            }
            else if(dots == 2)
            {
                searchText.text = "Searching..";
                dots+= 1;
            }
            else
            {
                searchText.text = "Searching...";
                dots = 1;
            }
            yield return new WaitForSeconds(0.9f);
        }
    }

    void CompletePuzzle()
    {
        puzzleComplete.gps = true;
        FindFirstObjectByType<MechanicUnlock>().CheckUnlocks();
    }
}
