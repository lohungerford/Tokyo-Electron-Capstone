using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Randomly selects floor tiles to open (break) on a recurring schedule.
/// Each tile gets a warning phase (flashing) before it disappears.
/// Tiles repair themselves after a set duration.
///
/// Controlled by Level1Manager — call StartTiles() to begin and SetPaused() to pause.
/// </summary>
public class LevelTileManager : MonoBehaviour
{
    [Header("Tile References")]
    [Tooltip("Drag all floor tiles with DisappearMechanic here")]
    public DisappearMechanic[] allTiles;

    [Header("Timing")]
    [Tooltip("Seconds between each new tile being selected to break")]
    public float timeBetweenBreaks = 8f;
    [Tooltip("Seconds the tile flashes before it actually breaks")]
    public float warningDuration = 5f;
    [Tooltip("Seconds the tile stays broken before repairing itself")]
    public float brokenDuration = 12f;
    [Tooltip("Maximum number of tiles that can be broken or warning at the same time")]
    public int maxActiveTiles = 3;

    private bool isRunning = false;
    private bool isPaused = false;
    private float nextBreakTimer = 0f;

    // Track which tiles are currently in warning or broken state
    private List<TileState> activeTiles = new List<TileState>();

    private class TileState
    {
        public DisappearMechanic tile;
        public float timer;
        public Phase phase;
    }

    private enum Phase { Warning, Broken }

    void Update()
    {
        if (!isRunning || isPaused) return;

        float dt = Time.deltaTime;

        // --- Update active tiles ---
        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            TileState state = activeTiles[i];
            state.timer -= dt;

            if (state.timer <= 0f)
            {
                if (state.phase == Phase.Warning)
                {
                    // Warning finished — break the tile
                    state.tile.BreakTile();
                    state.phase = Phase.Broken;
                    state.timer = brokenDuration;
                }
                else if (state.phase == Phase.Broken)
                {
                    // Broken duration finished — repair the tile
                    state.tile.RepairTile();
                    activeTiles.RemoveAt(i);
                }
            }
        }

        // --- Pick new tiles to break ---
        nextBreakTimer -= dt;
        if (nextBreakTimer <= 0f)
        {
            nextBreakTimer = timeBetweenBreaks;

            if (activeTiles.Count < maxActiveTiles)
            {
                DisappearMechanic tile = PickRandomAvailableTile();
                if (tile != null)
                {
                    tile.StartWarning();
                    activeTiles.Add(new TileState
                    {
                        tile = tile,
                        timer = warningDuration,
                        phase = Phase.Warning
                    });
                }
            }
        }
    }

    /// <summary>Pick a random tile that is not currently active (warning or broken).</summary>
    private DisappearMechanic PickRandomAvailableTile()
    {
        // Build list of available tiles
        List<DisappearMechanic> available = new List<DisappearMechanic>();
        foreach (DisappearMechanic tile in allTiles)
        {
            if (tile == null) continue;
            if (tile.IsBroken() || tile.IsWarning()) continue;
            available.Add(tile);
        }

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    // =============================================
    // PUBLIC API (called by Level1Manager)
    // =============================================

    public void StartTiles()
    {
        isRunning = true;
        nextBreakTimer = timeBetweenBreaks;
    }

    public void StopTiles()
    {
        isRunning = false;

        // Repair all active tiles
        foreach (TileState state in activeTiles)
        {
            state.tile.StopWarning();
            state.tile.RepairTile();
        }
        activeTiles.Clear();
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        foreach (DisappearMechanic tile in allTiles)
        {
            if (tile != null) tile.SetPaused(paused);
        }
    }
}
