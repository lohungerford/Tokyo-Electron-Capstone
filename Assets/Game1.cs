using UnityEngine;
using TMPro;

public class Game1 : MonoBehaviour
{
    private GameObject questionObject;

    // This gets called by the Meta SDK button event
    public void OnButtonPressed()
    {
        Debug.Log("Button 2 pressed!");

        if (questionObject != null)
            Destroy(questionObject);

        questionObject = new GameObject("QuestionText");
        questionObject.transform.position = transform.position + new Vector3(0, 0.5f, 0);
        questionObject.transform.LookAt(Camera.main.transform);
        questionObject.transform.Rotate(0, 180, 0);

        TextMeshPro tmp = questionObject.AddComponent<TextMeshPro>();
        tmp.text = "The FAB lights shut off, what tool do you need (Flashlight durr)?";
        tmp.fontSize = 0.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        Debug.Log("Question shown!");
    }
}