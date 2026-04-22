using UnityEngine;
using TMPro;

public class DetectionZone : MonoBehaviour
{
    private GameObject messageObject;

    private bool itemInside = false;
    private float timer = 1000f;
    private bool timerRunning = false;

    private void Update()
    {
        if (!timerRunning || !itemInside) return;

        timer -= Time.deltaTime;
        timer = Mathf.Max(timer, 0f);

        UpdateText($"CORRECT!\n\nItem detected\n\nTime remaining: {Mathf.CeilToInt(timer)}s");

        if (timer <= 0f)
        {
            timerRunning = false;
            ShowMessage("TIME IS UP!\n\nGreat job!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string detected = GetDetectedItemName(other);

        if (detected == null) return;

        itemInside = true;
        timerRunning = true;
        timer = 60f;

        ShowMessage($"{detected} detected in zone!\n\nTime remaining: 1000s");
    }

    private void OnTriggerExit(Collider other)
    {
        string detected = GetDetectedItemName(other);

        if (detected == null) return;

        itemInside = false;
        timerRunning = false;

        ShowMessage($"{detected} removed!\nPlace it back!");

        Invoke(nameof(HideMessage), 2f);
    }

    // ---------------- CLEAN DETECTION LOGIC ----------------
    private string GetDetectedItemName(Collider other)
    {
        GameObject obj = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        string name = obj.name;
        string tag = obj.tag;

        if (tag == "Mug" || name.Contains("Cup"))
            return "Mug";

        if (tag == "Flashlight" || name.Contains("Torch"))
            return "Flashlight";

        if (tag == "Helmet")
            return "Safety Helmet";

        if (tag == "Glasses")
            return "Safety Glasses";

        if (tag == "Boots")
            return "Safety Boots";

        if (tag == "Gloves")
            return "Safety Gloves";

        return null;
    }

    // ---------------- UI ----------------
    private void ShowMessage(string text)
    {
        if (messageObject == null)
        {
            messageObject = new GameObject("ZoneMessage");
            messageObject.transform.position = transform.position + Vector3.up * 1f;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(messageObject.transform, false);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 0.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        UpdateText(text);
    }

    private void UpdateText(string text)
    {
        if (messageObject == null) return;

        TextMeshPro tmp = messageObject.GetComponentInChildren<TextMeshPro>();
        if (tmp != null)
            tmp.text = text;
    }

    private void HideMessage()
    {
        if (messageObject != null)
        {
            Destroy(messageObject);
            messageObject = null;
        }
    }
}