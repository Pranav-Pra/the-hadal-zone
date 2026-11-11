using UnityEngine;
using UnityEngine.UI;

public class Satelite : MonoBehaviour
{
    [Header("Slider References")]
    [SerializeField] Slider rotationSlider;
    [SerializeField] Slider positionSlider;

    void Update()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        float currentAngle = Mathf.Deg2Rad * 360 * positionSlider.value;
        int radius = 75;
        transform.localPosition = new Vector3 (radius * Mathf.Cos(currentAngle), radius * Mathf.Sin(currentAngle));
    }
}
