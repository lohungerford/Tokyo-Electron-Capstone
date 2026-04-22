using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's current health on the hand UI.
/// Drives both a Slider (fill bar) and an optional TMP text label.
///
/// Attach to any GameObject in the hand UI canvas.
/// Drag in PlayerHealth, the Slider, and optionally the TMP label.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Slider whose value tracks current HP. Min/Max are set automatically from PlayerHealth.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Optional text label — shows 'current/max' (e.g. '8/10'). Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI healthLabel;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateDisplay;
    }

    private void Start()
    {
        if (playerHealth != null)
            UpdateDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void UpdateDisplay(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthLabel != null)
            healthLabel.text = $"{current}/{max}";
    }
}
