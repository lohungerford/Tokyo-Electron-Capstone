using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Moves friendly robots between random floor tile positions.
///
/// WALK MODE  — Direct Rigidbody movement toward a random tile centre.
///              Barrier cooldown (checked BEFORE ground check) prevents the
///              escape velocity set in OnCollisionEnter from being zeroed by
///              a tile-edge false positive.
///
/// FALL MODE  — Two independent signals trigger a fall:
///              (a) Frame-buffered raycast: N consecutive no-ground frames.
///              (b) Tile check: the nearest tile (by XZ) is broken AND the
///                  raycast also says no-ground on this frame.
///              On entry the capsule and all child colliders are disabled so
///              edge-friction cannot hold the bot in mid-air.
///              gameObject.SetActive(false) once below fallDeathY so
///              Level1Manager.CountAliveRobots() registers it as a lost bot.
///
/// IMPORTANT  — FriendlyAIScript and NavMeshAgent are Destroy()ed in Start()
///              (not just disabled) to prevent Start()-order races, GameHUD
///              interactions, and any other re-enable path from hijacking
///              the robot's position.
///
/// Requires: Rigidbody (non-kinematic, Continuous), LevelTileManager in Inspector.
/// Is Kinematic must be UNCHECKED.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BotBounceMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("How close to a tile centre counts as arrived.")]
    [SerializeField] private float waypointReachThreshold = 0.6f;

    [Header("References")]
    [Tooltip("Drag the LevelTileManager from the scene here.")]
    [SerializeField] private LevelTileManager tileManager;

    [Header("Barrier Collision")]
    [Tooltip("After hitting a barrier the bot pushes away for this many seconds " +
             "before velocity override resumes toward the new target.")]
    [SerializeField] private float barrierCooldownDuration = 0.5f;

    [Header("Fall Detection")]
    [Tooltip("Length of the downward ground-check ray.")]
    [SerializeField] private float groundCheckDistance = 0.4f;
    [Tooltip("How many consecutive FixedUpdate frames must return no-ground before " +
             "fall mode triggers (fallback if the direct tile check doesn't fire). " +
             "Higher values avoid false positives at tile edges.")]
    [SerializeField] private int noGroundFramesRequired = 8;
    [Tooltip("Y at which a falling bot is deactivated and counted as lost.")]
    [SerializeField] private float fallDeathY = -2f;

    // ── Components ───────────────────────────────────────────────────────────
    private Rigidbody rb;
    private Animator  anim;

    // ── State ─────────────────────────────────────────────────────────────────
    private DisappearMechanic currentTarget;
    private bool  isRunning      = false;
    private bool  isPaused       = false;
    private bool  isFalling      = false;
    private float barrierCooldown = 0f;
    private int   noGroundFrames  = 0;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Start()
    {
        rb   = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        // ── Bug 1 fix ────────────────────────────────────────────────────────
        // DESTROY conflicting components — do NOT just disable them.
        //
        // Why Destroy instead of enabled = false:
        //  • Start() execution order is not guaranteed. If FriendlyAIScript.Start()
        //    runs first it calls InitPatrolMode() which re-enables the NavMeshAgent
        //    and sets rb.isKinematic = true — warping the robot onto the NavMesh.
        //  • GameHUD.Resume() uses FindObjectsByType<NavMeshAgent>() and pokes
        //    every agent it finds; a merely-disabled agent is still found.
        //  • Destroy() is deferred to end-of-frame, so we ALSO disable immediately
        //    to stop any same-frame side effects.
        // ──────────────────────────────────────────────────────────────────────
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;   // immediate — stops any same-frame movement
            Destroy(agent);          // permanent — removed at end of frame
        }

        FriendlyAIScript friendly = GetComponent<FriendlyAIScript>();
        if (friendly != null)
        {
            friendly.enabled = false;
            Destroy(friendly);
        }

        // Force Rigidbody settings AFTER disabling conflicting components.
        // FriendlyAIScript.InitPatrolMode() may have set isKinematic = true
        // if it ran before us in the same frame.
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        // Safety: ensure root motion is off — if ON the Animator overrides
        // transform.position every frame, which looks like teleporting.
        if (anim != null) anim.applyRootMotion = false;
    }

    private void FixedUpdate()
    {
        if (!isRunning || isPaused) return;

        // ── Fall death ────────────────────────────────────────────────────────
        if (transform.position.y < fallDeathY)
        {
            rb.linearVelocity = Vector3.zero;
            gameObject.SetActive(false);
            return;
        }

        // ── Already falling — let Rigidbody gravity do the work ──────────────
        if (isFalling) return;

        // ── Barrier cooldown (checked BEFORE ground check) ────────────────────
        // CRITICAL ORDER: OnCollisionEnter sets an escape velocity and starts
        // this cooldown. If we ran the ground check first, a tile-edge
        // false-positive would zero the escape velocity before this is reached,
        // and the bot would stay glued to the barrier.
        if (barrierCooldown > 0f)
        {
            barrierCooldown -= Time.fixedDeltaTime;
            if (anim != null) anim.SetFloat("Speed", rb.linearVelocity.magnitude);
            return;
        }

        // ── Ground check (frame-buffered + direct tile check) ─────────────────
        bool grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        if (!grounded)
        {
            // Bug 2 fix — deterministic tile check.
            // The raycast can oscillate between hitting and missing adjacent
            // tile edges, so noGroundFrames never reaches the threshold and
            // the robot hovers in mid-air.  Checking the nearest tile by XZ
            // gives a reliable second signal: if BOTH the raycast says
            // "no ground" AND the tile directly below is broken, fall now.
            DisappearMechanic tileBelow = GetTileUnderRobot();
            if (tileBelow != null && tileBelow.IsBroken())
            {
                EnterFallMode();
                return;
            }

            noGroundFrames++;
            if (noGroundFrames >= noGroundFramesRequired)
            {
                EnterFallMode();
                return;
            }
            // Buffer zone: just return and let physics settle naturally.
            // Do NOT zero velocity here — that causes a freeze loop where the
            // bot oscillates at the tile edge and never accumulates enough
            // consecutive no-ground frames to trigger fall mode.
            return;
        }
        noGroundFrames = 0;

        // ── Waypoint selection ────────────────────────────────────────────────
        if (currentTarget == null || currentTarget.IsBroken())
        {
            PickNewTarget();
            return;
        }

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < waypointReachThreshold)
        {
            PickNewTarget();
            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 velocity  = direction * moveSpeed;
        velocity.y        = rb.linearVelocity.y; // preserve gravity
        rb.linearVelocity = velocity;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
            rotationSpeed * Time.fixedDeltaTime);

        if (anim != null) anim.SetFloat("Speed", moveSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isRunning || isFalling) return;

        // Ignore other robots
        if (collision.gameObject.GetComponent<BotBounceMovement>() != null) return;

        Vector3 rawNormal = collision.contacts[0].normal;
        if (Mathf.Abs(rawNormal.y) > 0.7f) return; // floor/ceiling — ignore

        Vector3 normal = new Vector3(rawNormal.x, 0f, rawNormal.z).normalized;
        if (normal == Vector3.zero) return;

        // Push away from barrier. The cooldown above prevents FixedUpdate from
        // overriding this escape velocity before the bot clears the obstacle.
        rb.linearVelocity = new Vector3(
            normal.x * moveSpeed,
            rb.linearVelocity.y,
            normal.z * moveSpeed);
        barrierCooldown = barrierCooldownDuration;
        PickNewTarget();
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private void EnterFallMode()
    {
        isFalling = true;

        // Bug 2 fix — disable ALL colliders (capsule + child limb colliders)
        // so edge-friction on adjacent solid tiles can't hold the bot in the air.
        // Robots self-deactivate via fallDeathY, so they don't need collision
        // during the fall.  KillZone only responds to the Player tag anyway.
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = false;

        // Strong downward kick to start the fall cleanly.
        rb.linearVelocity = new Vector3(0f, -6f, 0f);
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    /// <summary>
    /// Finds the tile whose XZ centre is closest to the robot's current XZ position.
    /// Used by the Bug 2 fix: when the raycast says "no ground" AND this tile is
    /// broken, we know for certain the robot is over a hole and should fall — even
    /// if the capsule is physically resting on an adjacent edge.
    /// </summary>
    private DisappearMechanic GetTileUnderRobot()
    {
        if (tileManager == null || tileManager.allTiles == null) return null;

        float bestSqr = float.MaxValue;
        DisappearMechanic bestTile = null;
        Vector3 pos = transform.position;

        foreach (DisappearMechanic tile in tileManager.allTiles)
        {
            if (tile == null) continue;
            float dx = tile.transform.position.x - pos.x;
            float dz = tile.transform.position.z - pos.z;
            float sqr = dx * dx + dz * dz;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestTile = tile;
            }
        }

        return bestTile;
    }

    private void PickNewTarget()
    {
        if (tileManager == null || tileManager.allTiles == null ||
            tileManager.allTiles.Length == 0) return;

        List<DisappearMechanic> candidates = new List<DisappearMechanic>();
        foreach (DisappearMechanic tile in tileManager.allTiles)
        {
            if (tile == null || tile == currentTarget) continue;
            if (tile.IsBroken()) continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0) return;
        currentTarget = candidates[Random.Range(0, candidates.Count)];
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    public void StartMoving()
    {
        isRunning = true;
        PickNewTarget();
    }

    public void Stop()
    {
        isRunning = false;
        if (rb   != null) rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);
        enabled = false;
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (paused && rb != null) rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetFloat("Speed", paused ? 0f : moveSpeed);
    }
}
