using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

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
        UpdateLabel();
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

    // helper functions (can't be called elsewhere)
    private void UpdateLabel()
    {
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
