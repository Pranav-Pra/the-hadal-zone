using UnityEngine;
using UnityEngine.InputSystem;
 
// Anchored first-person look for the robot
// Mouse left/right spins the robot 360 degrees
// Mouse up/down tilts only the camera
public class RobotLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;   // the Main Camera (child of the robot)
 
    [Header("Look Settings")]
    [SerializeField] private float turnSpeed = 90f;   // degrees per second, left/right
    [SerializeField] private float tiltSpeed = 60f;   // degrees per second, up/down
    [SerializeField] private float minPitch = -60f;   // how far down you can look
    [SerializeField] private float maxPitch = 60f;    // how far up you can look
 
    private float pitch;
 
    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
 
        // Left/Right: -1, 0, or 1
        float turn = 0f;
        if (keyboard.leftArrowKey.isPressed  || keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) turn += 1f;

        // Up/Down: -1, 0, or 1
        float tilt = 0f;
        if (keyboard.upArrowKey.isPressed   || keyboard.wKey.isPressed) tilt += 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) tilt -= 1f;

 
        // Rotate the whole robot around its vertical axis (full 360)
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
 
        // Tilt only the camera, clamped (negative pitch looks up in Unity)
        pitch = Mathf.Clamp(pitch - tilt * tiltSpeed * Time.deltaTime, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
 