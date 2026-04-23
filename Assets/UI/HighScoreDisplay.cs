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
        int levelOneScore = PointsManager.GetSavedLevelScore("LevelOne");
        int levelTwoScore = PointsManager.GetSavedLevelScore("LevelTwo");
        int levelThreeScore = PointsManager.GetSavedLevelScore("LevelThree");
        int highScore = PointsManager.GetSavedHighScore();

        SetLabel(level1ScoreText, levelOneScore);
        SetLabel(level2ScoreText, levelTwoScore);
        SetLabel(level3ScoreText, levelThreeScore);
        SetLabel(finalScoreText, highScore);
    }

    private static void SetLabel(TextMeshProUGUI label, int value)
    {
        if (label != null)
            label.text = value.ToString();
    }
}
