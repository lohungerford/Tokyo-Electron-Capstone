using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Friendly robot AI with two movement modes:
///   - Patrol mode  (useBounceMovement = false): NavMesh patrol between waypoints. Used in Tutorial 1.
///   - Bounce mode  (useBounceMovement = true):  Raycast-based movement. Walks straight, bounces off
///                                                walls/barriers, falls through open floor tiles.
///                                                No Rigidbody needed. Used in Level 1+.
/// Audio/footstep and emotion setup work in both modes.
/// </summary>
public class FriendlyAIScript : MonoBehaviour
{
    // ---------------------------------------------
    // PATROL (NavMesh) settings
    // ---------------------------------------------
    [Header("Patrol Mode (NavMesh)")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.2f;
    public float stoppingDistance = 0.4f;
    public float idleTimeAtPoint = 1.5f;
    public float stuckTimeout = 3f;

    // ---------------------------------------------
    // BOUNCE (Raycast) settings
    // ---------------------------------------------
    [Header("Bounce Mode (Raycast)")]
    [Tooltip("Enable for level scenes where robots bounce around and can fall through open tiles.")]
    public bool useBounceMovement = false;
    public float bounceSpeed = 1.5f;
    public float rotationSpeed = 10f;
    [Tooltip("How far ahead the robot checks for walls")]
    public float wallDetectDistance = 0.6f;
    [Tooltip("Radius of the wall detection sphere")]
    public float wallDetectRadius = 0.3f;
    [Tooltip("Height above pivot to cast wall detection ray from (must be > wallDetectRadius)")]
    public float wallDetectHeight = 0.6f;
    [Tooltip("How far down to check for floor")]
    public float groundCheckDistance = 1.5f;
    [Tooltip("Small offset above pivot to start ground raycast from")]
    public float groundRayOffset = 0.3f;
    public float gravity = 15f;
    [Tooltip("Y position below which the robot is considered fallen and gets destroyed")]
    public float destroyBelowY = -10f;

    // ---------------------------------------------
    // ANIMATION
    // ---------------------------------------------
    private Animator anim;
    public EmotionChanger emotionChanger;
    public RobotColorManager robotColorManager;

    // ---------------------------------------------
    // AUDIO
    // ---------------------------------------------
    public AudioSource audioSource;
    public AudioClip footstepClip;
    public float footstepInterval = 0.4f;
    private float footstepTimer = 0f;

    // ---------------------------------------------
    // PRIVATE
    // ---------------------------------------------
    private NavMeshAgent agent;
    private Rigidbody rb;

    // Patrol-mode state
    private int currentIndex;
    private float idleTimer;
    private float stuckTimer;
    private Vector3 lastPosition;

    // Bounce-mode state
    private Vector3 moveDirection;
    private float fallSpeed;
    private bool isGrounded;

    // =============================================
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
        anim  = GetComponent<Animator>();

        // Friendly — always happy
        if (emotionChanger != null)
        {
            emotionChanger.SetEmotionEyes(1);
            emotionChanger.SetEmotionMouth(1);
        }
        if (robotColorManager != null)
            robotColorManager.ChangeBodyColor(1);

        if (useBounceMovement)
            InitBounceMode();
        else
            InitPatrolMode();
    }

    // =============================================
    void Update()
    {
        if (!useBounceMovement)
        {
            HandlePatrol();
            DetectIfStuck();

            float agentSpeed = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;
            if (anim != null) anim.SetFloat("Speed", agentSpeed);
            HandleFootsteps(agentSpeed);
        }
        else
        {
            HandleBounceMovement();

            float speed = isGrounded && moveDirection != Vector3.zero ? bounceSpeed : 0f;
            if (anim != null) anim.SetFloat("Speed", speed);
            HandleFootsteps(speed);
        }
    }

    // =============================================
    // BOUNCE MOVEMENT (no Rigidbody, all raycasts)
    // =============================================

