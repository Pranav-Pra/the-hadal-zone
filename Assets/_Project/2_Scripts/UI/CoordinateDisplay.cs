using System;
using TMPro;
using UnityEngine;

public class CoordinateDisplay : MonoBehaviour
{
    public Transform target;          // your player/robot
    public double baseLat = 11.3733;  // latitude at Unity origin
    public double baseLon = 142.5917; // longitude at Unity origin
    public float baseDepth = 10935f;  // depth (m) at Unity y = 0
    public double coordScale = 50.0;  // 1 Unity unit = 50 real meters
    TMP_Text label;

    void Start() => label = GetComponent<TMP_Text>();

    void Update()
    {
        Vector3 p = target.position;
        double lat = baseLat + p.z * coordScale / 111320.0;
        double lon = baseLon + p.x * coordScale / (111320.0 * Math.Cos(baseLat * Math.PI / 180));
        float depth = baseDepth - p.y * (float)coordScale;

        Transform cam = Camera.main.transform;
        float heading = cam.eulerAngles.y;
        float pitch = -Mathf.DeltaAngle(0f, cam.eulerAngles.x);

        label.text =
            $"<mspace=0.6em>{Math.Abs(lat):0.00000}</mspace>°{(lat >= 0 ? "N" : "S")}  " +
            $"<mspace=0.6em>{Math.Abs(lon):0.00000}</mspace>°{(lon >= 0 ? "E" : "W")}\n" +
            $"DEPTH  <mspace=0.6em>{depth:N1}</mspace> m" +
            $"\nHDG  <mspace=0.6em>{heading:000}</mspace>°   PITCH  <mspace=0.6em>{pitch:+00;-00}</mspace>°";
    }
}