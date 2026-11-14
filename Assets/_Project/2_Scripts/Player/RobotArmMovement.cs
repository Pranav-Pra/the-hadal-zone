using UnityEngine;
using UnityEngine.InputSystem;

// Arrow-key walking for the top-down view
[RequireComponent(typeof(CharacterController))]
public class RobotArmMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 90f;   // degrees per second
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform turnPivot;

    private CharacterController controller;
    private float verticalVelocity;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Turning (left/right arrows)
        float turn = 0f;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) turn -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) turn += 1f;

        Vector3 pivotShift = Vector3.zero;
        float angle = turn * turnSpeed * Time.deltaTime;
        if (angle != 0f)
        {
            Quaternion spin = Quaternion.Euler(0f, angle, 0f);
            if (turnPivot != null)
            {
                // where this object ends up if the whole robot swings around the pivot
                Vector3 p = turnPivot.position;
                Vector3 newPos = p + spin * (transform.position - p);
                pivotShift = newPos - transform.position;
            }
            transform.rotation = spin * transform.rotation;
        }

        // Driving (up/down arrows or W/S) along the facing direction
        float drive = 0f;
        if (kb.upArrowKey.isPressed   || kb.wKey.isPressed) drive += 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) drive -= 1f;
        Vector3 direction = transform.forward * drive;

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = (direction * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime + pivotShift;
        controller.Move(move);
    }
}