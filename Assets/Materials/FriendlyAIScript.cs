using UnityEngine;
using UnityEngine.AI;

public class FriendlyAIScript : MonoBehaviour
{
    
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.2f;
    public float stoppingDistance = 0.4f;
    public float idleTimeAtPoint = 1.5f;

    
    public float stuckTimeout = 3f; // seconds before forcing a new destination

    private NavMeshAgent agent;
    private int currentIndex;
    private float idleTimer;
    private float stuckTimer;
    private Vector3 lastPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = true;

        if (patrolPoints.Length > 0)
            GoToNextPoint();
    }

    void Update()
    {
        HandlePatrol();
        DetectIfStuck();
    }

    // ---------------------------------------------
    // PATROL LOGIC
    // ---------------------------------------------

    void HandlePatrol()
    {
        if (agent.pathPending) return;

        // Arrived at destination
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
        if (patrolPoints.Length == 0) return;

        currentIndex = Random.Range(0, patrolPoints.Length);
        agent.SetDestination(patrolPoints[currentIndex].position);
        stuckTimer = 0f;
    }

    // ---------------------------------------------
    // STUCK PREVENTION
    // ---------------------------------------------

    void DetectIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < 0.01f && agent.hasPath)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTimeout)
            {
                GoToNextPoint(); // force new target
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }
}
