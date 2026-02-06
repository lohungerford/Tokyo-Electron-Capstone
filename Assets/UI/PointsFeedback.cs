using System.Collections;
using UnityEngine;
using TMPro;

public class PointsFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointsManager pointsManager;
    [SerializeField] private TextMeshProUGUI popupLabel;
    [SerializeField] private CanvasGroup popupCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float showDuration = 1f;       // pop-up shows for 1 second
    [SerializeField] private float fadeDuration = 0.25f;    // pop-up takes 1/4 sec to fade

    private Coroutine popupRoutine;

    private void OnEnable()
    {
        if (pointsManager != null)
            pointsManager.OnPointsDelta += HandlePointsDelta;
    }

    private void OnDisable()
    {
        if (pointsManager != null)
            pointsManager.OnPointsDelta -= HandlePointsDelta;
    }

    private void Start()
    {
        popupCanvasGroup.alpha = 0f;        // hides point pop-up at start
    }

    private void HandlePointsDelta(int delta)
    {
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(ShowPopup(delta));
    }

    private IEnumerator ShowPopup(int delta)
    {
        string sign = delta > 0 ? "+" : "-";
        popupLabel.text = $"{sign}{Mathf.Abs(delta)}";

        // sets pop-up color based on point gain/deduction
        popupLabel.color = delta > 0 ? Color.green : Color.red;

        popupCanvasGroup.alpha = 1f;        // makes pop-up fully visible

        yield return new WaitForSeconds(showDuration);

        float t = 0f;
        float startAlpha = popupCanvasGroup.alpha;

        while (t < fadeDuration)        // fades pop-up
        {
            t += Time.deltaTime;
            popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }

        popupCanvasGroup.alpha = 0f;        // makes pop-up invisible
        popupRoutine = null;
    }
}
