using UnityEngine;
using UnityEngine.AI;

public class FriendlyAIScript : MonoBehaviour
{
    [Header("Follow Settings")]
    public float followDistance = 1.5f;   // how close the robot gets
    public float stopDistance = 1.2f;     // when it fully stops
    public float followSpeed = 1.2f;      // slow, friendly pace
    public float rotationSpeed = 4f;

    [Header("Player Target")]
    public Transform playerOverride;      // VR camera or fallback
    private Transform playerHead;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = followSpeed;
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = false; // we rotate manually

        if (playerOverride != null)
            playerHead = playerOverride;
        else
            playerHead = Camera.main.transform;
    }

    void Update()
    {
        FollowPlayer();
        RotateTowardPlayer();
    }

    // ---------------------------------------------
    // FOLLOW LOGIC
    // ---------------------------------------------

    void FollowPlayer()
    {
        float dist = HorizontalDistanceToPlayer();

        if (dist > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(playerHead.position);
        }
        else
        {
            agent.isStopped = true;
        }
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

    void RotateTowardPlayer()
    {
        Vector3 direction = playerHead.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );
        }
    }
}
