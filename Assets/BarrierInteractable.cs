using UnityEngine;
using UnityEngine.AI;

public class BarrierInteractable : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public NavMeshObstacle obstacle;

    private Collider cachedCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
        if (cachedCollider == null)
        {
            cachedCollider = GetComponentInChildren<Collider>();
        }

        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        ConfigureObstacle();
    }

    public void SetHeldState(bool isHeld)
    {
        if (obstacle != null)
        {
            obstacle.enabled = !isHeld;
        }
    }

    private void ConfigureObstacle()
    {
        if (obstacle == null || cachedCollider == null)
        {
            return;
        }

        // Use the collider bounds so the carved obstacle matches the placed barrier footprint.
        Vector3 localCenter = transform.InverseTransformPoint(cachedCollider.bounds.center);
        Vector3 localSize = transform.InverseTransformVector(cachedCollider.bounds.size);
        localSize = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z)
        );

        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = localCenter;
        obstacle.size = localSize;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.enabled = cachedCollider.enabled;
    }
}
