using UnityEngine;

public class BarrierMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followOrigin;
    [SerializeField] private GameObject barrierPrefab;

    [Header("Grab settings")]
    [SerializeField] private LayerMask barrierMask;
    [SerializeField] private float grabRange = 2f;

    [Header("Hold settings")]
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private float holdHeightOffset = -0.3f;

    [Header("Drop settings")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundSnapRayDistance = 10f;

    private BarrierInteractable held;
    private Collider heldCollider;
    private float heldYaw;

    private void Update()
    {
        // X button: grab nearest, spawn new, or drop current
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            if (held == null)
            {
                if (!TryGrabNearest())
                {
                    SpawnBarrier();
                }
            }
            else
            {
                Drop();
            }
        }

        // Y button: rotate held barrier by 90 degrees
        if (held != null && OVRInput.GetDown(OVRInput.Button.Four))
        {
            heldYaw += 90f;
        }

        if (held != null)
        {
            Follow();
        }
    }

    private bool TryGrabNearest()
    {
        Collider[] hits = Physics.OverlapSphere(
            followOrigin.position,
            grabRange,
            barrierMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            return false;
        }

        Collider best = hits[0];
        float bestDist = Vector3.Distance(followOrigin.position, best.transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float dist = Vector3.Distance(followOrigin.position, hits[i].transform.position);
            if (dist < bestDist)
            {
                best = hits[i];
                bestDist = dist;
            }
        }

        BarrierInteractable interactable = best.GetComponentInParent<BarrierInteractable>();
        if (interactable == null)
        {
            return false;
        }

        held = interactable;
        heldYaw = held.transform.eulerAngles.y;

        PrepareHeldBarrier();

        return true;
    }

    private void SpawnBarrier()
    {
        Vector3 spawnPos = followOrigin.position + followOrigin.forward * holdDistance;
        spawnPos.y += holdHeightOffset;

        Quaternion spawnRot = Quaternion.Euler(0f, followOrigin.eulerAngles.y, 0f);

        GameObject newBarrier = Instantiate(barrierPrefab, spawnPos, spawnRot);

        held = newBarrier.GetComponent<BarrierInteractable>();
        if (held == null)
        {
            Debug.LogError("Spawned barrier is missing BarrierInteractable.");
            return;
        }

        heldYaw = held.transform.eulerAngles.y;

        PrepareHeldBarrier();
    }

    private void PrepareHeldBarrier()
    {
        if (held == null)
        {
            return;
        }

        if (held.rb != null)
        {
            held.rb.isKinematic = true;
            held.rb.linearVelocity = Vector3.zero;
            held.rb.angularVelocity = Vector3.zero;
        }

        heldCollider = held.GetComponent<Collider>();
        if (heldCollider == null)
        {
            heldCollider = held.GetComponentInChildren<Collider>();
        }

        if (heldCollider != null)
        {
            heldCollider.enabled = false;
        }
    }

    private void Follow()
    {
        Vector3 targetPos = followOrigin.position + followOrigin.forward * holdDistance;
        targetPos.y += holdHeightOffset;

        Quaternion targetRot = Quaternion.Euler(0f, heldYaw, 0f);
        held.transform.SetPositionAndRotation(targetPos, targetRot);
    }

    private void Drop()
    {
        if (held == null)
        {
            return;
        }

        Vector3 dropPos = held.transform.position;
        Ray ray = new Ray(dropPos + Vector3.up * 2f, Vector3.down);

        float halfHeight = 0.5f;
        Collider barrierCollider = held.GetComponent<Collider>();
        if (barrierCollider == null)
        {
            barrierCollider = held.GetComponentInChildren<Collider>();
        }

        if (Physics.Raycast(ray, out RaycastHit hit, groundSnapRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (barrierCollider != null)
            {
                halfHeight = barrierCollider.bounds.extents.y;
            }

            dropPos.y = hit.point.y + halfHeight;
        }

        held.transform.SetPositionAndRotation(dropPos, Quaternion.Euler(0f, heldYaw, 0f));

        if (held.rb != null)
        {
            // Keep it kinematic so it stays fixed in place
            held.rb.isKinematic = true;
            held.rb.linearVelocity = Vector3.zero;
            held.rb.angularVelocity = Vector3.zero;
        }

        if (heldCollider != null)
        {
            heldCollider.enabled = true;
        }

        held = null;
        heldCollider = null;
    }
}