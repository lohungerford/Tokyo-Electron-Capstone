using UnityEngine;

public class BarrierMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followOrigin;
    [SerializeField] private LayerMask barrierMask;

    [Header("Grab settings")]
    [SerializeField] private float grabRange = 2f;
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private float holdHeightOffset = -0.3f;

    [Header("Drop settings")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundSnapRayDistance = 10f;

    private BarrierInteractable held;
    private float heldYaw;

    void Update()
    {
        // X on left controller: grab or drop
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            if (held == null)
                TryGrabNearest();
            else
                Drop();
        }

        // Y on left controller: rotate held barrier by 90 degrees
        if (held != null && OVRInput.GetDown(OVRInput.Button.Four))
        {
            heldYaw += 90f;
        }

        if (held != null)
            Follow();
    }

    private void TryGrabNearest()
    {
        Collider[] hits = Physics.OverlapSphere(
            followOrigin.position,
            grabRange,
            barrierMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            Debug.Log("No barrier found in grab range.");
            return;
        }

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
        {
            Debug.Log("Nearest object did not have BarrierInteractable.");
            return;
        }

        held.rb.isKinematic = true;
        heldYaw = held.transform.eulerAngles.y;

        Debug.Log("Picked up barrier: " + held.name);
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
        if (held == null) return;

        Vector3 dropPos = held.transform.position;

        Ray ray = new Ray(dropPos + Vector3.up * 2f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundSnapRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit ground: " + hit.collider.name);

            Collider barrierCollider = held.GetComponent<Collider>();
            float halfHeight = 0.5f;

            if (barrierCollider != null)
                halfHeight = barrierCollider.bounds.extents.y;

            dropPos.y = hit.point.y + halfHeight;
        }
        else
        {
            Debug.Log("Did not hit ground.");
        }

        held.transform.SetPositionAndRotation(dropPos, Quaternion.Euler(0f, heldYaw, 0f));
        held.rb.isKinematic = false;

        Debug.Log("Dropped barrier at: " + dropPos);

        held = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!followOrigin) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followOrigin.position, grabRange);
    }
}