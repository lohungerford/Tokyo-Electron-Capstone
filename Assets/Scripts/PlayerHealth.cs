using System;
using UnityEngine;

/// <summary>
/// Tracks player hit points for Level 2+.
///
/// Place on the same GameObject as the "Player" tag (Camera Rig root).
/// Level1Manager subscribes to OnDeath to trigger the lose condition.
/// HitFlashEffect subscribes to OnHit for the screen flash.
/// PlayerHealthUI subscribes to OnHealthChanged to update the hand HUD.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 10;

    /// <summary>Fires whenever health changes — passes (currentHealth, maxHealth).</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Fires on every hit, even if health is still above zero.</summary>
    public event Action OnHit;

    /// <summary>Fires once when health reaches zero.</summary>
    public event Action OnDeath;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Start()
    {
        ResetHealth();
    }

    /// <summary>
    /// Called by RobotAI each time it lands a hit.
    /// Ignored if the player is already dead.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHit?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Resets health to max. Called automatically on Start and on scene reload.
    /// </summary>
    public void ResetHealth()
    {
        IsDead = false;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    // -------------------------------------------------------
    // Debug helpers (right-click the component in Inspector)
    // -------------------------------------------------------

    [ContextMenu("DEBUG/Take 2 Damage")]
    private void DebugTakeDamage() => TakeDamage(2);

    [ContextMenu("DEBUG/Reset Health")]
    private void DebugReset() => ResetHealth();
}
