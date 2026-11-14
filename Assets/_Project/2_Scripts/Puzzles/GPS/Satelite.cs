using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Satelite : MonoBehaviour
{
    [Header("Slider References")]
    [SerializeField] Slider rotationSlider;
    [SerializeField] Slider positionSlider;

    [Header("Satelite Starting Values")]
    [SerializeField] int radius;
    [SerializeField] Vector3 startingLocation;
    [SerializeField] float rotationOffset;

    void Update()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        float currentAngle = Mathf.Deg2Rad * 360 * positionSlider.value;
        transform.localPosition = startingLocation + new Vector3(radius * Mathf.Cos(currentAngle), radius * Mathf.Sin(currentAngle), 0f);

        if (transform.parent != null)
        {
            Vector3 direction = transform.localPosition;
            if (direction.sqrMagnitude > 0f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
            }
            
        }
    }
}
