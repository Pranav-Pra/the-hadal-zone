using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Blackbox : MonoBehaviour
{
    [SerializeField] RectTransform player;
    [SerializeField] List<GameObject> obstaclePrefabs;
    [SerializeField] ViewSwitcher viewSwitcher;

    private List<GameObject> obstacles;
    [SerializeField] int maxObstaclesPerLine;

    [SerializeField] List<String> keys;
    [SerializeField] TextScrambler textScrambler;
    
    [SerializeField] float playerSpeed;
    [SerializeField] float obstacleSpeed;

    private Vector3 startPosition;
    private string key;
    private bool isDashing;

    

    void Start()
    {
        obstacles = new List<GameObject>();
        startPosition = player.anchoredPosition;
        // Make the Key that is being solved for
        int keyInt = UnityEngine.Random.Range(0, keys.Count);
        key = keys[keyInt];
        textScrambler.level = 1;
        textScrambler.Scramble(key);
        for(int i = 1; i < 6; i++)
        {
            MakeObstacles(-154 + (75 * i));
        }
    }
    
    void Update()
    {
        if (viewSwitcher != null && viewSwitcher.GetCharge() <= 0)
        {
            OnChargeDepleted();
            return;
        }

        foreach (GameObject obstacle in obstacles)
        {
            if(IsOverlapping(player.GetComponent<RectTransform>(), obstacle.GetComponent<RectTransform>()))
            {
                ResetGame();
                if (viewSwitcher.GetCharge() <= 0)
                {
                    OnChargeDepleted();
                }
                return;
            }
        }
        MovePlayer();
        MoveObstacles();
    }

    void OnChargeDepleted()
    {
        StopAllCoroutines();
        isDashing = false;
        player.anchoredPosition = (Vector2)startPosition;
        textScrambler.SetLevel(1);

        foreach (GameObject obstacle in obstacles)
        {
            Destroy(obstacle);
        }
        obstacles.Clear();

        key = keys[UnityEngine.Random.Range(0, keys.Count)];
        textScrambler.Scramble(key);
        for (int i = 1; i < 6; i++)
        {
            MakeObstacles(-154 + (75 * i));
        }

        transform.parent.gameObject.SetActive(false);
    }

    void MoveObstacles()
    {
        float lineHeight = ((RectTransform)player.parent).rect.height;
        float bottomLimit = -lineHeight / 2f - obstaclePrefabs[0].GetComponent<RectTransform>().rect.height;
        List<float> respawnColumns = new List<float>();

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = obstacles[i];
            RectTransform rect = obstacle.GetComponent<RectTransform>();
            rect.anchoredPosition += Vector2.down * obstacleSpeed * Time.deltaTime;

            if (rect.anchoredPosition.y < bottomLimit)
            {
                respawnColumns.Add(rect.anchoredPosition.x);
                obstacles.RemoveAt(i);
                Destroy(obstacle);
            }
        }

        foreach (float x in respawnColumns)
        {
            SpawnObstacleAbove(x);
        }
    }

    void SpawnObstacleAbove(float x)
    {
        float lineHeight = ((RectTransform)player.parent).rect.height;
        float slotHeight = obstaclePrefabs[0].GetComponent<RectTransform>().rect.height;
        GameObject obstaclePrefab = obstaclePrefabs[UnityEngine.Random.Range(0, obstaclePrefabs.Count)];
        GameObject obstacle = Instantiate(obstaclePrefab, player.parent);
        float y = lineHeight / 2f + slotHeight * (UnityEngine.Random.Range(0, 3) + 0.5f);
        obstacle.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        obstacles.Add(obstacle);
    }

    void MakeObstacles(int xComponent)
    {
        float lineHeight = ((RectTransform)player.parent).rect.height;
        float slotHeight = obstaclePrefabs[0].GetComponent<RectTransform>().rect.height;
        int slotCount = Mathf.Max(2, Mathf.FloorToInt(lineHeight / slotHeight));

        List<int> slots = new List<int>();
        for (int s = 0; s < slotCount; s++) slots.Add(s);

        // Always leave at least one slot free so the player can fit on the line
        int count = Mathf.Min(UnityEngine.Random.Range(3, maxObstaclesPerLine + 1), slotCount - 1);
        for(int i = 0; i < count; i++)
        {
            int pick = UnityEngine.Random.Range(0, slots.Count);
            int slot = slots[pick];
            slots.RemoveAt(pick);

            GameObject obstaclePrefab = obstaclePrefabs[UnityEngine.Random.Range(0, obstaclePrefabs.Count)];
            GameObject obstacle = Instantiate(obstaclePrefab, player.parent);
            float y = -lineHeight / 2f + slotHeight * (slot + 0.5f);
            obstacle.GetComponent<RectTransform>().anchoredPosition = new Vector2(xComponent, y);
            obstacles.Add(obstacle);
        }
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        isDashing = false;
        viewSwitcher.RemoveCharge();
        player.anchoredPosition = (Vector2)startPosition;
        textScrambler.SetLevel(1);
    }

    void MovePlayer()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard.qKey.isPressed)
        {
            Vector3 pos = player.position;
            pos.y += playerSpeed * Time.deltaTime;
            player.position = pos;
        }
        if (keyboard.eKey.isPressed)
        {
            Vector3 pos = player.position;
            pos.y -= playerSpeed * Time.deltaTime;
            player.position = pos;
        }
        WrapPlayerVertically();
        if (keyboard.spaceKey.wasPressedThisFrame && !isDashing)
        {
            Vector2 targetPosition = player.anchoredPosition;
            targetPosition.x += 75;
            StartCoroutine(Dash(player.anchoredPosition, targetPosition, 1f));
        }
    }

    void WrapPlayerVertically()
    {
        float halfLine = ((RectTransform)player.parent).rect.height / 2f;
        float halfPlayer = player.rect.height / 2f;
        Vector2 pos = player.anchoredPosition;
        if (pos.y < -halfLine - halfPlayer)
        {
            pos.y = halfLine + halfPlayer;
            player.anchoredPosition = pos;
        }
        else if (pos.y > halfLine + halfPlayer)
        {
            pos.y = -halfLine - halfPlayer;
            player.anchoredPosition = pos;
        }
    }

    private bool IsOverlapping(RectTransform rectA, RectTransform rectB)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];

        rectA.GetWorldCorners(cornersA);
        rectB.GetWorldCorners(cornersB);

        Rect r1 = new Rect(cornersA[0].x, cornersA[0].y, cornersA[2].x - cornersA[0].x, cornersA[2].y - cornersA[0].y);
        Rect r2 = new Rect(cornersB[0].x, cornersB[0].y, cornersB[2].x - cornersB[0].x, cornersB[2].y - cornersB[0].y);

        return r1.Overlaps(r2);
    }

    private IEnumerator Dash(Vector2 startPosition, Vector2 targetPosition, float time)
    {
        isDashing = true;
        float elapsed = 0f;
        while(elapsed < time)
        {

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            player.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        player.anchoredPosition = targetPosition;
        textScrambler.SetLevel(textScrambler.level + 1);
        if (textScrambler.level == 5)
        {
            SolveBlackBox();
        }
        isDashing = false;
    }

    void SolveBlackBox()
    {
        Debug.Log("You Win!");
    }
}
