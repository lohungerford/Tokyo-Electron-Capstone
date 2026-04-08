using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private PointsManager pointsManager;

    [Header("Timer Settings")]
    [SerializeField] private float startSeconds = 5f * 60f; // 5 minutes
    [SerializeField] private bool startOnEnable = true;

    private float remaining;
    private bool running;

    private void OnEnable()
    {
        ResetTimer();
        if (startOnEnable) StartTimer();
    }

    private void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            // need to write code to trigger what happens when time hits 0
        }

        UpdateLabel();
    }

    public void StartTimer(){
        running = true;
        pointsManager?.ResetPoints();       // comment out for actual gameplay
    }
    public void PauseTimer() => running = false;

    public void ResetTimer()
    {
        remaining = startSeconds;
        running = false;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        int totalSeconds = Mathf.CeilToInt(remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerLabel.text = $"{minutes:0}:{seconds:00}";
    }
}
