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
    private float heldYaw;

    void Update()
    {
        // X button
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            if (held == null)
            {
                if (!TryGrabNearest())
                    SpawnBarrier();
            }
            else
            {
                Drop();
            }
        }

        // Y button rotates
        if (held != null && OVRInput.GetDown(OVRInput.Button.Four))
        {
            heldYaw += 90f;
        }

        if (held != null)
            Follow();
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
            return false;

        Collider best = hits[0];
        float bestDist = Vector3.Distance(followOrigin.position, best.transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float d = Vector3.Distance(followOrigin.position, hits[i].transform.position);
            if (d < bestDist)
            {
                best = hits[i];
                bestDist = d;
            }
        }

        held = best.GetComponentInParent<BarrierInteractable>();

        if (held == null)
            return false;

        held.rb.isKinematic = true;
        heldYaw = held.transform.eulerAngles.y;

        return true;
    }

    private void SpawnBarrier()
    {
        Vector3 spawnPos = followOrigin.position + followOrigin.forward * holdDistance;
        spawnPos.y += holdHeightOffset;

        Quaternion spawnRot = Quaternion.Euler(0f, followOrigin.eulerAngles.y, 0f);

        GameObject newBarrier = Instantiate(barrierPrefab, spawnPos, spawnRot);

        held = newBarrier.GetComponent<BarrierInteractable>();
        held.rb.isKinematic = true;
        heldYaw = newBarrier.transform.eulerAngles.y;
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
        Vector3 dropPos = held.transform.position;

        Ray ray = new Ray(dropPos + Vector3.up * 2f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundSnapRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Collider barrierCollider = held.GetComponent<Collider>();
            float halfHeight = 0.5f;

            if (barrierCollider != null)
                halfHeight = barrierCollider.bounds.extents.y;

            dropPos.y = hit.point.y + halfHeight;
        }

        held.transform.SetPositionAndRotation(dropPos, Quaternion.Euler(0f, heldYaw, 0f));
        held.rb.isKinematic = false;

        held = null;
    }
}