using UnityEngine;
using System.Collections;

public class KillZone : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Transform respawnPoint;

    // Drag your rig's TrackingSpace here (from Hierarchy)
    [SerializeField] private Transform trackingSpace;

    [Tooltip("The top-level rig object to move (ex: [BuildingBlock] Camera Rig). If left empty, script will find a parent tagged Player.")]
    [SerializeField] private Transform rigRootOverride;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float cooldownSeconds = 0.25f;

    private bool isRespawning;

    private void OnTriggerEnter(Collider other)
    {
        if (isRespawning) return;
        if (respawnPoint == null) return;

        Transform rigRoot = rigRootOverride != null ? rigRootOverride : FindTaggedParent(other.transform, playerTag);
        if (rigRoot == null) return;

        StartCoroutine(Respawn(rigRoot));
    }

    private IEnumerator Respawn(Transform rigRoot)
    {
        isRespawning = true;

        // If there is a Rigidbody on the rig root, stop any momentum-like motion
        Rigidbody rb = rigRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Move the whole rig
        rigRoot.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        // Critical for Meta XR: remove headset/controller offset so we don't "snap back"
        if (trackingSpace != null)
        {
            trackingSpace.localPosition = Vector3.zero;
            trackingSpace.localRotation = Quaternion.identity;
        }

        // Let XR + physics settle
        yield return null;
        yield return new WaitForFixedUpdate();

        yield return new WaitForSeconds(cooldownSeconds);
        isRespawning = false;
    }

    private Transform FindTaggedParent(Transform start, string tag)
    {
        Transform t = start;
        while (t != null && !t.CompareTag(tag)) t = t.parent;
        return t;
    }
}