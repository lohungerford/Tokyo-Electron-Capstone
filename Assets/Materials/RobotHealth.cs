using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RobotHealth : MonoBehaviour
{
    public int maxHealth = 5;
    private int currentHealth;

    // ---------------------------------------------
    // AUDIO
    // ---------------------------------------------

    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    // ---------------------------------------------
    // INTERNALS
    // ---------------------------------------------

    private Animator anim;
    private NavMeshAgent agent;
    private RobotAI robotAI;
    private FriendlyAIScript friendlyAI;
    private bool isDead = false;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        robotAI = GetComponent<RobotAI>();
        friendlyAI = GetComponent<FriendlyAIScript>();

        // Cache ragdoll components and disable them at start
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        foreach (Rigidbody rb in ragdollBodies)
            rb.isKinematic = true;
    }

    public void TakeHit()
    {
        if (isDead) return;

        currentHealth--;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        if (anim != null)
            StartCoroutine(PlayHitAnimation());

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator PlayHitAnimation()
    {
        anim.SetInteger("vary", Random.Range(0, 3));
        anim.SetBool("Hit", true);
        yield return new WaitForSeconds(0.6f);
        anim.SetBool("Hit", false);
    }

    void Die()
    {
        isDead = true;

        // Stop all AI behaviour
        if (robotAI != null) robotAI.enabled = false;
        if (friendlyAI != null) friendlyAI.enabled = false;
        if (agent != null) agent.isStopped = true;

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        EnableRagdoll();
    }

    void EnableRagdoll()
    {
        // Disable animator so physics takes over
        if (anim != null) anim.enabled = false;

        // Disable NavMeshAgent fully so it doesn't fight physics
        if (agent != null) agent.enabled = false;

        // Enable all ragdoll rigidbodies
        foreach (Rigidbody rb in ragdollBodies)
            rb.isKinematic = false;
    }
}
