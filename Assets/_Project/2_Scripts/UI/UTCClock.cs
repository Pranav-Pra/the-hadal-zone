using System;
using TMPro;
using UnityEngine;

public class UtcClock : MonoBehaviour
{
    TMP_Text label;

    void Start() => label = GetComponent<TMP_Text>();

    void Update()
    {
        label.text = "<mspace=0.6em>" + DateTime.UtcNow.ToString("HH:mm:ss") + "</mspace> UTC";
    }
}