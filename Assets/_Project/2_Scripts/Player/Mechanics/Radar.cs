using UnityEngine;
using System.Collections.Generic;

public class Radar : MonoBehaviour
{
    [SerializeField] GameObject detector;
    [SerializeField] int detectorRotationSpeed;
    
    [SerializeField] Transform player;
    [SerializeField] RectTransform radarCenter;
    [SerializeField] GameObject enemyDetectionPrefab;
    [SerializeField] float detectionRadius = 20f;
    [SerializeField] float radarRadius = 100f;

    [SerializeField] AudioClip pingSound;
    [SerializeField] AudioSource source;

    private List<GameObject> enemiesList;

    void Awake()
    {
        enemiesList = new List<GameObject>();
    }

    private void Update()
    {
        detector.transform.Rotate(0f, 0f, -detectorRotationSpeed * Time.deltaTime);
        CheckEnemy();
    }

    private void CheckEnemy()
    {
        foreach (GameObject enemy in enemiesList)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            } 
        }
        enemiesList.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Vector3 offset = enemy.transform.position - player.position;
            Vector2 flat = new Vector2(offset.x, offset.z);
            float distance = flat.magnitude;
            if (detectionRadius > 0f && distance <= detectionRadius)
            {
                Vector2 pos = flat / detectionRadius * radarRadius;
                GameObject enemyPrefab = Instantiate(enemyDetectionPrefab, radarCenter);
                //source.PlayOneShot(pingSound);
                
                enemyPrefab.GetComponent<RectTransform>().anchoredPosition = pos;
                enemiesList.Add(enemyPrefab);
            }
        }
    }
}
