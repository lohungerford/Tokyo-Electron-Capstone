using UnityEngine;
using TMPro;

public class DisplayText : MonoBehaviour
{
    public TextMeshProUGUI myText;

    void Start()
    {
        myText.text = "What PPE do you need?";
    }

    // Call this from anywhere to change the text
    public void ChangeText(string newText)
    {
        myText.text = newText;
    }
}