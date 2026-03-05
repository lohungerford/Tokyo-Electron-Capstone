using UnityEngine;

public class BarrierMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followOrigin;   // CenterEyeAnchor
    [SerializeField] private LayerMask barrierMask;    // Barrier layer only

    [Header("Grab settings")]
    [SerializeField] private float grabRange = 1.5f;
    [SerializeField] private float holdDistance = 1.0f;
    [SerializeField] private float holdHeightOffset = -0.3f; // tweak so it sits lower than eye level
    [SerializeField] private KeyCode testKey = KeyCode.X;    // TEMP for keyboard testing

    private BarrierInteractable held;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three)) // A on right controller
        {
            Debug.Log("X pressed");
            if (held == null) TryGrabNearest();
            else Drop();
        }

        if (held != null)
            Follow();
    }

    private void TryGrabNearest()
    {
        // Find nearest barrier via overlap sphere
        Collider[] hits = Physics.OverlapSphere(followOrigin.position, grabRange, barrierMask, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0) return;

        // pick closest
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
        if (held == null) return;

        // Make it "stick" by disabling physics while held
        held.rb.isKinematic = true;
    }

    private void Follow()
    {
        Vector3 targetPos = followOrigin.position + followOrigin.forward * holdDistance;
        targetPos.y += holdHeightOffset;

        // Keep upright, face same yaw as player
        Quaternion targetRot = Quaternion.Euler(0f, followOrigin.eulerAngles.y, 0f);

        held.transform.SetPositionAndRotation(targetPos, targetRot);
    }

    private void Drop()
    {
        if (held == null) return;

        held.rb.isKinematic = false;
        // keep gravity off for now; you can enable later if you want it to fall when dropped
        held = null;
    }

    // Optional: visualize grab radius in Scene view
    private void OnDrawGizmosSelected()
    {
        if (!followOrigin) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(followOrigin.position, grabRange);
    }
}