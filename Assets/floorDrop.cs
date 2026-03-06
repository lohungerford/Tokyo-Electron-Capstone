using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomTileDrop : MonoBehaviour
{
    [Header("All floor tiles - assign in Inspector")]
    public List<GameObject> FloorTiles = new List<GameObject>();

    [Header("How far away the random tile must be from this button")]
    public float MinDistance = 3f;

    [Header("Drop Settings")]
    public float DropDistance = 5f;
    public float DropSpeed = 8f;

    private bool buttonPressed = false;

    void Update()
    {
        // Press SPACE to test without VR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed - testing drop");
            buttonPressed = false;
            DropRandomTile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger hit by: " + other.gameObject.name + " tag: " + other.gameObject.tag);

        if (buttonPressed) return;

        if (other.CompareTag("LeftController") || other.CompareTag("RightController"))
        {
            Debug.Log("Controller detected - dropping tile!");
            buttonPressed = true;
            DropRandomTile();
        }
    }

    public void DropRandomTile()
    {
        if (FloorTiles.Count == 0)
        {
            Debug.LogWarning("No floor tiles assigned!");
            return;
        }

        List<GameObject> validTiles = new List<GameObject>();
        foreach (GameObject tile in FloorTiles)
        {
            if (tile == null) continue;
            float dist = Vector3.Distance(transform.position, tile.transform.position);
            Debug.Log("Tile: " + tile.name + " distance: " + dist);
            if (dist >= MinDistance)
                validTiles.Add(tile);
        }

        if (validTiles.Count == 0)
        {
            Debug.LogWarning("No valid tiles far enough away! Try lowering MinDistance.");
            return;
        }

        int randomIndex = Random.Range(0, validTiles.Count);
        GameObject chosenTile = validTiles[randomIndex];
        Debug.Log("Dropping tile: " + chosenTile.name);

        StartCoroutine(AnimateDrop(chosenTile));
    }

    IEnumerator AnimateDrop(GameObject tile)
    {
        Vector3 startPos = tile.transform.position;
        Vector3 targetPos = startPos - new Vector3(0, DropDistance, 0);

        // Small shake before dropping
        float shakeTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            float shakeX = Random.Range(-0.05f, 0.05f);
            float shakeZ = Random.Range(-0.05f, 0.05f);
            tile.transform.position = startPos + new Vector3(shakeX, 0, shakeZ);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tile.transform.position = startPos;

        // Drop straight down
        while (Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
        {
            tile.transform.position = Vector3.MoveTowards(
                tile.transform.position, targetPos, DropSpeed * Time.deltaTime);
            yield return null;
        }

        tile.transform.position = targetPos;
        Debug.Log("Tile drop complete!");
    }
}