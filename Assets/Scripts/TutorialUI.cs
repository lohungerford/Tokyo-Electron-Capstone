using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction.Samples;

public class TutorialUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failPanel;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName;

    private SceneLoader sceneLoader;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();
    }

    private void Start()
    {
        // instruction panel visible at start, others hidden
        ShowPanel(instructionPanel);
        HidePanel(successPanel);
        HidePanel(failPanel);
    }

    // -----------------------------------------------
    // Called by START button InteractableUnityEventWrapper
    // -----------------------------------------------
    public void OnStartPressed()
    {
        HidePanel(instructionPanel);
        // TutorialManager will listen for this and start the robot
    }

    // -----------------------------------------------
    // Called by TutorialManager when robot is blocked
    // -----------------------------------------------
    public void ShowSuccess()
    {
        HidePanel(instructionPanel);
        HidePanel(failPanel);
        ShowPanel(successPanel);
    }

    // -----------------------------------------------
    // Called by TutorialManager when robot falls
    // -----------------------------------------------
    public void ShowFail()
    {
        HidePanel(instructionPanel);
        HidePanel(successPanel);
        ShowPanel(failPanel);
    }

    // -----------------------------------------------
    // Called by CONTINUE button on success panel
    // -----------------------------------------------
    public void OnContinuePressed()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            sceneLoader.Load(nextSceneName);
        else
            Debug.LogWarning("TutorialUI: No next scene name set!");
    }

    // -----------------------------------------------
    // Called by RETRY button on fail panel
    // -----------------------------------------------
    public void OnRetryPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // -----------------------------------------------
    // Helpers
    // -----------------------------------------------
    private void ShowPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
    }
}
