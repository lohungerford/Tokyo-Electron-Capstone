using UnityEngine;
using TMPro;

public class DetectionZone : MonoBehaviour
{
    private GameObject messageObject;
    private bool mugInside = false;
    private float timer = 60f;
    private bool timerRunning = false;

    void Update()
    {
        if (timerRunning && mugInside)
        {
            timer -= Time.deltaTime;
            timer = Mathf.Max(timer, 0f);

            if (messageObject != null)
            {
                TextMeshPro tmp = messageObject.GetComponentInChildren<TextMeshPro>();
                int seconds = Mathf.CeilToInt(timer);
                tmp.text = "CORRECT!\n\n correct tool detected in zone\n\nTime remaining: " + seconds + "s";
            }

            if (timer <= 0f)
            {
                timerRunning = false;
                ShowMessage("TIME IS UP!\n\nGreat job!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered zone: " + other.gameObject.name + " tag: " + other.gameObject.tag);

        if (other.gameObject.name == "simpleGrabCupMesh" || other.gameObject.name == "simpleGrabTorchMesh" ||
            other.transform.root.CompareTag("Mug") || other.transform.root.CompareTag("Flashlight")) 
        {
            mugInside = true;
            timerRunning = true;
            timer = 60f;


            ShowMessage("CORRECT Item !\n\n  detected in zone\n\nTime remaining: 60s");
            Debug.Log("Mug detected!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "simpleGrabCupMesh" || other.gameObject.name == "simpleGrabTorchMesh" ||
            other.transform.root.CompareTag("Mug") || other.transform.root.CompareTag("Flashlight"))
        {
            mugInside = false;
            timerRunning = false;
            ShowMessage(" removed!\nPlace it back!");
            Invoke("HideMessage", 2f);
            Debug.Log("Mug removed!");
        }
    }
    void ShowMessage(string text)
    {
        if (messageObject == null)
        {
            messageObject = new GameObject("ZoneMessage");
            messageObject.transform.position = transform.position + new Vector3(0, 1f, 0);
            messageObject.transform.LookAt(Camera.main.transform);
            messageObject.transform.Rotate(0, 180, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(messageObject.transform, false);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 0.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        TextMeshPro t = messageObject.GetComponentInChildren<TextMeshPro>();
        t.text = text;
    }

    void HideMessage()
    {
        if (messageObject != null)
        {
            Destroy(messageObject);
            messageObject = null;
        }
    }
}