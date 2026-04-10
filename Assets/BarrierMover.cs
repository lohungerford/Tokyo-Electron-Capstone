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

    [Header("Player blocking")]
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private float playerRadius = 0.3f;
    [SerializeField] private float minPlayerHeight = 1.1f;
    [SerializeField] private float maxPlayerHeight = 2.2f;
    [SerializeField] private float overlapSkin = 0.02f;

    private BarrierInteractable held;
    private Collider heldCollider;
    private float heldYaw;
    private CharacterController playerBody;
    private Transform playerHead;
    private Vector3 lastValidRigPosition;

    private void Start()
    {
        playerBody = GetComponent<CharacterController>();
        playerHead = Camera.main != null ? Camera.main.transform : null;
        lastValidRigPosition = transform.position;
    }

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

    private void LateUpdate()
    {
        SyncPlayerBody();
        ResolvePlayerBarrierCollision();
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

        held.SetHeldState(true);

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

        held.SetHeldState(false);

        held = null;
        heldCollider = null;
    }

    private void SyncPlayerBody()
    {
        if (playerBody == null)
        {
            return;
        }

        if (playerHead == null && Camera.main != null)
        {
            playerHead = Camera.main.transform;
        }

        if (playerHead == null)
        {
            return;
        }

        float resolvedMinHeight = minPlayerHeight > 0f ? minPlayerHeight : 1.1f;
        float resolvedMaxHeight = maxPlayerHeight > resolvedMinHeight ? maxPlayerHeight : 2.2f;
        float resolvedRadius = playerRadius > 0f ? playerRadius : 0.3f;

        Vector3 localHead = transform.InverseTransformPoint(playerHead.position);
        float height = Mathf.Clamp(localHead.y, resolvedMinHeight, resolvedMaxHeight);
        float radius = Mathf.Min(resolvedRadius, height * 0.5f - 0.01f);

        playerBody.height = height;
        playerBody.radius = radius;
        playerBody.center = new Vector3(localHead.x, height * 0.5f, localHead.z);
    }

    private void ResolvePlayerBarrierCollision()
    {
        LayerMask resolvedMask = blockingMask.value == 0
            ? LayerMask.GetMask("Barrier")
            : blockingMask;

        if (resolvedMask.value == 0 || playerBody == null)
        {
            return;
        }

        if (playerHead == null && Camera.main != null)
        {
            playerHead = Camera.main.transform;
        }

        if (playerHead == null)
        {
            return;
        }

        Vector3 localCenter = playerBody.center;
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        float resolvedOverlapSkin = overlapSkin >= 0f ? overlapSkin : 0.02f;
        float radius = Mathf.Max(0.01f, playerBody.radius - resolvedOverlapSkin);
        float halfSegment = Mathf.Max(0f, playerBody.height * 0.5f - playerBody.radius);
        Vector3 top = worldCenter + Vector3.up * halfSegment;
        Vector3 bottom = worldCenter - Vector3.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            top,
            bottom,
            radius,
            resolvedMask,
            QueryTriggerInteraction.Ignore
        );

        if (overlaps.Length > 0)
        {
            transform.position = lastValidRigPosition;
            SyncPlayerBody();
            return;
        }

        lastValidRigPosition = transform.position;
    }
}
