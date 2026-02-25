using UnityEngine;
using UnityEngine.AI;

public class RobotAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }
    public State currentState = State.Patrol;

    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    public float chaseRange = 1f;
    public float attackRange = 1.5f;
    public float rotationSpeed = 4f;

    public Transform playerOverride; // optional override for VR camera
    private Transform playerHead;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Use override if provided, otherwise use Camera.main
        if (playerOverride != null)
            playerHead = playerOverride;
        else
            playerHead = Camera.main.transform;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                Attack();
                break;
        }
    }

    // ---------------------------------------------
    // STATES
    // ---------------------------------------------

    void Patrol()
    {
        agent.isStopped = false;

        // Move to next patrol point
        agent.SetDestination(patrolPoints[patrolIndex].position);

        // If reached patrol point, switch to next
        if (!agent.pathPending && agent.remainingDistance < 0.4f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        // Swap to Chase if player close
        if (PlayerInChaseRange())
            currentState = State.Chase;
    }

    void Chase()
    {
        agent.isStopped = false;

        agent.SetDestination(playerHead.position);
        RotateTowardPlayer();

        float dist = HorizontalDistanceToPlayer();

        if (dist < attackRange)
            currentState = State.Attack;

        if (dist > chaseRange)
            currentState = State.Patrol;
    }

    void Attack()
    {
        agent.isStopped = true;

        RotateTowardPlayer();

        // Debug attack (replace with animation later)
        Debug.Log("Robot attacks!!");

        float dist = HorizontalDistanceToPlayer();

        if (dist > attackRange)
            currentState = State.Chase;
    }

    // ---------------------------------------------
    // HELPERS
    // ---------------------------------------------

    float HorizontalDistanceToPlayer()
    {
        Vector3 flatRobot = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayer = new Vector3(playerHead.position.x, 0, playerHead.position.z);

        return Vector3.Distance(flatRobot, flatPlayer);
    }

    bool PlayerInChaseRange()
    {
        return HorizontalDistanceToPlayer() < chaseRange;
    }

    void RotateTowardPlayer()
    {
        Vector3 direction = (playerHead.position - transform.position);
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }
}
