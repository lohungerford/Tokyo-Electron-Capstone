using UnityEngine;

public class DisappearMechanic : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Collider tileCollider;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        tileCollider = GetComponent<Collider>();
    }

    public void BreakTile()
    {
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (tileCollider != null)
            tileCollider.enabled = false;
    }
}