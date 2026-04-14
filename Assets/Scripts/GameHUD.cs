using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction.Samples;

public class GameHUD : MonoBehaviour
{
    [Header("Pause Menu")]
    [Tooltip("Optional panel to show when paused (can be left empty)")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Scene Transition")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private SceneLoader sceneLoader;
    private bool isPaused = false;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();
    }

    private void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    // -----------------------------------------------
    // Called by Pause_PokeInteractable When Select()
    // -----------------------------------------------
    public void OnPausePressed()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;

        // Pause all animators except those under the Camera Rig (hands/head tracking).
        // We cannot use Time.timeScale = 0 in Meta VR — it freezes OVR pose tracking.
        foreach (Animator anim in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (IsUnderCameraRig(anim.transform)) continue;
            anim.speed = 0f;
        }

        // Stop all NavMesh agents (bots, etc.)
        foreach (UnityEngine.AI.NavMeshAgent agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
            agent.isStopped = true;

        SetScenePaused(true);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;

        foreach (Animator anim in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (IsUnderCameraRig(anim.transform)) continue;
            anim.speed = 1f;
        }

        foreach (UnityEngine.AI.NavMeshAgent agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
            agent.isStopped = false;

        SetScenePaused(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    // -----------------------------------------------
    // Called by Leave_PokeInteractable When Select()
    // -----------------------------------------------
    public void OnLeavePressed()
    {
        isPaused = false;
        sceneLoader.Load(mainMenuSceneName);
    }

    // -----------------------------------------------
    // Helpers
    // -----------------------------------------------

    /// <summary>
    /// Returns true if the given transform is a descendant of the OVR Camera Rig.
    /// Uses the OVRCameraRig component to find it reliably regardless of object name.
    /// </summary>
    private bool IsUnderCameraRig(Transform t)
    {
        OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig == null) return false;
        return t.IsChildOf(rig.transform);
    }

    private void SetScenePaused(bool paused)
    {
        foreach (DisappearMechanic tile in FindObjectsByType<DisappearMechanic>(FindObjectsSortMode.None))
        {
            tile.SetPaused(paused);
        }
    }
}
