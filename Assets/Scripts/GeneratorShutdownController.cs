using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Level 2 generator mechanic.
///
/// Attach one instance of this to a manager GameObject in the LevelTwo scene.
/// Drag all toxic/enemy robots into the <see cref="enemyRobots"/> array.
/// Wire BOTH generator button InteractableUnityEventWrapper OnClick events
/// to call <see cref="OnGeneratorPressed"/> on this component.
///
/// Behaviour:
///   - Pressing either generator pauses all enemy robots for <see cref="shutdownDuration"/> seconds.
///   - Pressing again while already paused RESETS the timer (both generators are useful).
///   - Robots freeze in place (NavMeshAgent stopped, Animator frozen, RobotAI disabled).
///   - After the timer expires, robots wake back up in Chase state so they immediately re-engage.
/// </summary>
public class GeneratorShutdownController : MonoBehaviour
{
    [Header("Enemy Robots")]
    [Tooltip("Drag all toxic/enemy RobotAI GameObjects here.")]
    [SerializeField] private RobotAI[] enemyRobots;

    [Header("Settings")]
    [Tooltip("How long (seconds) enemies stay paused after a generator press.")]
    [SerializeField] private float shutdownDuration = 10f;

    // -------------------------------------------------------
    // State
    // -------------------------------------------------------
    private bool isShutDown = false;
    private Coroutine wakeUpCoroutine;

    // -------------------------------------------------------
    // Called by both generator button InteractableUnityEventWrappers
    // -------------------------------------------------------

    /// <summary>
    /// Call this from both generator button OnClick events.
    /// Pressing while already shut down resets the 10-second window.
    /// </summary>
    public void OnGeneratorPressed()
    {
        // Cancel any in-progress wake-up countdown so we can restart it.
        if (wakeUpCoroutine != null)
            StopCoroutine(wakeUpCoroutine);

        ShutDownRobots();
        wakeUpCoroutine = StartCoroutine(WakeUpAfterDelay(shutdownDuration));
    }

    // -------------------------------------------------------
    // Internals
    // -------------------------------------------------------

    private void ShutDownRobots()
    {
        isShutDown = true;

        foreach (RobotAI robot in enemyRobots)
        {
            if (robot == null) continue;

            // Stop the NavMeshAgent so the robot freezes in place.
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // Freeze the Animator so the robot holds its current pose.
            Animator anim = robot.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
                anim.enabled = false;
            }

            // Disable the AI script so state-machine updates stop.
            robot.enabled = false;
        }
    }

    private IEnumerator WakeUpAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        WakeUpRobots();
        wakeUpCoroutine = null;
    }

    private void WakeUpRobots()
    {
        isShutDown = false;

        foreach (RobotAI robot in enemyRobots)
        {
            if (robot == null) continue;

            // Re-enable Animator before re-enabling RobotAI so it can play animations.
            Animator anim = robot.GetComponent<Animator>();
            if (anim != null) anim.enabled = true;

            // Resume NavMeshAgent.
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            if (agent != null) agent.isStopped = false;

            // Re-enable RobotAI and jump straight to Chase so it
            // immediately re-engages rather than wandering back to patrol.
            robot.enabled = true;
            robot.currentState = RobotAI.State.Chase;
        }
    }

    // -------------------------------------------------------
    // Read-only status (useful for Level2Manager if needed later)
    // -------------------------------------------------------

    /// <summary>True while enemies are currently shut down.</summary>
    public bool IsShutDown => isShutDown;
}
