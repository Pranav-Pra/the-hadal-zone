using System.Collections.Generic;
using UnityEngine;

// spawns any number of creature types around a center point
public class CreatureSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string name = "Eel";
        public Creature prefab;
        public int maxAlive = 3;                                   // most of this creature at once
        public Vector2 spawnInterval = new Vector2(5f, 15f);       // random seconds between spawns
        public Vector2 distance = new Vector2(5f, 60f);            // horizontal distance from robot
        public Vector2 heightAboveGround = new Vector2(0.5f, 6f);  // height above the sand

        public int startCount = 2;                              // how many exist when the game starts
        public Vector2 startDistance = new Vector2(15f, 35f);   // distance range for starting creatures


        [HideInInspector] public float nextSpawn;
        [HideInInspector] public List<Creature> alive = new List<Creature>();
    }

    [SerializeField] private Transform center;                 // the robot
    [SerializeField] private bool avoidSpawningInView = true;  // no popping in on screen
    [SerializeField] private List<SpawnEntry> creatures = new List<SpawnEntry>();

    void Start()
    {
        foreach (var e in creatures)
        {
            int count = Mathf.Min(e.startCount, e.maxAlive);
            for (int i = 0; i < count; i++)
                TrySpawn(e, e.startDistance, false);   // starting creatures may be on screen

            e.nextSpawn = Time.time + Random.Range(e.spawnInterval.x, e.spawnInterval.y);
        }
    }

    void Update()
    {
        foreach (var e in creatures)
        {
            e.alive.RemoveAll(c => c == null);   // forget creatures that despawned
            if (Time.time < e.nextSpawn || e.alive.Count >= e.maxAlive) continue;

            e.nextSpawn = Time.time + Random.Range(e.spawnInterval.x, e.spawnInterval.y);
            TrySpawn(e, e.distance, avoidSpawningInView);
        }
    }

    void TrySpawn(SpawnEntry e, Vector2 distanceRange, bool avoidView)
    {
        // Try a few random spots until one is valid
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // Square root spreads spawns evenly over the area instead of clustering near the robot
            float radius = Mathf.Sqrt(Random.Range(distanceRange.x * distanceRange.x, distanceRange.y * distanceRange.y));
            float height = Random.Range(e.heightAboveGround.x, e.heightAboveGround.y);

            Vector3 pos = center.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            pos.y = Creature.GroundHeight(pos) + height;

            if (!OnTerrain(pos)) continue;
            if (avoidView && InView(pos)) continue;

            Creature c = Instantiate(e.prefab, pos, Quaternion.identity);
            c.Init(center.position, radius, angle, height);
            e.alive.Add(c);
            return;
        }
    }

    bool OnTerrain(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return true;
        Vector3 o = t.transform.position, s = t.terrainData.size;
        return p.x >= o.x && p.x <= o.x + s.x && p.z >= o.z && p.z <= o.z + s.z;
    }

    bool InView(Vector3 p)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(p);
        return v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
    }
}