using UnityEngine;
using TMPro;

public class Game1 : MonoBehaviour
{
    private GameObject questionObject;
    private int currentQuestion = 0;

    private string[] questions = new string[]
    {
        "SCENARIO 1\n\nYou’re entering a dark room what should you use?",
        "SCENARIO 2\n\nYou’re entering a clean room what should you wear to prevent contamination?",
        "SCENARIO 3\n\nYou’re entering a toxic breathing environment what should you wear to protect yourself?",
        "SCENARIO 4\n\nYou are entering a zone with\nfalling debris overhead.\nWhat protective gear is required?",
        "SCENARIO 5\n\nYou are working near moving equipment and particles. What protects your eyes?",
        "SCENARIO 6\n\nThe clean room floor tiles are open. What protects your feet from contamination?",
        "SCENARIO 7\n\nYou are handling sensitive wafers. What protects them from oils on your hands?"
    };

    public void OnButtonPressed()
    {
        ShowQuestion(currentQuestion);
        currentQuestion = (currentQuestion + 1) % questions.Length;
    }

    public void CheckTag(string tagName)
    {
        Debug.Log("Entered tag: " + tagName);

        switch (tagName)
        {
            case "Flashlight":
                Debug.Log("Correct for Scenario 1: Flashlight");
                break;

            case "Suit":
            case "BunnySuit":
                Debug.Log("Correct for Scenario 2: Suit / Bunny Suit");
                break;

            case "Mask":
                Debug.Log("Correct for Scenario 3: Mask");
                break;

            case "Helmet":
                Debug.Log("Correct for Scenario 4: Helmet");
                break;

            case "Glasses":
                Debug.Log("Correct for Scenario 5: Glasses");
                break;

            case "Boots":
                Debug.Log("Correct for Scenario 6: Boots");
                break;

            case "Gloves":
                Debug.Log("Correct for Scenario 7: Gloves");
                break;

            default:
                Debug.Log("No matching tag found.");
                break;
        }
    }

    private void ShowQuestion(int index)
    {
        if (questionObject != null)
            Destroy(questionObject);

        questionObject = new GameObject("QuestionDisplay");
        questionObject.transform.position = transform.position + new Vector3(0, 1.2f, 0);
        questionObject.transform.LookAt(Camera.main.transform);
        questionObject.transform.Rotate(0, 180, 0);

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(questionObject.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 0.01f);
        bg.transform.localScale = new Vector3(2.6f, 1.4f, 1f);
        Destroy(bg.GetComponent<Collider>());
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.05f, 0.08f, 0.2f, 1f);
        bg.GetComponent<Renderer>().material = mat;

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bar.transform.SetParent(questionObject.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.58f, 0f);
        bar.transform.localScale = new Vector3(2.6f, 0.12f, 1f);
        Destroy(bar.GetComponent<Collider>());
        Material barMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        barMat.color = new Color(1f, 0.6f, 0f, 1f);
        bar.GetComponent<Renderer>().material = barMat;

        GameObject counterObj = new GameObject("Counter");
        counterObj.transform.SetParent(questionObject.transform, false);
        counterObj.transform.localPosition = new Vector3(0, 0.52f, 0f);
        TextMeshPro counter = counterObj.AddComponent<TextMeshPro>();
        counter.text = "QUESTION " + (index + 1) + " OF " + questions.Length;
        counter.fontSize = 0.18f;
        counter.alignment = TextAlignmentOptions.Center;
        counter.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        counter.fontStyle = FontStyles.Bold;
        RectTransform counterRect = counterObj.GetComponent<RectTransform>();
        counterRect.sizeDelta = new Vector2(2.4f, 0.2f);

        GameObject textObj = new GameObject("QuestionText");
        textObj.transform.SetParent(questionObject.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0f, 0f);
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = questions[index];
        tmp.fontSize = 0.28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.3f, 1.1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}