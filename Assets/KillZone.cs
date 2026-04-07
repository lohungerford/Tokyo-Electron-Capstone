using UnityEngine;

public class KillZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        //to be at the top of the object hierarchy
        Transform root = other.transform.root;
        //should only respond to the player object
        if (!root.CompareTag("Player")) return;

        // Find the CharacterController anywhere under the player root
        // (it lives on PlayerController, a child of the Camera Rig)
        CharacterController cc = root.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (respawnPoint != null)
        {
            // Move the CharacterController's GameObject — FirstPersonLocomotor
            // uses this position to drive the Camera Rig
            if (cc != null)
            {
                cc.transform.position = respawnPoint.position;
                cc.transform.rotation = respawnPoint.rotation;
            }
            else
            {
                root.position = respawnPoint.position;
                root.rotation = respawnPoint.rotation;
            }
        }

        if (cc != null) cc.enabled = true;
    }
}
