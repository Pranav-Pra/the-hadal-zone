using UnityEngine;
using System.Collections;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Resources;

public class GPSPuzzle : MonoBehaviour
{
[System.Serializable]
    public struct Coupling
    {
        public int source;
        public int target;
        public float ratio;
    }

    private bool puzzleActive;
    private float lockedTimer;
    private int lockedCount;
    private float[] couplingOffsets;

    [Header("Satelite References")]
    [SerializeField] Satelite[] satelites;
    [SerializeField] RectTransform robot;

    [Header("Hidden Rules")]
    [SerializeField] Coupling[] couplings;
    [Tooltip("All satelites must stay locked this long to complete the puzzle.")]
    [SerializeField] float holdSeconds = 0.4f;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI searchText;
    [SerializeField] bool showLockCount = true;

    [Header("Game Manager References")]
    [SerializeField] PuzzleCompletion puzzleComplete;

    void Start()
    {
        puzzleActive = true;
        couplingOffsets = new float[satelites.Length];
        StartCoroutine(SearchText());
    }

    void Update()
    {
        if (!puzzleActive) return;

        ApplyCouplings();

        lockedCount = 0;
        foreach (Satelite s in satelites)
        {
            s.Refresh(robot.position);
            if (s.IsLocked) lockedCount++;
        }

        if (satelites.Length > 0 && lockedCount == satelites.Length)
        {
            lockedTimer += Time.deltaTime;
            if (lockedTimer >= holdSeconds) CompletePuzzle();
        }
        else
        {
            lockedTimer = 0f;
        }
    }

    void ApplyCouplings()
    {
        for (int i = 0; i < couplingOffsets.Length; i++) couplingOffsets[i] = 0f;

        foreach (Coupling c in couplings)
        {
            bool valid = c.source >= 0 && c.source < satelites.Length
                      && c.target >= 0 && c.target < satelites.Length;
            if (valid) couplingOffsets[c.target] += c.ratio * satelites[c.source].SliderDeltaDegrees;
        }

        for (int i = 0; i < satelites.Length; i++) 
        {
            satelites[i].SetCouplingOffset(couplingOffsets[i]);
        }
    }

    void MakePoint()
    {
        int maxRadius = 45;
        float randomTheta = Random.Range(0, 6.26f);
        Vector3 pos = robot.position;
        pos.x = maxRadius * Mathf.Cos(randomTheta);
        pos.y = maxRadius * Mathf.Sin(randomTheta);
        robot.position = pos;

    }

    private IEnumerator SearchText()
    {
        int dots = 1;
        while (puzzleActive)
        {
            string text = "Searching" + new string('.', dots);
            if (showLockCount) text += "  " + lockedCount + "/" + satelites.Length;
            searchText.text = text;

            dots = dots == 3 ? 1 : dots + 1;
            yield return new WaitForSeconds(0.9f);
        }
    }

    void CompletePuzzle()
    {
        puzzleActive = false;
        foreach (Satelite s in satelites) s.SetInteractable(false);
        searchText.text = "Contact established";

        puzzleComplete.gps = true;
        MechanicUnlock unlock = FindFirstObjectByType<MechanicUnlock>();
        if (unlock != null) unlock.CheckUnlocks();

        FindFirstObjectByType<PuzzleObject>().PuzzleFinished();
    }
}