    void HandleBounceMovement()
    {
        float dt = Time.deltaTime;

        // --- GROUND CHECK ---
        // Cast a ray downward from slightly above the pivot
        Vector3 rayOrigin = transform.position + Vector3.up * groundRayOffset;
        RaycastHit groundHit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out groundHit, groundCheckDistance))
        {
            // Snap to the floor surface
            transform.position = new Vector3(
                transform.position.x,
                groundHit.point.y,
                transform.position.z
            );
            fallSpeed = 0f;
            isGrounded = true;
        }
        else
        {
            // No floor — fall
            isGrounded = false;
            fallSpeed += gravity * dt;
            transform.position += Vector3.down * fallSpeed * dt;

            // Destroy if fallen too far
            if (transform.position.y < destroyBelowY)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Only move horizontally when grounded
        if (!isGrounded) return;

        // --- WALL CHECK ---
        // SphereCast forward to detect walls and barriers
        Vector3 wallRayOrigin = transform.position + Vector3.up * wallDetectHeight;
        RaycastHit wallHit;

        if (Physics.SphereCast(wallRayOrigin, wallDetectRadius, moveDirection, out wallHit, wallDetectDistance))
        {
            // Ignore floor-tagged objects and upward-facing surfaces (tile edges)
            if (!wallHit.collider.CompareTag("Floor") && wallHit.normal.y < 0.3f)
            {
                // Bounce: pick a random direction AWAY from the wall
                // Use the wall normal as the base "away" direction, then randomize within a spread
                Vector3 normal = wallHit.normal;
                normal.y = 0f;
                normal.Normalize();

                if (normal != Vector3.zero)
                {
                    // Push the robot away from the wall so it can't clip through
                    transform.position += normal * 0.15f;

                    // Random angle within ±60 degrees of the wall normal
                    // This always sends the robot away from the wall, never along it
                    float randomAngle = Random.Range(-60f, 60f);
                    moveDirection = Quaternion.Euler(0f, randomAngle, 0f) * normal;
                    moveDirection.y = 0f;
                    moveDirection.Normalize();
                }
            }
        }

        // --- MOVE ---
        transform.position += moveDirection * bounceSpeed * dt;

        // --- ROTATE to face movement direction ---
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationSpeed * dt);
        }
    }

    // =============================================
    // MODE INITIALISATION
    // =============================================

    private void InitPatrolMode()
    {
        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.updateRotation = true;
        }

        // Keep Rigidbody kinematic so NavMesh drives movement
        if (rb != null) rb.isKinematic = true;

        if (patrolPoints != null && patrolPoints.Length > 0)
            GoToNextPoint();
    }

    private void InitBounceMode()
    {
        // Disable NavMeshAgent completely
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        // Rigidbody not needed — make it kinematic so it doesn't interfere
        if (rb != null) rb.isKinematic = true;

        SetRandomDirection();
        fallSpeed = 0f;
        isGrounded = true;
    }

    // =============================================
    // PATROL LOGIC
    // =============================================

    void HandlePatrol()
    {
        if (agent == null || !agent.enabled) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeAtPoint)
            {
                GoToNextPoint();
                idleTimer = 0f;
            }
        }
    }

    void GoToNextPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        currentIndex = Random.Range(0, patrolPoints.Length);
        agent.SetDestination(patrolPoints[currentIndex].position);
        stuckTimer = 0f;
    }

    void DetectIfStuck()
    {
        if (agent == null || !agent.enabled) return;

        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        if (movedDistance < 0.01f && agent.hasPath)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
                GoToNextPoint();
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;
    }

    // =============================================
    // FOOTSTEPS
    // =============================================

    void HandleFootsteps(float speed)
    {
        if (footstepClip == null || audioSource == null) return;

        if (speed > 0.1f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                audioSource.PlayOneShot(footstepClip);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // =============================================
    // HELPERS
    // =============================================

    private void SetRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        moveDirection.y = 0f;
        moveDirection.Normalize();
    }

    /// <summary>Stops the robot — call this when shutting down or game ends.</summary>
    public void Stop()
    {
        moveDirection = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null) anim.SetFloat("Speed", 0f);
        enabled = false;
    }
}
