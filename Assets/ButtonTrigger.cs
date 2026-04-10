
using UnityEngine;
using TMPro;

public class ButtonTrigger : MonoBehaviour
{
    private GameObject questionObject;
    private int currentQuestion = 0;

    // TIMER
    private TextMeshPro timerText;
    private float timeRemaining = 60f;
    private bool isTimerRunning = false;

    private string[] questions = new string[]
    {
        "SCENARIO 1\n\nThe FAB lights have shut off completely.\nWhich PPE tool do you need?",
        "SCENARIO 2\n\nYou are entering a zone with\nfalling debris overhead.\nWhat protective gear is required?",
        "SCENARIO 3\n\nYou are working with exposed\nhigh voltage electrical wiring.\nWhat PPE must you wear?",
        "SCENARIO 4\n\nA chemical spill has occurred\non the FAB floor.\nWhat protection do you need?",
        "SCENARIO 5\n\nYou are operating loud machinery\nfor an extended period.\nWhat gear protects you?"
    };

    void Update()
    {
        // Keep UI facing player
        if (questionObject != null && Camera.main != null)
        {
            questionObject.transform.LookAt(Camera.main.transform);
            questionObject.transform.Rotate(0, 180, 0);
        }

        // TIMER LOGIC
        if (isTimerRunning && timerText != null)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;

                int displayTime = Mathf.CeilToInt(timeRemaining);
                timerText.text = "TIME: " + displayTime;

                // TURN RED WHEN LOW
                if (displayTime <= 10)
                    timerText.color = Color.red;
                else
                    timerText.color = Color.white;
            }
            else
            {
                timeRemaining = 0;
                timerText.text = "TIME: 0";
                timerText.color = Color.red;
                isTimerRunning = false;
            }
        }
    }

    public void OnButtonPressed()
    {
        Debug.Log("Button pressed! Showing question " + (currentQuestion + 1));

        if (questionObject != null)
            Destroy(questionObject);

        // Spawn in front of player
        Transform cam = Camera.main.transform;

        questionObject = new GameObject("QuestionDisplay");
        questionObject.transform.position = cam.position + cam.forward * 2f + Vector3.up * 0.2f;
        questionObject.transform.LookAt(cam);
        questionObject.transform.Rotate(0, 180, 0);

        // BACKGROUND
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(questionObject.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.01f);
        bg.transform.localScale = new Vector3(2.6f, 1.4f, 1f);
        Destroy(bg.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.05f, 0.08f, 0.2f, 1f);
        bg.GetComponent<Renderer>().material = mat;

        // TOP BAR
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bar.transform.SetParent(questionObject.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.58f, 0f);
        bar.transform.localScale = new Vector3(2.6f, 0.12f, 1f);
        Destroy(bar.GetComponent<Collider>());

        Material barMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        barMat.color = new Color(1f, 0.6f, 0f, 1f);
        bar.GetComponent<Renderer>().material = barMat;

        // COUNTER
        GameObject counterObj = new GameObject("Counter");
        counterObj.transform.SetParent(questionObject.transform, false);
        counterObj.transform.localPosition = new Vector3(0, 0.52f, 0f);

        TextMeshPro counter = counterObj.AddComponent<TextMeshPro>();
        counter.text = "QUESTION " + (currentQuestion + 1) + " OF " + questions.Length;
        counter.fontSize = 5;
        counter.alignment = TextAlignmentOptions.Center;
        counter.color = Color.black;
        counter.fontStyle = FontStyles.Bold;
        counter.transform.localScale = Vector3.one * 0.01f;

        // QUESTION TEXT
        GameObject textObj = new GameObject("QuestionText");
        textObj.transform.SetParent(questionObject.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0f, 0f);

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = questions[currentQuestion];
        tmp.fontSize = 7;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.transform.localScale = Vector3.one * 0.01f;

        // TIMER TEXT
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(questionObject.transform, false);
        timerObj.transform.localPosition = new Vector3(0, -0.6f, 0f);

        timerText = timerObj.AddComponent<TextMeshPro>();
        timerText.text = "TIME: 60";
        timerText.fontSize = 5;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;
        timerText.transform.localScale = Vector3.one * 0.01f;

        // START TIMER
        timeRemaining = 60f;
        isTimerRunning = true;

        // NEXT QUESTION
        currentQuestion = (currentQuestion + 1) % questions.Length;

        Debug.Log("Question shown!");
    }
}

