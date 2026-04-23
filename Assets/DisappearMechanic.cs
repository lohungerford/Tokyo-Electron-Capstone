using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles a single floor tile that can warn, break (disappear), and repair (reappear).
/// Used by LevelTileManager to randomly open/close tiles.
/// </summary>
public class DisappearMechanic : MonoBehaviour
{
    [Header("Warning Settings")]
    [Tooltip("Color the tile flashes during the warning phase")]
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 1f); // red tint
    public float flashSpeed = 4f; // flashes per second

    private MeshRenderer meshRenderer;
    private Collider[] tileColliders;
    private NavMeshObstacle navMeshObstacle;
    private Color originalColor;
    private Material tileMaterial;

    private bool isWarning = false;
    private bool isBroken = false;
    private bool isPaused = false;
    private float flashTimer = 0f;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        tileColliders = GetComponents<Collider>();
        navMeshObstacle = GetComponent<NavMeshObstacle>();

        if (navMeshObstacle != null)
        {
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = false;

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                navMeshObstacle.shape = NavMeshObstacleShape.Box;
                navMeshObstacle.center = boxCollider.center;
                navMeshObstacle.size = boxCollider.size;
            }

            navMeshObstacle.enabled = false;
        }

        if (meshRenderer != null)
        {
            // Instance the material so each tile can flash independently
            tileMaterial = meshRenderer.material;
            originalColor = tileMaterial.color;
        }
    }

    void Update()
    {
        if (isPaused) return;

        // Flash between original and warning color during warning phase
        if (isWarning && tileMaterial != null)
        {
            flashTimer += Time.deltaTime * flashSpeed;
            float t = (Mathf.Sin(flashTimer * Mathf.PI * 2f) + 1f) / 2f;
            tileMaterial.color = Color.Lerp(originalColor, warningColor, t);
        }
    }

    /// <summary>Start the visual warning (flashing red). Tile is still solid.</summary>
    public void StartWarning()
    {
        if (isBroken) return;
        isWarning = true;
        flashTimer = 0f;
    }

    /// <summary>Stop the warning and restore the original color.</summary>
    public void StopWarning()
    {
        isWarning = false;
        flashTimer = 0f;
        if (tileMaterial != null)
            tileMaterial.color = originalColor;
    }

    /// <summary>Break the tile — disappears visually and physically.</summary>
    public void BreakTile()
    {
        StopWarning();
        isBroken = true;

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (tileColliders != null)
        {
            foreach (Collider tileCollider in tileColliders)
            {
                if (tileCollider != null)
                    tileCollider.enabled = false;
            }
        }

        if (navMeshObstacle != null)
            navMeshObstacle.enabled = true;
    }

    /// <summary>Repair the tile — reappears visually and physically.</summary>
    public void RepairTile()
    {
        isBroken = false;

        if (meshRenderer != null)
            meshRenderer.enabled = true;

        if (tileColliders != null)
        {
            foreach (Collider tileCollider in tileColliders)
            {
                if (tileCollider != null)
                    tileCollider.enabled = true;
            }
        }

        if (tileMaterial != null)
            tileMaterial.color = originalColor;

        if (navMeshObstacle != null)
            navMeshObstacle.enabled = false;
    }

    public bool IsBroken() => isBroken;
    public bool IsWarning() => isWarning;

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }
}
