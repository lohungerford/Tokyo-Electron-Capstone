using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the Tutorial2 game loop:
///   1. Evil robot is visible but inactive until player presses START
///   2. Robot activates (patrols/animates behind glass — visual only)
///   3. Player finds the generator and presses the button
///   4. All RobotAI in the scene shut down → SUCCESS
///   No fail condition — robot is separated by glass wall.
///
/// The ShutDownAllRobots() method is also reusable for Level 2/3
/// by calling it directly with shutdownPermanent = false.
/// </summary>
public class Tutorial2Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotAI evilRobot;
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Generator Settings")]
    [Tooltip("If true, robots stay off permanently after generator press (use for tutorial). " +
             "If false, robots wake up after shutdownDuration (use for levels).")]
    [SerializeField] private bool shutdownPermanent = true;
    [Tooltip("How long robots stay disabled before waking up (only used if shutdownPermanent = false)")]
    [SerializeField] private float shutdownDuration = 8f;

    // internals
    private bool gameStarted = false;
    private bool outcomeResolved = false;
    private bool isShutDown = false;
    private float shutdownTimer = 0f;

    private void Start()
    {
        if (evilRobot != null)
        {
            // Keep robot inactive until START is pressed
            evilRobot.enabled = false;
            NavMeshAgent agent = evilRobot.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }
    }

    private void Update()
    {
        if (!gameStarted || outcomeResolved) return;

        // Count down wakeup timer if temporarily shut down (levels only)
        if (isShutDown && !shutdownPermanent)
        {
            shutdownTimer -= Time.deltaTime;
            if (shutdownTimer <= 0f)
                WakeUpRobots();
        }
    }

    // -----------------------------------------------
    // Called by START button InteractableUnityEventWrapper
    // -----------------------------------------------
    public void StartTutorial()
    {
        if (gameStarted) return;
        gameStarted = true;

        if (evilRobot == null) return;

        // Activate robot so it animates/patrols behind the glass
        NavMeshAgent agent = evilRobot.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true;
        evilRobot.enabled = true;
    }

    // -----------------------------------------------
    // Called by Generator button InteractableUnityEventWrapper
    // -----------------------------------------------
    public void OnGeneratorPressed()
    {
        if (!gameStarted || outcomeResolved || isShutDown) return;

        ShutDownAllRobots();

        if (shutdownPermanent)
        {
            outcomeResolved = true;
            tutorialUI?.ShowSuccess();
        }
    }

    // -----------------------------------------------
    // Disable all RobotAI in the scene
    // -----------------------------------------------
    public void ShutDownAllRobots()
    {
        isShutDown = true;
        shutdownTimer = shutdownDuration;

        foreach (RobotAI robot in FindObjectsByType<RobotAI>(FindObjectsSortMode.None))
        {
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Animator anim = robot.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
                anim.enabled = false; // freeze robot in place
            }

            robot.enabled = false;
        }
    }

    // -----------------------------------------------
    // Wake all robots back up (used in levels, not tutorial)
    // -----------------------------------------------
    public void WakeUpRobots()
    {
        isShutDown = false;

        foreach (RobotAI robot in FindObjectsByType<RobotAI>(FindObjectsSortMode.None))
        {
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            if (agent != null) agent.isStopped = false;

            Animator anim = robot.GetComponent<Animator>();
            if (anim != null) anim.enabled = true;

            robot.enabled = true;
        }
    }
}
