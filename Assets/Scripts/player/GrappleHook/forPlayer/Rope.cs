using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrappleRopeRenderer : MonoBehaviour
{
    [SerializeField] private PlayerGrapple playerGrapple;
    [SerializeField] private Transform firePoint;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        HandleRope();
    }

    private void HandleRope()
    {
        if (playerGrapple == null || firePoint == null)
            return;

        if (playerGrapple.IsGrappling && playerGrapple.CurrentTower != null)
        {
            DrawRope(playerGrapple.CurrentTower.transform.position);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void DrawRope(Vector3 targetPosition)
    {
        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, targetPosition);
    }
}