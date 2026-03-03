using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerAttacks : MonoBehaviour
{

    private PlayerTongueAttack playerTongueAttack;
    private PlayerMovement movement;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerTongueAttack = GetComponent<PlayerTongueAttack>();

        // Subscribe to tongue finished event to resume movement automatically
        playerTongueAttack.OnTongueFinished += ResumeMovement;
    }

    private void Update()
    {
        HandleTongueInput();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to prevent memory leaks
        playerTongueAttack.OnTongueFinished -= ResumeMovement;
    }

    private void ResumeMovement()
    {
        movement.ResumeMovement();
    }

    private void HandleTongueInput()
    {
        if (playerTongueAttack == null) return;

        if (Input.GetButtonDown("Fire2"))
        {
            Vector3 cursorDirection = GetCursorDirection();

            // Rotate player toward cursor cursorDirectionection
            transform.forward = cursorDirection;

            // Lock movement and rotation using the stored forward vector
            movement.StopMovement(forward: cursorDirection);

            // Start tongue attack
            playerTongueAttack.StartExtend();
        }

        if (Input.GetButtonUp("Fire2"))
        {
            playerTongueAttack.StartRetract();
        }
    }

    private Vector3 GetCursorDirection()
    {
        // Cast a ray from the camera through the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);

        // Find the point where the ray intersects the ground plane
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 cursorDirection = hitPoint - transform.position;
            cursorDirection.y = 0f; // Ignore vertical difference
            return cursorDirection.normalized;
        }
        return transform.forward; // if raycast fails, cursor direction is player direction so dart shoots in the direction player is facing
    }
}