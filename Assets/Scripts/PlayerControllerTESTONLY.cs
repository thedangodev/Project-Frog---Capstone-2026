using UnityEngine;
using UnityEngine.InputSystem;

// Just a basic Player Controller for testing, not meant for use with later iterations of Project Frog. -E

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController_NewInput : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;

    [Header("Facing")]
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private Vector2 moveInput;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (!cameraTransform && Camera.main)
            cameraTransform = Camera.main.transform;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (cameraTransform)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            move = (camRight * move.x + camForward * move.z);
            if (move.sqrMagnitude > 1f) move.Normalize();
        }

        Vector3 horizontal = move * moveSpeed;

        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;

        verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move((horizontal + verticalVelocity) * Time.deltaTime);

        if (faceMoveDirection && move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }
    }
}