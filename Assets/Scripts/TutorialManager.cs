using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the Tutorial1 game loop:
///   1. Robot is inactive until player presses START
///   2. Robot walks toward the open floor tile
///   3. SUCCESS: robot's NavMesh velocity stays near zero (blocked by barrier)
///   4. FAIL:    robot reaches the hole edge, NavMeshAgent is disabled,
///               Rigidbody takes over and robot falls through
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FriendlyAIScript friendlyRobot;
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Robot Target")]
    [Tooltip("Place this transform on the far side of the hole — robot walks toward it")]
    [SerializeField] private Transform tileTarget;

    [Header("Hole Detection")]
    [Tooltip("Place this transform at the edge of the NavMesh, just before the hole")]
    [SerializeField] private Transform holeEdge;
    [Tooltip("How close the robot needs to be to the hole edge before it falls")]
    [SerializeField] private float holeEdgeDistance = 0.6f;

    [Header("Success Detection")]
    [Tooltip("How long the robot must be stopped before counting as success")]
    [SerializeField] private float stoppedDuration = 2f;
    [Tooltip("Velocity below this counts as stopped")]
    [SerializeField] private float stoppedVelocityThreshold = 0.05f;

    // internals
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator anim;
    private bool gameStarted = false;
    private bool outcomeResolved = false;
    private bool robotFalling = false;
    private float stoppedTimer = 0f;

    private void Start()
    {
        if (friendlyRobot != null)
        {
            friendlyRobot.enabled = false;
            agent = friendlyRobot.GetComponent<NavMeshAgent>();
            anim = friendlyRobot.GetComponent<Animator>();
            rb = friendlyRobot.GetComponent<Rigidbody>();

            if (agent != null) agent.enabled = false;

            // make sure rigidbody is kinematic at start (agent controls movement)
            if (rb != null) rb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (!gameStarted || outcomeResolved) return;

        if (robotFalling) return; // let physics handle it

        // drive walk animation from NavMesh velocity
        if (anim != null && agent != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);

        DetectHoleEdge();
        DetectSuccess();
    }

    // -----------------------------------------------
    // Called by START button
    // -----------------------------------------------
    public void StartTutorial()
    {
        if (gameStarted) return;
        gameStarted = true;

        if (friendlyRobot == null || agent == null) return;

        agent.enabled = true;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;

        if (tileTarget != null)
            agent.SetDestination(tileTarget.position);
    }

    // -----------------------------------------------
    // Check if robot reached the hole edge
    // -----------------------------------------------
    private void DetectHoleEdge()
    {
        if (holeEdge == null || agent == null) return;

        float dist = Vector3.Distance(friendlyRobot.transform.position, holeEdge.position);

        if (dist <= holeEdgeDistance)
            StartFall();
    }

    // -----------------------------------------------
    // Disable NavMeshAgent and let robot fall via physics
    // -----------------------------------------------
    private void StartFall()
    {
        robotFalling = true;

        // stop and disable NavMeshAgent so physics takes over
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // enable rigidbody gravity so robot falls
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // play idle/fall animation
        if (anim != null)
            anim.SetFloat("Speed", 0f);

        // notify UI after short delay so player sees the robot fall
        Invoke(nameof(TriggerFail), 1.5f);
    }

    private void TriggerFail()
    {
        outcomeResolved = true;
        tutorialUI?.ShowFail();
    }

    // -----------------------------------------------
    // Success detection — robot stopped by barrier
    // -----------------------------------------------
    private void DetectSuccess()
    {
        if (agent == null || !agent.enabled) return;
        if (!agent.hasPath) return;

        bool isStopped = agent.velocity.magnitude < stoppedVelocityThreshold;

        if (isStopped)
        {
            stoppedTimer += Time.deltaTime;
            if (stoppedTimer >= stoppedDuration)
            {
                outcomeResolved = true;
                tutorialUI?.ShowSuccess();
            }
        }
        else
        {
            stoppedTimer = 0f;
        }
    }
}
