using UnityEngine;

public class StepTriggerBreak : MonoBehaviour
{
    public DisappearMechanic tile;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (tile != null)
                tile.BreakTile();
        }
    }
}