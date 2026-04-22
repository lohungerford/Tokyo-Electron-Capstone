using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Level 1 game loop:
///   1. Player sees instruction screen, presses START
///   2. Robots start bouncing, tiles start disappearing
///   3. Score = aliveRobots per second (5 bots = 5 pts/sec)
///   4. Losing a robot reduces the multiplier (fewer points per second)
///   5. Lose condition: player falls OR 3+ robots fall
///   6. Game ends when timer runs out — final score shown
///
/// Integrates with: LevelTileManager, PointsManager, TutorialUI (reused for panels),
///                  GameHUD (pause support), FriendlyAIScript (Level 1 robot movement).
/// </summary>
public class Level1Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelTileManager tileManager;
    [SerializeField] private PointsManager pointsManager;
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Robots")]
    [Tooltip("Drag all friendly robots with FriendlyAIScript here")]
    [SerializeField] private FriendlyAIScript[] robots;

    [Header("HUD Text")]
    [Tooltip("The dynamic countdown label on the HUD (e.g. the TMP text that shows 1:30)")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Game Settings")]
    [Tooltip("Total level duration in seconds")]
    [SerializeField] private float levelDuration = 90f;
    [Tooltip("Points awarded per alive robot per second")]
    [SerializeField] private int pointsPerRobotPerSecond = 1;
    [Tooltip("How many robots must fall before the player loses")]
    [SerializeField] private int maxRobotDeaths = 3;

    [Header("Player Fall Detection")]
    [Tooltip("If the player's Y goes below this, they have fallen")]
    [SerializeField] private float playerFallY = -5f;
    [SerializeField] private Transform playerTransform;

    // State
    private bool gameStarted = false;
    private bool gameOver = false;
    private bool isPaused = false;
    private float timer;
    private float pointAccumulator = 0f;
    private int totalTrackedRobots = 0;
    private int robotsAlive;
    private int robotsFallen = 0;

    private void Start()
    {
        totalTrackedRobots = 0;
        foreach (FriendlyAIScript robot in robots)
        {
            if (robot != null)
            {
                totalTrackedRobots++;
                robot.PrepareForExternalStart();
            }
        }

        timer = levelDuration;
        CountAliveRobots();
        UpdateHUD();
    }

    private void Update()
    {
        if (!gameStarted || gameOver || isPaused) return;

        float dt = Time.deltaTime;

        // --- Timer ---
        timer -= dt;
        if (timer <= 0f)
        {
            timer = 0f;
            OnTimerEnd();
            return;
        }

        // --- Check alive robots ---
        CountAliveRobots();

        // --- Lose condition: too many robots fell ---
        if (robotsFallen >= maxRobotDeaths)
        {
            OnLose("Too many robots fell!");
            return;
        }

        // --- Lose condition: player fell ---
        if (playerTransform != null && playerTransform.position.y < playerFallY)
        {
            RestartSceneForPlayerFall();
            return;
        }

        // --- Score: add points per alive robot per second ---
        pointAccumulator += robotsAlive * pointsPerRobotPerSecond * dt;
        if (pointAccumulator >= 1f)
        {
            int wholePoints = Mathf.FloorToInt(pointAccumulator);
            pointAccumulator -= wholePoints;
            if (pointsManager != null)
                pointsManager.AddPoints(wholePoints);
        }

        UpdateHUD();
    }

    // =============================================
    // GAME FLOW
    // =============================================

    /// <summary>Called by START button InteractableUnityEventWrapper.</summary>
    public void StartLevel()
    {
        if (gameStarted) return;
        gameStarted = true;

        // Start all robots walking to random tile waypoints
        foreach (FriendlyAIScript robot in robots)
        {
            if (robot != null) robot.StartMoving();
        }

        // Start tile manager
        if (tileManager != null)
            tileManager.StartTiles();

        // Reset score
        if (pointsManager != null)
            pointsManager.ResetPoints();

        CountAliveRobots();
        UpdateHUD();
    }

    private void OnTimerEnd()
    {
        gameOver = true;
        StopEverything();

        // Timer ran out — player survived, show success
        if (tutorialUI != null)
            tutorialUI.ShowSuccess();
    }

    private void OnLose(string reason)
    {
        gameOver = true;
        StopEverything();
        Debug.Log($"Level 1 LOST: {reason}");

        if (tutorialUI != null)
            tutorialUI.ShowFail();
    }

    private void RestartSceneForPlayerFall()
    {
        gameOver = true;
        isPaused = false;
        Debug.Log("Level 1 LOST: player fell, restarting scene.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StopEverything()
    {
        // Stop all robots
        foreach (FriendlyAIScript robot in robots)
        {
            if (robot != null && robot.gameObject != null)
                robot.Stop();
        }

        // Stop tile manager
        if (tileManager != null)
            tileManager.StopTiles();
    }

    // =============================================
    // ROBOT TRACKING
    // =============================================

    private void CountAliveRobots()
    {
        int alive = 0;
        foreach (FriendlyAIScript robot in robots)
        {
            if (robot != null && robot.gameObject != null && robot.gameObject.activeInHierarchy)
                alive++;
        }

        robotsAlive = alive;
        robotsFallen = Mathf.Max(0, totalTrackedRobots - robotsAlive);
    }

    // =============================================
    // PAUSE (called by GameHUD)
    // =============================================

    public void SetPaused(bool paused)
    {
        isPaused = paused;

        if (tileManager != null)
            tileManager.SetPaused(paused);

        // Pause/unpause all robots
        foreach (FriendlyAIScript robot in robots)
        {
            if (robot != null && robot.gameObject != null)
                robot.SetPaused(paused);
        }
    }

    // =============================================
    // HUD
    // =============================================

    private void UpdateHUD()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
        }

    }
}
