using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Red screen-edge flash when the player takes damage.
///
/// VR SETUP (World Space canvas required — Screen Space Overlay breaks in Quest):
///   1. Create a Canvas → set Render Mode to "World Space".
///   2. Parent the Canvas to your CenterEyeAnchor (or OVRCameraRig/TrackingSpace/CenterEyeAnchor).
///   3. Set canvas local position to (0, 0, 0.31) — just past the near clip plane.
///   4. Set canvas Width/Height so it fills the view at that distance
///      (e.g. Width=0.6, Height=0.34 for ~90° FOV at 0.31m).
///   5. Add a child Image that fills the canvas (Anchor: stretch/stretch).
///      Set its color to red (255, 0, 0, 255) — this script controls the alpha.
///   6. Attach HitFlashEffect to any GameObject in the scene.
///      Drag in the PlayerHealth and the Image.
/// </summary>
public class HitFlashEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image flashImage;

    [Header("Flash Settings")]
    [Tooltip("Maximum alpha reached at the peak of the flash (0 = invisible, 1 = fully opaque).")]
    [SerializeField] private float peakAlpha = 0.45f;
    [Tooltip("How fast the flash fades in (seconds).")]
    [SerializeField] private float fadeInDuration = 0.05f;
    [Tooltip("How long the flash holds at peak before fading (seconds).")]
    [SerializeField] private float holdDuration = 0.08f;
    [Tooltip("How long the flash takes to fade out completely (seconds).")]
    [SerializeField] private float fadeOutDuration = 0.45f;

    private Coroutine flashRoutine;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHit += TriggerFlash;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHit -= TriggerFlash;
    }

    private void Start()
    {
        SetImageAlpha(0f);
    }

    public void TriggerFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return StartCoroutine(LerpAlpha(0f, peakAlpha, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(LerpAlpha(peakAlpha, 0f, fadeOutDuration));
        flashRoutine = null;
    }

    private IEnumerator LerpAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetImageAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetImageAlpha(to);
    }

    private void SetImageAlpha(float alpha)
    {
        if (flashImage == null) return;
        Color c = flashImage.color;
        c.a = alpha;
        flashImage.color = c;
    }
}
