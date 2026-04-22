#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class LevelTwoAutoSetup
{
    private const string LevelTwoSceneName = "LevelTwo";
    private const string LevelManagerObjectName = "Level1Manager";
    private const string TileManagerObjectName = "LevelTileManager";
    private const float DefaultEnemyChaseRange = 10f;
    private const float DefaultEnemyAttackRange = 1.5f;
    private const float TargetEnemySpeed = 0.6f;

    [MenuItem("Tools/Level Setup/Auto-Wire LevelTwo")]
    public static void AutoWireLevelTwo()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        FriendlyAIScript[] robots = FindSceneObjects<FriendlyAIScript>(activeScene)
            .Where(robot => robot.useBounceMovement && robot.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(robot => robot.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (robots.Length == 0)
        {
            EditorUtility.DisplayDialog("LevelTwo Setup", "No GoodRob robots with FriendlyAIScript were found.", "OK");
            return;
        }

        List<GameObject> floorTiles = FindSceneFloorTiles(activeScene);
        if (floorTiles.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "LevelTwo Setup",
                "No floor tiles named 'Floor (...)' were found. Rename or add the floor tiles first.",
                "OK");
            return;
        }

        List<DisappearMechanic> disappearTiles = new List<DisappearMechanic>(floorTiles.Count);
        foreach (GameObject floorTile in floorTiles)
        {
            BoxCollider collider = floorTile.GetComponent<BoxCollider>();
            if (collider == null)
                collider = Undo.AddComponent<BoxCollider>(floorTile);

            ConfigureColliderFromMesh(floorTile, collider);
            ConfigureNavMeshObstacle(floorTile, collider);

            DisappearMechanic disappearMechanic = floorTile.GetComponent<DisappearMechanic>();
            if (disappearMechanic == null)
                disappearMechanic = Undo.AddComponent<DisappearMechanic>(floorTile);

            disappearTiles.Add(disappearMechanic);
        }

        LevelTileManager tileManager = FindOrCreateSceneComponent<LevelTileManager>(activeScene, TileManagerObjectName);
        Undo.RecordObject(tileManager, "Wire LevelTwo Tile Manager");
        tileManager.allTiles = disappearTiles.ToArray();
        tileManager.timeBetweenBreaks = 10f;
        tileManager.warningDuration = 5f;
        tileManager.brokenDuration = 12f;
        tileManager.maxActiveTiles = 3;
        EditorUtility.SetDirty(tileManager);

        Level1Manager levelManager = FindOrCreateSceneComponent<Level1Manager>(activeScene, LevelManagerObjectName);
        TutorialUI tutorialUI = FindSceneObjects<TutorialUI>(activeScene).FirstOrDefault();
        PointsManager pointsManager = FindSceneObjects<PointsManager>(activeScene).FirstOrDefault();
        Transform playerTransform = FindPlayerTransform(activeScene);
        TextMeshProUGUI timerText = FindTimerText(activeScene);

        SerializedObject levelManagerSerialized = new SerializedObject(levelManager);
        levelManagerSerialized.FindProperty("tileManager").objectReferenceValue = tileManager;
        levelManagerSerialized.FindProperty("pointsManager").objectReferenceValue = pointsManager;
        levelManagerSerialized.FindProperty("tutorialUI").objectReferenceValue = tutorialUI;
        levelManagerSerialized.FindProperty("timerText").objectReferenceValue = timerText;
        levelManagerSerialized.FindProperty("levelDuration").floatValue = 90f;
        levelManagerSerialized.FindProperty("pointsPerRobotPerSecond").intValue = 1;
        levelManagerSerialized.FindProperty("maxRobotDeaths").intValue = Mathf.Max(1, robots.Length - 1);
        levelManagerSerialized.FindProperty("playerFallY").floatValue = -5f;
        levelManagerSerialized.FindProperty("playerTransform").objectReferenceValue = playerTransform;

        SerializedProperty robotsProperty = levelManagerSerialized.FindProperty("robots");
        robotsProperty.arraySize = robots.Length;
        for (int i = 0; i < robots.Length; i++)
            robotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = robots[i];

        levelManagerSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(levelManager);

        if (tutorialUI != null)
        {
            SerializedObject tutorialUiSerialized = new SerializedObject(tutorialUI);
            tutorialUiSerialized.FindProperty("nextSceneName").stringValue = "LevelThree";
            tutorialUiSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorialUI);
        }

        TextMeshProUGUI levelTitle = FindSceneObjects<TextMeshProUGUI>(activeScene)
            .FirstOrDefault(text => string.Equals(text.text, "Level 1 - Floor Patrol", StringComparison.Ordinal));
        if (levelTitle != null)
        {
            levelTitle.text = "Level 2 - Floor Patrol";
            EditorUtility.SetDirty(levelTitle);
        }

        foreach (FriendlyAIScript robot in robots)
        {
            SerializedObject robotSerialized = new SerializedObject(robot);
            robotSerialized.FindProperty("useBounceMovement").boolValue = true;
            robotSerialized.FindProperty("startMovingOnAwake").boolValue = false;
            robotSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(robot);
        }

        AutoWireEnemyInternal(activeScene, playerTransform);
        BuildNavMeshSurfaces(activeScene);
        EditorSceneManager.MarkSceneDirty(activeScene);

        string timerMessage = timerText != null ? timerText.name : "none found";
        string pointsMessage = pointsManager != null ? pointsManager.name : "none found";
        string tutorialMessage = tutorialUI != null ? tutorialUI.name : "none found";
        string playerMessage = playerTransform != null ? playerTransform.name : "none found";

        EditorUtility.DisplayDialog(
            "LevelTwo Setup Complete",
            $"Wired {robots.Length} robots and {disappearTiles.Count} tiles.\n" +
            $"Fail threshold set to {Mathf.Max(1, robots.Length - 1)} robot losses.\n" +
            $"TutorialUI: {tutorialMessage}\n" +
            $"PointsManager: {pointsMessage}\n" +
            $"Timer text: {timerMessage}\n" +
            $"Player transform: {playerMessage}\n\n" +
            "Enemy chase settings were applied and NavMeshSurfaces were rebuilt.\n" +
            "Save the scene, then test START in Play Mode.",
            "OK");
    }

    [MenuItem("Tools/Level Setup/Setup Level 2 UI Content")]
    public static void SetupLevelTwoUIContent()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();
        int textPatches = 0;

        // -------------------------------------------------------
        // 1. Fix TutorialUI — nextSceneName and verify retry
        // -------------------------------------------------------
        TutorialUI tutorialUI = FindSceneObjects<TutorialUI>(activeScene).FirstOrDefault();
        if (tutorialUI != null)
        {
            SerializedObject tuSO = new SerializedObject(tutorialUI);
            tuSO.FindProperty("nextSceneName").stringValue = "LevelThree";
            tuSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorialUI);
            log.AppendLine("✓ TutorialUI: nextSceneName set to 'LevelThree'.");
            log.AppendLine("⚠ MANUAL CHECK: Open the fail panel and confirm its RETRY button");
            log.AppendLine("  is wired to TutorialUI → OnRetryPressed() (not OnContinuePressed).");
        }
        else
        {
            log.AppendLine("⚠ TutorialUI not found in scene.");
        }

        // -------------------------------------------------------
        // 2. Replace any Level 1 text references in the scene's UI panels
        // -------------------------------------------------------
        var replacements = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Level 1 - Floor Patrol",    "Level 2 - Chemical Environment" },
            { "Level 1",                   "Level 2" },
            { "Level One",                 "Level Two" },
            { "LevelOne",                  "LevelTwo" },
            // Instruction-panel body copy
            { "Place barriers to protect friendly robots from falling through broken floor tiles.",
              "Protect friendly robots from toxic bots and broken floor tiles.\nUse generators to shut down toxic robots.\nYou have 10 HP — don't let them hit you." },
        };

        foreach (TMPro.TextMeshProUGUI tmp in FindSceneObjects<TMPro.TextMeshProUGUI>(activeScene))
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text)) continue;

            string original = tmp.text;
            string updated = original;

            foreach (var kv in replacements)
                updated = updated.Replace(kv.Key, kv.Value);

            if (!string.Equals(original, updated, StringComparison.Ordinal))
            {
                Undo.RecordObject(tmp, "Patch Level 2 UI Text");
                tmp.text = updated;
                EditorUtility.SetDirty(tmp);
                log.AppendLine($"✓ Updated text on '{tmp.gameObject.name}': \"{Truncate(original, 40)}\" → \"{Truncate(updated, 40)}\"");
                textPatches++;
            }
        }

        if (textPatches == 0)
            log.AppendLine("— No Level 1 text references found to patch (may already be updated).");

        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Level 2 UI Content — Complete",
            log.ToString() + "\nSave the scene when done.",
            "OK");
    }

    [MenuItem("Tools/Level Setup/Auto-Wire Player Health")]
    public static void AutoWirePlayerHealth()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();

        // -------------------------------------------------------
        // 1. PlayerHealth on the Player-tagged object
        // -------------------------------------------------------
        PlayerHealth playerHealth = FindSceneObjects<PlayerHealth>(activeScene).FirstOrDefault();
        GameObject playerObject = null;

        if (playerHealth == null)
        {
            // Search for the Player-tagged object.
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag("Player"))
                    {
                        playerObject = t.gameObject;
                        break;
                    }
                }
                if (playerObject != null) break;
            }

            if (playerObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Auto-Wire Player Health",
                    "Could not find a GameObject tagged 'Player'.\n" +
                    "Tag your Camera Rig root (or the player pivot) as 'Player' and run this tool again.",
                    "OK");
                return;
            }

            playerHealth = Undo.AddComponent<PlayerHealth>(playerObject);
            log.AppendLine($"✓ Added PlayerHealth to '{playerObject.name}'.");
        }
        else
        {
            playerObject = playerHealth.gameObject;
            log.AppendLine($"✓ Found existing PlayerHealth on '{playerObject.name}'.");
        }

        EditorUtility.SetDirty(playerHealth);

        // -------------------------------------------------------
        // 2. Wire PlayerHealth into Level1Manager
        // -------------------------------------------------------
        Level1Manager levelManager = FindSceneObjects<Level1Manager>(activeScene).FirstOrDefault();
        if (levelManager != null)
        {
            SerializedObject lmSO = new SerializedObject(levelManager);
            lmSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            lmSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelManager);
            log.AppendLine($"✓ Wired PlayerHealth into Level1Manager.");
        }
        else
        {
            log.AppendLine("⚠ Level1Manager not found — run Auto-Wire LevelTwo first.");
        }

        // -------------------------------------------------------
        // 3. Hit flash canvas parented to CenterEyeAnchor
        // -------------------------------------------------------
        HitFlashEffect existingFlash = FindSceneObjects<HitFlashEffect>(activeScene).FirstOrDefault();
        if (existingFlash != null)
        {
            // Already exists — just make sure PlayerHealth is wired.
            SerializedObject flashSO = new SerializedObject(existingFlash);
            flashSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            flashSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existingFlash);
            log.AppendLine($"✓ Found existing HitFlashEffect — re-wired PlayerHealth.");
        }
        else
        {
            // Find CenterEyeAnchor via OVRCameraRig.
            OVRCameraRig ovrRig = FindSceneObjects<OVRCameraRig>(activeScene).FirstOrDefault();
            Transform eyeAnchor = ovrRig != null ? ovrRig.centerEyeAnchor : null;

            if (eyeAnchor == null)
            {
                // Fallback: search by name.
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (string.Equals(t.name, "CenterEyeAnchor", StringComparison.OrdinalIgnoreCase))
                        {
                            eyeAnchor = t;
                            break;
                        }
                    }
                    if (eyeAnchor != null) break;
                }
            }

            if (eyeAnchor == null)
            {
                log.AppendLine("⚠ CenterEyeAnchor not found — HitFlashEffect canvas was NOT created.\n" +
                               "  Create a World Space Canvas under CenterEyeAnchor manually (see script comments).");
            }
            else
            {
                // Create the flash canvas as a child of CenterEyeAnchor.
                GameObject canvasGO = new GameObject("HitFlashCanvas");
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create HitFlashCanvas");
                canvasGO.transform.SetParent(eyeAnchor, false);

                Canvas canvas = Undo.AddComponent<Canvas>(canvasGO);
                canvas.renderMode = RenderMode.WorldSpace;

                // Position just past the near clip plane so it always overlays the scene.
                RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
                canvasRT.localPosition = new Vector3(0f, 0f, 0.31f);
                canvasRT.localRotation = Quaternion.identity;
                canvasRT.localScale = Vector3.one;
                canvasRT.sizeDelta = new Vector2(0.6f, 0.34f); // ~fills view at 0.31 m

                // Fullscreen red Image child.
                GameObject imageGO = new GameObject("FlashImage");
                Undo.RegisterCreatedObjectUndo(imageGO, "Create FlashImage");
                imageGO.transform.SetParent(canvasGO.transform, false);

                UnityEngine.UI.Image flashImage = Undo.AddComponent<UnityEngine.UI.Image>(imageGO);
                flashImage.color = new Color(1f, 0f, 0f, 0f); // red, alpha=0 at start

                // Stretch the image to fill the canvas.
                RectTransform imageRT = imageGO.GetComponent<RectTransform>();
                imageRT.anchorMin = Vector2.zero;
                imageRT.anchorMax = Vector2.one;
                imageRT.offsetMin = Vector2.zero;
                imageRT.offsetMax = Vector2.zero;

                // Add HitFlashEffect and wire it.
                HitFlashEffect flashEffect = Undo.AddComponent<HitFlashEffect>(canvasGO);
                SerializedObject flashSO = new SerializedObject(flashEffect);
                flashSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
                flashSO.FindProperty("flashImage").objectReferenceValue = flashImage;
                flashSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(flashEffect);

                log.AppendLine($"✓ Created HitFlashCanvas under '{eyeAnchor.name}' and wired HitFlashEffect.");
                log.AppendLine("  Adjust canvas Width/Height in Inspector if the flash doesn't fill the view.");
            }
        }

        // -------------------------------------------------------
        // 4. PlayerHealthUI — find the health Slider and optional label.
        //    Explicitly skip anything inside a PPE-named hierarchy to avoid
        //    wiring to the wrong UI panel.
        // -------------------------------------------------------
        PlayerHealthUI existingUI = FindSceneObjects<PlayerHealthUI>(activeScene).FirstOrDefault();
        if (existingUI != null)
        {
            SerializedObject uiSO = new SerializedObject(existingUI);
            uiSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            uiSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existingUI);
            log.AppendLine("✓ Found existing PlayerHealthUI — re-wired PlayerHealth.");
        }
        else
        {
            // Find a Slider named "Health*" or "*HP*" that is NOT under a PPE panel.
            UnityEngine.UI.Slider healthSlider = FindSceneObjects<UnityEngine.UI.Slider>(activeScene)
                .FirstOrDefault(s =>
                    !IsUnderPPEHierarchy(s.transform) &&
                    (s.name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     s.name.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0));

            // Find a TMP label named "Health*" or "*HP*" that is NOT under a PPE panel.
            TMPro.TextMeshProUGUI healthLabel = FindSceneObjects<TMPro.TextMeshProUGUI>(activeScene)
                .FirstOrDefault(t =>
                    !IsUnderPPEHierarchy(t.transform) &&
                    (t.name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     t.name.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0));

            if (healthSlider != null || healthLabel != null)
            {
                // Attach PlayerHealthUI to the Slider's GameObject if found, else the label's.
                GameObject uiTarget = healthSlider != null
                    ? healthSlider.gameObject
                    : healthLabel.gameObject;

                PlayerHealthUI ui = Undo.AddComponent<PlayerHealthUI>(uiTarget);
                SerializedObject uiSO = new SerializedObject(ui);
                uiSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
                if (healthSlider != null)
                    uiSO.FindProperty("healthSlider").objectReferenceValue = healthSlider;
                if (healthLabel != null)
                    uiSO.FindProperty("healthLabel").objectReferenceValue = healthLabel;
                uiSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ui);

                string sliderMsg = healthSlider != null ? $"Slider '{healthSlider.name}'" : "no Slider";
                string labelMsg  = healthLabel  != null ? $"Label '{healthLabel.name}'"   : "no Label";
                log.AppendLine($"✓ Added PlayerHealthUI — {sliderMsg}, {labelMsg}.");
            }
            else
            {
                log.AppendLine("⚠ No health Slider or TMP label found (names must contain 'health' or 'hp',");
                log.AppendLine("  and must not be inside a PPE panel).");
                log.AppendLine("  Name your hand UI Slider 'HealthSlider' and re-run this tool.");
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Auto-Wire Player Health — Complete",
            log.ToString() + "\nSave the scene when done.",
            "OK");
    }

    [MenuItem("Tools/Level Setup/Auto-Wire Generator Shutdown")]
    public static void AutoWireGeneratorShutdown()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        // Find all enemy robots (RobotAI components NOT named GoodRob*)
        RobotAI[] allRobotAIs = FindSceneObjects<RobotAI>(activeScene).ToArray();
        RobotAI[] enemyRobots = allRobotAIs
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (enemyRobots.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Generator Shutdown Setup",
                "No enemy RobotAI found in the scene (looking for any RobotAI not named 'GoodRob...').\n" +
                "Add your toxic robot(s) to the scene first.",
                "OK");
            return;
        }

        // Find or create the controller GameObject
        GeneratorShutdownController controller = FindSceneObjects<GeneratorShutdownController>(activeScene).FirstOrDefault();
        if (controller == null)
        {
            GameObject go = new GameObject("GeneratorShutdownController");
            SceneManager.MoveGameObjectToScene(go, activeScene);
            Undo.RegisterCreatedObjectUndo(go, "Create GeneratorShutdownController");
            controller = Undo.AddComponent<GeneratorShutdownController>(go);
        }

        // Wire enemy robots array and duration
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("shutdownDuration").floatValue = 10f;

        SerializedProperty robotsProp = so.FindProperty("enemyRobots");
        robotsProp.arraySize = enemyRobots.Length;
        for (int i = 0; i < enemyRobots.Length; i++)
            robotsProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyRobots[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(activeScene);

        string robotList = string.Join(", ", enemyRobots.Select(r => r.gameObject.name));
        EditorUtility.DisplayDialog(
            "Generator Shutdown Setup Complete",
            $"GeneratorShutdownController wired with {enemyRobots.Length} enemy robot(s):\n{robotList}\n\n" +
            "Shutdown duration: 10 seconds\n\n" +
            "Next: on each generator's InteractableUnityEventWrapper, add an OnClick event\n" +
            "and point it to GeneratorShutdownController → OnGeneratorPressed().\n\n" +
            "Save the scene when done.",
            "OK");
    }

    [MenuItem("Tools/Level Setup/Check Enemy Robot Speeds")]
    public static void CheckEnemyRobotSpeeds()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        RobotAI[] enemies = FindSceneObjects<RobotAI>(activeScene)
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (enemies.Length == 0)
        {
            EditorUtility.DisplayDialog("Enemy Robot Speeds", "No enemy robots found in the scene.", "OK");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (RobotAI robot in enemies)
        {
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            float speed = agent != null ? agent.speed : -1f;
            float accel = agent != null ? agent.acceleration : -1f;
            sb.AppendLine($"{robot.gameObject.name}");
            sb.AppendLine($"  Speed:        {speed:F2}");
            sb.AppendLine($"  Acceleration: {accel:F2}");
            sb.AppendLine();
        }
        sb.AppendLine($"Target speed (Set Enemy Speeds tool): {TargetEnemySpeed:F2}");

        EditorUtility.DisplayDialog("Enemy Robot Speeds", sb.ToString(), "OK");
    }

    [MenuItem("Tools/Level Setup/Set Enemy Robot Speeds")]
    public static void SetEnemyRobotSpeeds()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        RobotAI[] enemies = FindSceneObjects<RobotAI>(activeScene)
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (enemies.Length == 0)
        {
            EditorUtility.DisplayDialog("Set Enemy Speeds", "No enemy robots found in the scene.", "OK");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (RobotAI robot in enemies)
        {
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            if (agent == null) continue;

            float oldSpeed = agent.speed;
            Undo.RecordObject(agent, "Set Enemy Robot Speed");
            agent.speed = TargetEnemySpeed;
            EditorUtility.SetDirty(agent);

            sb.AppendLine($"{robot.gameObject.name}: {oldSpeed:F2} → {TargetEnemySpeed:F2}");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog(
            "Set Enemy Speeds Complete",
            sb.ToString() + $"\nAll enemy robots set to speed {TargetEnemySpeed:F2}.\nSave the scene to keep changes.",
            "OK");
    }

    [MenuItem("Tools/Level Setup/Auto-Wire LevelTwo Enemy")]
    public static void AutoWireLevelTwoEnemy()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelTwoScene(activeScene))
            return;

        Transform playerTransform = FindPlayerTransform(activeScene);
        AutoWireEnemyInternal(activeScene, playerTransform);
        BuildNavMeshSurfaces(activeScene);
        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "LevelTwo Enemy Setup Complete",
            $"Player target: {(playerTransform != null ? playerTransform.name : "none found")}\n" +
            "Enemy chase settings were applied and all NavMeshSurfaces were rebuilt.\n" +
            "Save the scene, then test the toxic robot in Play Mode.",
            "OK");
    }

    private static List<GameObject> FindSceneFloorTiles(Scene scene)
    {
        return FindSceneObjects<Transform>(scene)
            .Select(transform => transform.gameObject)
            .Where(gameObject =>
                gameObject.name.StartsWith("Floor (", StringComparison.Ordinal) &&
                gameObject.GetComponent<MeshRenderer>() != null &&
                gameObject.GetComponent<MeshFilter>() != null)
            .OrderBy(gameObject => gameObject.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void ConfigureColliderFromMesh(GameObject floorTile, BoxCollider collider)
    {
        MeshFilter meshFilter = floorTile.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            collider.center = meshFilter.sharedMesh.bounds.center;
            collider.size = meshFilter.sharedMesh.bounds.size;
        }

        collider.isTrigger = false;
    }

    private static void ConfigureNavMeshObstacle(GameObject floorTile, BoxCollider collider)
    {
        NavMeshObstacle obstacle = floorTile.GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = Undo.AddComponent<NavMeshObstacle>(floorTile);

        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = collider.center;
        obstacle.size = collider.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.enabled = false;
    }

    private static Transform FindPlayerTransform(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("Player"))
                    return child;
            }
        }

        return null;
    }

    private static TextMeshProUGUI FindTimerText(Scene scene)
    {
        TextMeshProUGUI[] texts = FindSceneObjects<TextMeshProUGUI>(scene).ToArray();

        TextMeshProUGUI namedMatch = texts.FirstOrDefault(text =>
            text.name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0);
        if (namedMatch != null)
            return namedMatch;

        return texts.FirstOrDefault(text =>
            string.Equals(text.text, "1:30", StringComparison.Ordinal) ||
            string.Equals(text.text, "0:00", StringComparison.Ordinal));
    }

    private static T FindOrCreateSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        T existing = FindSceneObjects<T>(scene).FirstOrDefault(component => component.gameObject.name == objectName);
        if (existing != null)
            return existing;

        GameObject gameObject = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
        return Undo.AddComponent<T>(gameObject);
    }

    private static IEnumerable<T> FindSceneObjects<T>(Scene scene) where T : UnityEngine.Object
    {
        return Resources.FindObjectsOfTypeAll<T>().Where(obj =>
        {
            if (obj is Component component)
                return component.gameObject.scene == scene;

            if (obj is GameObject gameObject)
                return gameObject.scene == scene;

            return false;
        });
    }

    private static bool ValidateLevelTwoScene(Scene activeScene)
    {
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("LevelTwo Setup", "Open the LevelTwo scene before running setup.", "OK");
            return false;
        }

        if (string.Equals(activeScene.name, LevelTwoSceneName, StringComparison.Ordinal))
            return true;

        bool continueAnyway = EditorUtility.DisplayDialog(
            "LevelTwo Setup",
            $"Active scene is '{activeScene.name}', not '{LevelTwoSceneName}'. Continue anyway?",
            "Continue",
            "Cancel");

        return continueAnyway;
    }

    private static void AutoWireEnemyInternal(Scene scene, Transform playerTransform)
    {
        RobotAI[] enemyRobots = FindSceneObjects<RobotAI>(scene)
            .Where(robot => !robot.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .ToArray();

        if (enemyRobots.Length == 0)
            return;

        foreach (RobotAI enemyRobot in enemyRobots)
        {
            SerializedObject enemySerialized = new SerializedObject(enemyRobot);
            enemySerialized.FindProperty("playerOverride").objectReferenceValue = playerTransform;
            enemySerialized.FindProperty("chaseRange").floatValue = Mathf.Max(DefaultEnemyChaseRange, enemySerialized.FindProperty("chaseRange").floatValue);
            enemySerialized.FindProperty("attackRange").floatValue = Mathf.Max(DefaultEnemyAttackRange, enemySerialized.FindProperty("attackRange").floatValue);
            enemySerialized.FindProperty("canRun").boolValue = true;
            // Enemies stay idle until Level1Manager.StartLevel() activates them.
            enemySerialized.FindProperty("startActiveOnAwake").boolValue = false;
            enemySerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyRobot);

            NavMeshAgent agent = enemyRobot.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Undo.RecordObject(agent, "Configure LevelTwo Enemy NavMeshAgent");
                agent.enabled = true;
                // Do not clamp speed here — use Tools > Set Enemy Robot Speeds to control speed.
                agent.acceleration = Mathf.Max(agent.acceleration, 8f);
                agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.6f);
                agent.autoRepath = true;
                EditorUtility.SetDirty(agent);
            }
        }

        // Wire all enemies into Level1Manager so StartLevel() activates them.
        Level1Manager levelManager = FindSceneObjects<Level1Manager>(scene).FirstOrDefault();
        if (levelManager != null)
        {
            SerializedObject levelManagerSerialized = new SerializedObject(levelManager);
            SerializedProperty enemiesProp = levelManagerSerialized.FindProperty("enemyRobots");
            if (enemiesProp != null)
            {
                enemiesProp.arraySize = enemyRobots.Length;
                for (int i = 0; i < enemyRobots.Length; i++)
                    enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyRobots[i];
                levelManagerSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(levelManager);
            }
        }
    }

    private static void BuildNavMeshSurfaces(Scene scene)
    {
        foreach (NavMeshSurface surface in FindSceneObjects<NavMeshSurface>(scene))
        {
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
        }
    }

    /// <summary>
    /// Returns true if the given transform is anywhere inside a hierarchy
    /// whose root or ancestors contain "PPE" in their name.
    /// Used to avoid wiring health UI components to PPE panel labels by mistake.
    /// </summary>
    private static bool IsUnderPPEHierarchy(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.name.IndexOf("PPE", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }
        return false;
    }

    /// <summary>Truncates a string to maxLength and appends "…" if it was cut.</summary>
    private static string Truncate(string s, int maxLength)
    {
        if (s == null) return string.Empty;
        s = s.Replace("\n", " ").Replace("\r", "");
        return s.Length <= maxLength ? s : s.Substring(0, maxLength) + "…";
    }
}
#endif
