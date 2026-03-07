using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RobotAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }
    public State currentState = State.Patrol;

    public Transform[] patrolPoints;
    private int patrolIndex = 0;
    private int patrolDirection = 1;

    public float patrolWaitTime = 1.5f;
    private float patrolWaitTimer = 0f;
    private bool isWaitingAtPoint = false;

    public float chaseRange = 10f;
    public float attackRange = 2.5f;
    public float rotationSpeed = 4f;

    public Transform playerOverride; // optional override for VR camera
    private Transform playerHead;
    private NavMeshAgent agent;

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
    public AudioClip attackSound;
    public AudioClip chaseSound;

    public AudioClip footstepClip;
    public float footstepInterval = 0.4f;
    private float footstepTimer = 0f;

    // ---------------------------------------------
    // ATTACK TIMING
    // ---------------------------------------------

    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    private State previousState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // Use override if provided, otherwise use Camera.main
        if (playerOverride != null)
            playerHead = playerOverride;
        else
            playerHead = Camera.main.transform;

        previousState = currentState;
        OnEnterState(currentState);
        SetEmotion(7); // Always hostile
    }

    void Update()
    {
        // Detect and handle state transitions
        if (currentState != previousState)
        {
            OnExitState(previousState);
            OnEnterState(currentState);
            previousState = currentState;
        }

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

        // Drive walk/run animation from NavMesh velocity
        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);

        HandleFootsteps();
    }

    // ---------------------------------------------
    // STATE ENTER / EXIT
    // ---------------------------------------------

    void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Patrol:
                if (anim != null) { anim.SetFloat("run", 0f); anim.SetBool("Battle", false); }
                break;

            case State.Chase:
                if (anim != null) { anim.SetFloat("run", 1f); anim.SetBool("Battle", false); }
                if (audioSource != null && chaseSound != null)
                    audioSource.PlayOneShot(chaseSound);
                break;

            case State.Attack:
                if (anim != null) anim.SetBool("Battle", true);
                attackTimer = -0.5f; // 0.5s grace period for BattleMotions transition
                break;
        }
    }

    void OnExitState(State state)
    {
        if (state == State.Attack && anim != null)
        {
            anim.SetBool("Battle", false);
            anim.SetBool("Hit", false);
            foreach (string b in attackBools)
                anim.SetBool(b, false);
            lastAttackBool = null;
        }
    }

    // ---------------------------------------------
    // STATES
    // ---------------------------------------------

    void Patrol()
    {
        if (PlayerInChaseRange())
        {
            isWaitingAtPoint = false;
            currentState = State.Chase;
            return;
        }

        // Single patrol point — just stand and watch
        if (patrolPoints.Length <= 1)
        {
            agent.isStopped = true;
            return;
        }

        if (isWaitingAtPoint)
        {
            agent.isStopped = true;
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                isWaitingAtPoint = false;
                patrolWaitTimer = 0f;
                AdvancePatrolIndex();
                agent.SetDestination(patrolPoints[patrolIndex].position);
                agent.isStopped = false;
            }
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            isWaitingAtPoint = true;
    }

    void AdvancePatrolIndex()
    {
        patrolIndex += patrolDirection;
        if (patrolIndex >= patrolPoints.Length)
        {
            patrolDirection = -1;
            patrolIndex = patrolPoints.Length - 2;
        }
        else if (patrolIndex < 0)
        {
            patrolDirection = 1;
            patrolIndex = 1;
        }
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

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            TriggerAttack();
        }

        if (HorizontalDistanceToPlayer() > attackRange)
            currentState = State.Chase;
    }

    // ---------------------------------------------
    // HELPERS
    // ---------------------------------------------

    void HandleFootsteps()
    {
        if (footstepClip == null || audioSource == null) return;

        bool isMoving = agent.velocity.magnitude > 0.1f;

        if (isMoving)
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

    void TriggerAttack()
    {
        if (anim != null)
            StartCoroutine(PlayAttackAnimation());

        if (audioSource != null && attackSound != null)
            audioSource.PlayOneShot(attackSound);
    }

    private string[] attackBools = { "Thumb", "Cry", "Win", "Angry" };
    private string lastAttackBool = null;

    IEnumerator PlayAttackAnimation()
    {
        // Reset previous attack bool
        if (lastAttackBool != null)
            anim.SetBool(lastAttackBool, false);

        // Pick a random attack
        lastAttackBool = attackBools[Random.Range(0, attackBools.Length)];
        anim.SetBool(lastAttackBool, true);

        // Reset after animation has time to play
        yield return new WaitForSeconds(1.0f);
        anim.SetBool(lastAttackBool, false);
    }

    void SetEmotion(int index)
    {
        if (emotionChanger != null)
        {
            emotionChanger.SetEmotionEyes(index);
            emotionChanger.SetEmotionMouth(index);
        }
        if (robotColorManager != null)
            robotColorManager.ChangeBodyColor(index);
    }

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
