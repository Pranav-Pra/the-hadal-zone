using UnityEngine;
 
// keeps a sprite facing the camera
// only rotates around the vertical (Y) axis, so creatures don't tilt
public class Billboard : MonoBehaviour
{
    private Transform cam;
 
    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }
 
    // LateUpdate runs after the camera has turned this frame, so there's no lag
    void LateUpdate()
    {
        if (cam == null) return;
 
        Vector3 direction = transform.position - cam.position;
        direction.y = 0f;   // ignore height so the sprite stays upright
 
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}