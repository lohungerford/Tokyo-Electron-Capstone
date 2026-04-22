using TMPro;
using UnityEngine;

public class HighScoreDisplay : MonoBehaviour
{
    [Header("Scoreboard Labels")]
    [SerializeField] private TextMeshProUGUI level1ScoreText;
    [SerializeField] private TextMeshProUGUI level2ScoreText;
    [SerializeField] private TextMeshProUGUI level3ScoreText;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private void OnEnable()
    {
        Refresh();
    }

    [ContextMenu("Refresh High Score")]
    public void Refresh()
    {
        int highScore = PointsManager.GetSavedHighScore();

        SetLabel(level1ScoreText, highScore);
        SetLabel(level2ScoreText, 0);
        SetLabel(level3ScoreText, 0);
        SetLabel(finalScoreText, highScore);
    }

    private static void SetLabel(TextMeshProUGUI label, int value)
    {
        if (label != null)
            label.text = value.ToString();
    }
}
