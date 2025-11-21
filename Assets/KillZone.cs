using UnityEngine;

public class KillZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        //should only respond to the player object
        if (!other.CompareTag("PlayeR")) return;

        //if the player has a character controller, disable that before moving
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }
        if (respawnPoint != null)
        {
            other.transform.position = respawnPoint.position;
            other.transform.rotation = respawnPoint.rotation;
        }
        
        if (cc != null)
        {
            cc.enabled = true;
        }
    }
}
