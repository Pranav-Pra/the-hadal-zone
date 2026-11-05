using UnityEngine;

public class Creature : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float speed = 1.5f;                        // meters per second along the circle
    [SerializeField] private Vector2 flipInterval = new Vector2(6f, 15f); // random seconds between direction flips
    [SerializeField] private bool spriteFacesRight = true;              // which way the art faces in the PNG

    [Header("Lifetime")]
    [SerializeField] private Vector2 lifetime = new Vector2(20f, 60f);  // random seconds before it may despawn
    [SerializeField] private float viewMargin = 0.1f;                   // extra screen edge counted as "on camera"

    private Vector3 center;
    private float radius;
    private float angle;             // position on the circle, in radians
    private float heightAboveGround;
    private int direction;           // +1 or -1
    private float nextFlip;
    private float despawnTime;
    private SpriteRenderer sprite;

    // Called by the spawner right after the creature is created
    public void Init(Vector3 orbitCenter, float orbitRadius, float startAngle, float height)
    {
        center = orbitCenter;
        radius = orbitRadius;
        angle = startAngle;
        heightAboveGround = height;
        direction = Random.value < 0.5f ? 1 : -1;   // random starting direction
        nextFlip = Time.time + Random.Range(flipInterval.x, flipInterval.y);
        despawnTime = Time.time + Random.Range(lifetime.x, lifetime.y);
        sprite = GetComponentInChildren<SpriteRenderer>();
        Move(0f);
    }

    void Update()
    {
        if (Time.time >= nextFlip)
        {
            direction = -direction;
            nextFlip = Time.time + Random.Range(flipInterval.x, flipInterval.y);
        }

        Move(Time.deltaTime);

        // lifetime up and camera not looking
        if (Time.time >= despawnTime && !IsOnCamera())
            Destroy(gameObject);
    }

    void Move(float dt)
    {
        angle += direction * (speed / radius) * dt;   // same speed at any distance

        Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        pos.y = GroundHeight(pos) + heightAboveGround; // follows the terrain's hills
        transform.position = pos;

        UpdateFacing();
    }

    // flip sprite so it faces the way it's moving on screen
    void UpdateFacing()
    {
        Camera cam = Camera.main;
        if (sprite == null || cam == null) return;

        Vector3 moveDir = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * direction;
        bool movingRight = Vector3.Dot(moveDir, cam.transform.right) > 0f;
        // sprite.flipX = movingRight != spriteFacesRight;

        // had to change becuase new material for creatures to make 2d sprites lit by 3d lighting
        Vector3 s = sprite.transform.localScale;
        s.x = Mathf.Abs(s.x) * (movingRight == spriteFacesRight ? 1f : -1f);
        sprite.transform.localScale = s;
    }

    bool IsOnCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        return v.z > 0f &&
            v.x > -viewMargin && v.x < 1f + viewMargin &&
            v.y > -viewMargin && v.y < 1f + viewMargin;
    }

    public static float GroundHeight(Vector3 pos)
    {
        Terrain t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(pos) + t.transform.position.y : 0f;
    }
}
