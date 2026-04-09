using UnityEngine;
using TMPro;

public class Game1 : MonoBehaviour
{
    private GameObject questionObject;
    private int currentQuestion = 0;

    private string[] questions = new string[]
    {
        "SCENARIO 1\n\nThe FAB lights have shut off completely.\nWhich PPE tool do you need?",
        "SCENARIO 2\n\nYou are entering a zone with\nfalling debris overhead.\nWhat protective gear is required?",
        "SCENARIO 3\n\nYou are working with exposed\nhigh voltage electrical wiring.\nWhat PPE must you wear?",
        "SCENARIO 4\n\nA chemical spill has occurred\non the FAB floor.\nWhat protection do you need?",
        "SCENARIO 5\n\nYou are operating loud machinery\nfor an extended period.\nWhat gear protects you?"
    };

    public void OnButtonPressed()
    {
        Debug.Log("Button pressed! Showing question " + (currentQuestion + 1));

        if (questionObject != null)
            Destroy(questionObject);

        // Root object
        questionObject = new GameObject("QuestionDisplay");
        questionObject.transform.position = transform.position + new Vector3(0, 1.2f, 0);
        questionObject.transform.LookAt(Camera.main.transform);
        questionObject.transform.Rotate(0, 180, 0);

        // Background panel
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(questionObject.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.01f);
        bg.transform.localScale = new Vector3(2.6f, 1.4f, 1f);
        Destroy(bg.GetComponent<Collider>());
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.05f, 0.08f, 0.2f, 1f);
        bg.GetComponent<Renderer>().material = mat;

        // Top accent bar
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bar.transform.SetParent(questionObject.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.58f, 0f);
        bar.transform.localScale = new Vector3(2.6f, 0.12f, 1f);
        Destroy(bar.GetComponent<Collider>());
        Material barMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        barMat.color = new Color(1f, 0.6f, 0f, 1f);
        bar.GetComponent<Renderer>().material = barMat;

        // Question counter text
        GameObject counterObj = new GameObject("Counter");
        counterObj.transform.SetParent(questionObject.transform, false);
        counterObj.transform.localPosition = new Vector3(0, 0.52f, 0f);
        TextMeshPro counter = counterObj.AddComponent<TextMeshPro>();
        counter.text = "QUESTION " + (currentQuestion + 1) + " OF " + questions.Length;
        counter.fontSize = 0.18f;
        counter.alignment = TextAlignmentOptions.Center;
        counter.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        counter.fontStyle = FontStyles.Bold;
        RectTransform counterRect = counterObj.GetComponent<RectTransform>();
        counterRect.sizeDelta = new Vector2(2.4f, 0.2f);

        // Main question text
        GameObject textObj = new GameObject("QuestionText");
        textObj.transform.SetParent(questionObject.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0f, 0f);
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = questions[currentQuestion];
        tmp.fontSize = 0.28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.3f, 1.1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Advance to next question
        currentQuestion = (currentQuestion + 1) % questions.Length;

        Debug.Log("Question shown!");
    }
}