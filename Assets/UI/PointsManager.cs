using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;
using UnityEngine.SceneManagement;

/* ACTION POINT COSTS:  (may need to be adjusted after gameplay testing)
    AI Friendly Robots:
        - Escorting them to safety: +200/robot
        - Letting them fall through open floor tiles: -150/robot
        - Setting up correct barricades around open floor tiles: +10/barricade
            - +50 booster when floor tile fully surrounded, allowing floor tile to refill
              and barricades to disappear about 5 sec after booster given (MAY OR MAY NOT DO)
    AI Enemy Robots/Unsafe Zones:
        - Getting enemy robots to fall through open floor tiles (MAY OR MAY NOT DO) OR using
          power-ups against them to deactivate them: +25/robot
        - Temporarily disabling enemy robots and removing unsafe zones by successfully
          completing circuit puzzle: +75/completion
        - Failing to complete puzzle before enemy robot(s) attack you or unsafe zone
          spreads to your floor tile: -200/fail *
    PPE Minigame:
        - Picking a correct PPE item: +25/item
        - Picking an incorrect PPE item: -10/item
    Player Strategy:
        - Falling through an open floor tile: -400/fall *
        - Implementing a power-up (to either guide a friendly robot or disable an enemy
          robot): +5/power-up

    * = teleports player back to starting point of current level, resetting unsafe zones, enemy
        robots, and friendly robots but NOT resetting the level timer
*/

public class PointsManager : MonoBehaviour
{
    public const string HighScoreKey = "HighScore";
    public const string LevelOneScoreKey = "HighScore_LevelOne";
    public const string LevelTwoScoreKey = "HighScore_LevelTwo";
    public const string LevelThreeScoreKey = "HighScore_LevelThree";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI pointsLabel;

    public event Action<int> OnPointsChanged;
    public event Action<int> OnPointsDelta;

    private int points = 0;

    private void Start()
    {
        ResetPoints();
    }

    // below methods can be called in other scripts

    public void AddPoints(int amount)
    {
        points += amount;
        SaveScoresIfBetter(points);
        UpdateLabel();
        OnPointsDelta?.Invoke(amount);
        OnPointsChanged?.Invoke(points);
    }

    public void SubtractPoints(int amount)
    {
        AddPoints(-Mathf.Abs(amount));
    }

    public void SetPoints(int amount)
    {
        // uncomment below line if want to prevent negative score
        // points = Mathf.Max(0, amount);
        points = amount;
        SaveScoresIfBetter(points);
        UpdateLabel();
        OnPointsChanged?.Invoke(points);
    }

    public void ResetPoints()
    {
        points = 0;
        UpdateLabel();
        OnPointsChanged?.Invoke(points);
    }

    public int GetPoints()
    {
        return points;
    }

    public static int GetSavedHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public static int GetSavedLevelScore(string sceneName)
    {
        string key = GetSceneScoreKey(sceneName);
        return string.IsNullOrEmpty(key) ? 0 : PlayerPrefs.GetInt(key, 0);
    }

    public static void SaveHighScoreIfBetter(int score)
    {
        if (score <= GetSavedHighScore()) return;

        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save();
    }

    public static void SaveLevelScoreIfBetter(string sceneName, int score)
    {
        string key = GetSceneScoreKey(sceneName);
        if (string.IsNullOrEmpty(key)) return;
        if (score <= PlayerPrefs.GetInt(key, 0)) return;

        PlayerPrefs.SetInt(key, score);
        PlayerPrefs.Save();
    }

    private static void SaveScoresIfBetter(int score)
    {
        SaveHighScoreIfBetter(score);
        SaveLevelScoreIfBetter(SceneManager.GetActiveScene().name, score);
    }

    private static string GetSceneScoreKey(string sceneName)
    {
        return sceneName switch
        {
            "LevelOne" => LevelOneScoreKey,
            "LevelTwo" => LevelTwoScoreKey,
            "LevelThree" => LevelThreeScoreKey,
            _ => null
        };
    }

    // helper functions (can't be called elsewhere)
    private void UpdateLabel()
    {
        if (pointsLabel != null)
            pointsLabel.text = $"{points}";
    }

    [ContextMenu("DEBUG/Add +10 Points")]
    private void DebugAdd10()
    {
        AddPoints(10);
    }

    [ContextMenu("DEBUG/Subtract -10 Points")]
    private void DebugSub10()
    {
        SubtractPoints(10);
    } 
}
