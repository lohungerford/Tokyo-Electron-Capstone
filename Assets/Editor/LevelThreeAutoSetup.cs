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

public static class LevelThreeAutoSetup
{
    private const string LevelThreeSceneName = "LevelThree";
    private const string LevelManagerObjectName = "Level1Manager";
    private const string TileManagerObjectName = "LevelTileManager";
    private const float DefaultEnemyChaseRange = 10f;
    private const float DefaultEnemyAttackRange = 1.5f;
    private const float TargetEnemySpeed = 1.2f; // faster than Level 2's 0.6
    private const int EnemyAttackDamage = 1;     // less damage than Level 2's 2

    // =========================================================
    // 1. Full baseline wiring
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Auto-Wire LevelThree")]
    public static void AutoWireLevelThree()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        FriendlyAIScript[] robots = FindSceneObjects<FriendlyAIScript>(activeScene)
            .Where(r => r.useBounceMovement && r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (robots.Length == 0)
        {
            EditorUtility.DisplayDialog("LevelThree Setup",
                "No GoodRob robots with FriendlyAIScript were found.", "OK");
            return;
        }

        List<GameObject> floorTiles = FindSceneFloorTiles(activeScene);
        if (floorTiles.Count == 0)
        {
            EditorUtility.DisplayDialog("LevelThree Setup",
                "No floor tiles named 'Floor (...)' were found. Rename or add the floor tiles first.", "OK");
            return;
        }

        // Add BoxCollider + NavMeshObstacle + DisappearMechanic to every tile
        List<DisappearMechanic> disappearTiles = new List<DisappearMechanic>(floorTiles.Count);
        foreach (GameObject floorTile in floorTiles)
        {
            BoxCollider col = floorTile.GetComponent<BoxCollider>();
            if (col == null)
                col = Undo.AddComponent<BoxCollider>(floorTile);

            ConfigureColliderFromMesh(floorTile, col);
            ConfigureNavMeshObstacle(floorTile, col);

            DisappearMechanic dm = floorTile.GetComponent<DisappearMechanic>();
            if (dm == null)
                dm = Undo.AddComponent<DisappearMechanic>(floorTile);

            disappearTiles.Add(dm);
        }

        // LevelTileManager
        LevelTileManager tileManager = FindOrCreateSceneComponent<LevelTileManager>(activeScene, TileManagerObjectName);
        Undo.RecordObject(tileManager, "Wire LevelThree Tile Manager");
        tileManager.allTiles = disappearTiles.ToArray();
        tileManager.timeBetweenBreaks = 10f;
        tileManager.warningDuration = 5f;
        tileManager.brokenDuration = 12f;
        tileManager.maxActiveTiles = 3;
        EditorUtility.SetDirty(tileManager);

        // Level1Manager
        Level1Manager levelManager = FindOrCreateSceneComponent<Level1Manager>(activeScene, LevelManagerObjectName);
        TutorialUI tutorialUI = FindSceneObjects<TutorialUI>(activeScene).FirstOrDefault();
        PointsManager pointsManager = FindSceneObjects<PointsManager>(activeScene).FirstOrDefault();
        Transform playerTransform = FindPlayerTransform(activeScene);
        TextMeshProUGUI timerText = FindTimerText(activeScene);

        SerializedObject lmSO = new SerializedObject(levelManager);
        lmSO.FindProperty("tileManager").objectReferenceValue = tileManager;
        lmSO.FindProperty("pointsManager").objectReferenceValue = pointsManager;
        lmSO.FindProperty("tutorialUI").objectReferenceValue = tutorialUI;
        lmSO.FindProperty("timerText").objectReferenceValue = timerText;
        lmSO.FindProperty("levelDuration").floatValue = 90f;
        lmSO.FindProperty("pointsPerRobotPerSecond").intValue = 1;
        lmSO.FindProperty("maxRobotDeaths").intValue = Mathf.Max(1, robots.Length - 1);
        lmSO.FindProperty("playerFallY").floatValue = -5f;
        lmSO.FindProperty("playerTransform").objectReferenceValue = playerTransform;

        SerializedProperty robotsProp = lmSO.FindProperty("robots");
        robotsProp.arraySize = robots.Length;
        for (int i = 0; i < robots.Length; i++)
            robotsProp.GetArrayElementAtIndex(i).objectReferenceValue = robots[i];

        lmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(levelManager);

        // TutorialUI — Level 3 ends at MainMenu
        if (tutorialUI != null)
        {
            SerializedObject tuSO = new SerializedObject(tutorialUI);
            tuSO.FindProperty("nextSceneName").stringValue = "MainMenu";
            tuSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorialUI);
        }

        // Friendly robot settings
        foreach (FriendlyAIScript robot in robots)
        {
            SerializedObject rSO = new SerializedObject(robot);
            rSO.FindProperty("useBounceMovement").boolValue = true;
            rSO.FindProperty("startMovingOnAwake").boolValue = false;
            rSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(robot);
        }

        AutoWireEnemyL3Internal(activeScene, playerTransform);
        BuildNavMeshSurfaces(activeScene);
        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "LevelThree Setup Complete",
            $"Wired {robots.Length} robots and {disappearTiles.Count} tiles.\n" +
            $"Fail threshold: {Mathf.Max(1, robots.Length - 1)} robot losses.\n" +
            $"TutorialUI nextSceneName: 'MainMenu'\n" +
            $"Timer text: {(timerText != null ? timerText.name : "none found")}\n" +
            $"Player transform: {(playerTransform != null ? playerTransform.name : "none found")}\n\n" +
            "Enemy robots configured (canRun=true, attackDamage=1, speed wired).\n" +
            "NavMeshSurfaces rebuilt.\n\n" +
            "Next steps:\n" +
            "  1. Run 'L3 - Auto-Wire Generator Shutdown'\n" +
            "  2. Run 'L3 - Auto-Wire Player Health'\n" +
            "  3. Run 'L3 - Setup Level 3 UI Content'\n" +
            "  4. Run 'L3 - Set Enemy Robot Speeds'\n" +
            "  5. Manually wire generator buttons → GeneratorShutdownController.OnGeneratorPressed()\n" +
            "  6. Manually fix RETRY button → TutorialUI.OnRetryPressed()\n" +
            "  7. Save scene.",
            "OK");
    }

    // =========================================================
    // 2. Enemy-only wiring (Level 3: canRun=true, attackDamage=1)
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Auto-Wire LevelThree Enemy")]
    public static void AutoWireLevelThreeEnemy()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        Transform playerTransform = FindPlayerTransform(activeScene);
        AutoWireEnemyL3Internal(activeScene, playerTransform);
        BuildNavMeshSurfaces(activeScene);
        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "LevelThree Enemy Setup Complete",
            $"Player target: {(playerTransform != null ? playerTransform.name : "none found")}\n" +
            "canRun=true, attackDamage=1, startActiveOnAwake=false applied to all enemy robots.\n" +
            "NavMeshSurfaces rebuilt.\n" +
            "Save the scene, then test in Play Mode.",
            "OK");
    }

    // =========================================================
    // 3. Generator shutdown wiring
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Auto-Wire Generator Shutdown")]
    public static void AutoWireGeneratorShutdown()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        RobotAI[] enemyRobots = FindSceneObjects<RobotAI>(activeScene)
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (enemyRobots.Length == 0)
        {
            EditorUtility.DisplayDialog("Generator Shutdown Setup",
                "No enemy RobotAI found (any RobotAI not named 'GoodRob...').\n" +
                "Add your electric robot(s) to the scene first.", "OK");
            return;
        }

        GeneratorShutdownController controller =
            FindSceneObjects<GeneratorShutdownController>(activeScene).FirstOrDefault();
        if (controller == null)
        {
            GameObject go = new GameObject("GeneratorShutdownController");
            SceneManager.MoveGameObjectToScene(go, activeScene);
            Undo.RegisterCreatedObjectUndo(go, "Create GeneratorShutdownController");
            controller = Undo.AddComponent<GeneratorShutdownController>(go);
        }

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
            $"GeneratorShutdownController wired with {enemyRobots.Length} robot(s):\n{robotList}\n\n" +
            "Shutdown duration: 10 seconds\n\n" +
            "MANUAL STEP: On each generator's InteractableUnityEventWrapper,\n" +
            "add an OnClick event → GeneratorShutdownController.OnGeneratorPressed().\n\n" +
            "Save the scene when done.",
            "OK");
    }

    // =========================================================
    // 4. Player health wiring (identical logic to Level 2 tool)
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Auto-Wire Player Health")]
    public static void AutoWirePlayerHealth()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();

        // 4a. Find or add PlayerHealth
        PlayerHealth playerHealth = FindSceneObjects<PlayerHealth>(activeScene).FirstOrDefault();
        GameObject playerObject = null;

        if (playerHealth == null)
        {
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag("Player")) { playerObject = t.gameObject; break; }
                }
                if (playerObject != null) break;
            }

            if (playerObject == null)
            {
                EditorUtility.DisplayDialog("Auto-Wire Player Health",
                    "Could not find a GameObject tagged 'Player'.\n" +
                    "Tag your Camera Rig root as 'Player' and run this tool again.", "OK");
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

        // 4b. Wire into Level1Manager
        Level1Manager levelManager = FindSceneObjects<Level1Manager>(activeScene).FirstOrDefault();
        if (levelManager != null)
        {
            SerializedObject lmSO = new SerializedObject(levelManager);
            lmSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            lmSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelManager);
            log.AppendLine("✓ Wired PlayerHealth into Level1Manager.");
        }
        else
        {
            log.AppendLine("⚠ Level1Manager not found — run L3 - Auto-Wire LevelThree first.");
        }

        // 4c. Hit flash canvas
        HitFlashEffect existingFlash = FindSceneObjects<HitFlashEffect>(activeScene).FirstOrDefault();
        if (existingFlash != null)
        {
            SerializedObject flashSO = new SerializedObject(existingFlash);
            flashSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            flashSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existingFlash);
            log.AppendLine("✓ Found existing HitFlashEffect — re-wired PlayerHealth.");
        }
        else
        {
            OVRCameraRig ovrRig = FindSceneObjects<OVRCameraRig>(activeScene).FirstOrDefault();
            Transform eyeAnchor = ovrRig != null ? ovrRig.centerEyeAnchor : null;

            if (eyeAnchor == null)
            {
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (string.Equals(t.name, "CenterEyeAnchor", StringComparison.OrdinalIgnoreCase))
                        { eyeAnchor = t; break; }
                    }
                    if (eyeAnchor != null) break;
                }
            }

            if (eyeAnchor == null)
            {
                log.AppendLine("⚠ CenterEyeAnchor not found — HitFlashEffect canvas NOT created.");
                log.AppendLine("  Create a World Space Canvas under CenterEyeAnchor manually.");
            }
            else
            {
                GameObject canvasGO = new GameObject("HitFlashCanvas");
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create HitFlashCanvas");
                canvasGO.transform.SetParent(eyeAnchor, false);

                Canvas canvas = Undo.AddComponent<Canvas>(canvasGO);
                canvas.renderMode = RenderMode.WorldSpace;

                RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
                canvasRT.localPosition = new Vector3(0f, 0f, 0.31f);
                canvasRT.localRotation = Quaternion.identity;
                canvasRT.localScale = Vector3.one;
                canvasRT.sizeDelta = new Vector2(0.6f, 0.34f);

                GameObject imageGO = new GameObject("FlashImage");
                Undo.RegisterCreatedObjectUndo(imageGO, "Create FlashImage");
                imageGO.transform.SetParent(canvasGO.transform, false);

                UnityEngine.UI.Image flashImage = Undo.AddComponent<UnityEngine.UI.Image>(imageGO);
                flashImage.color = new Color(1f, 0f, 0f, 0f);

                RectTransform imageRT = imageGO.GetComponent<RectTransform>();
                imageRT.anchorMin = Vector2.zero;
                imageRT.anchorMax = Vector2.one;
                imageRT.offsetMin = Vector2.zero;
                imageRT.offsetMax = Vector2.zero;

                HitFlashEffect flashEffect = Undo.AddComponent<HitFlashEffect>(canvasGO);
                SerializedObject flashSO = new SerializedObject(flashEffect);
                flashSO.FindProperty("playerHealth").objectReferenceValue = playerHealth;
                flashSO.FindProperty("flashImage").objectReferenceValue = flashImage;
                flashSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(flashEffect);

                log.AppendLine($"✓ Created HitFlashCanvas under '{eyeAnchor.name}'.");
            }
        }

        // 4d. PlayerHealthUI
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
            UnityEngine.UI.Slider healthSlider = FindSceneObjects<UnityEngine.UI.Slider>(activeScene)
                .FirstOrDefault(s =>
                    !IsUnderPPEHierarchy(s.transform) &&
                    (s.name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     s.name.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0));

            TextMeshProUGUI healthLabel = FindSceneObjects<TextMeshProUGUI>(activeScene)
                .FirstOrDefault(t =>
                    !IsUnderPPEHierarchy(t.transform) &&
                    (t.name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     t.name.IndexOf("hp", StringComparison.OrdinalIgnoreCase) >= 0));

            if (healthSlider != null || healthLabel != null)
            {
                GameObject uiTarget = healthSlider != null ? healthSlider.gameObject : healthLabel.gameObject;
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
                log.AppendLine("⚠ No health Slider or TMP label found (name must contain 'health' or 'hp',");
                log.AppendLine("  not inside a PPE panel). Name your Slider 'HealthSlider' and re-run.");
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Auto-Wire Player Health — Complete",
            log.ToString() + "\nSave the scene when done.", "OK");
    }

    // =========================================================
    // 5. UI text patches
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Setup Level 3 UI Content")]
    public static void SetupLevelThreeUIContent()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();
        int textPatches = 0;

        TutorialUI tutorialUI = FindSceneObjects<TutorialUI>(activeScene).FirstOrDefault();
        if (tutorialUI != null)
        {
            SerializedObject tuSO = new SerializedObject(tutorialUI);
            tuSO.FindProperty("nextSceneName").stringValue = "MainMenu";
            tuSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorialUI);
            log.AppendLine("✓ TutorialUI: nextSceneName set to 'MainMenu'.");
            log.AppendLine("⚠ MANUAL CHECK: Confirm the RETRY button calls TutorialUI.OnRetryPressed().");
        }
        else
        {
            log.AppendLine("⚠ TutorialUI not found in scene.");
        }

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Level 2 - Chemical Environment",  "Level 3 - Electrical Environment" },
            { "Level 2 - Floor Patrol",          "Level 3 - Electrical Environment" },
            { "Level 1 - Floor Patrol",          "Level 3 - Electrical Environment" },
            { "Level 2",                         "Level 3" },
            { "Level 1",                         "Level 3" },
            { "Level Two",                       "Level Three" },
            { "Level One",                       "Level Three" },
            { "LevelTwo",                        "LevelThree" },
            { "LevelOne",                        "LevelThree" },
            {
                "Protect friendly robots from toxic bots and broken floor tiles.\nUse generators to shut down toxic robots.\nYou have 10 HP — don't let them hit you.",
                "Protect friendly robots from electric bots and broken floor tiles.\nElectric bots are fast — use generators to shut them down!\nYou have 10 HP, but each hit only deals 1 damage."
            },
            {
                "Place barriers to protect friendly robots from falling through broken floor tiles.",
                "Protect friendly robots from electric bots and broken floor tiles.\nElectric bots are fast — use generators to shut them down!\nYou have 10 HP, but each hit only deals 1 damage."
            },
        };

        foreach (TextMeshProUGUI tmp in FindSceneObjects<TextMeshProUGUI>(activeScene))
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text)) continue;

            string original = tmp.text;
            string updated = original;

            foreach (var kv in replacements)
                updated = updated.Replace(kv.Key, kv.Value);

            if (!string.Equals(original, updated, StringComparison.Ordinal))
            {
                Undo.RecordObject(tmp, "Patch Level 3 UI Text");
                tmp.text = updated;
                EditorUtility.SetDirty(tmp);
                log.AppendLine($"✓ '{tmp.gameObject.name}': \"{Truncate(original, 40)}\" → \"{Truncate(updated, 40)}\"");
                textPatches++;
            }
        }

        if (textPatches == 0)
            log.AppendLine("— No Level 1/2 text references found to patch (may already be updated).");

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Level 3 UI Content — Complete",
            log.ToString() + "\nSave the scene when done.", "OK");
    }

    // =========================================================
    // 6. Speed setter (Level 3 target: 1.2)
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Set Enemy Robot Speeds")]
    public static void SetEnemyRobotSpeeds()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
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
            Undo.RecordObject(agent, "Set L3 Enemy Robot Speed");
            agent.speed = TargetEnemySpeed;
            EditorUtility.SetDirty(agent);
            sb.AppendLine($"{robot.gameObject.name}: {oldSpeed:F2} → {TargetEnemySpeed:F2}");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Set Level 3 Enemy Speeds Complete",
            sb.ToString() + $"\nAll enemy robots set to speed {TargetEnemySpeed:F2}.\nSave the scene.",
            "OK");
    }

    // =========================================================
    // 7. Speed check
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Check Enemy Robot Speeds")]
    public static void CheckEnemyRobotSpeeds()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        RobotAI[] enemies = FindSceneObjects<RobotAI>(activeScene)
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .OrderBy(r => r.gameObject.name, StringComparer.Ordinal)
            .ToArray();

        if (enemies.Length == 0)
        {
            EditorUtility.DisplayDialog("Enemy Robot Speeds", "No enemy robots found.", "OK");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (RobotAI robot in enemies)
        {
            NavMeshAgent agent = robot.GetComponent<NavMeshAgent>();
            float speed = agent != null ? agent.speed : -1f;
            sb.AppendLine($"{robot.gameObject.name}  speed={speed:F2}  attackDamage={robot.attackDamage}  canRun={robot.canRun}");
        }
        sb.AppendLine($"\nTarget speed: {TargetEnemySpeed:F2}  |  Target attackDamage: {EnemyAttackDamage}");

        EditorUtility.DisplayDialog("Level 3 Enemy Robot Status", sb.ToString(), "OK");
    }

    // =========================================================
    // 8. NavMesh bake
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Bake NavMesh")]
    public static void BakeNavMesh()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();

        // Find or create a NavMeshSurface
        NavMeshSurface surface = FindSceneObjects<NavMeshSurface>(activeScene).FirstOrDefault();
        if (surface == null)
        {
            GameObject go = new GameObject("NavMeshSurface");
            SceneManager.MoveGameObjectToScene(go, activeScene);
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
            surface = Undo.AddComponent<NavMeshSurface>(go);

            // Collect from all child objects in the scene
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            log.AppendLine("✓ Created new NavMeshSurface (CollectObjects.All).");
        }
        else
        {
            log.AppendLine($"✓ Found existing NavMeshSurface on '{surface.gameObject.name}'.");
        }

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(activeScene);

        log.AppendLine("✓ NavMesh baked.");
        log.AppendLine("\nIf robots still don't move, check:");
        log.AppendLine("  - Robot is positioned on the walkable NavMesh (shown as blue overlay)");
        log.AppendLine("  - NavMeshAgent component is enabled on the robot");
        log.AppendLine("  - 'startActiveOnAwake' is false (activated by START button)");

        EditorUtility.DisplayDialog("Bake NavMesh — Complete",
            log.ToString() + "\nSave the scene when done.", "OK");
    }

    // =========================================================
    // 9. Wall colliders
    // =========================================================

    [MenuItem("Tools/Level Setup/L3 - Add Wall Colliders")]
    public static void AddWallColliders()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!ValidateLevelThreeScene(activeScene))
            return;

        System.Text.StringBuilder log = new System.Text.StringBuilder();
        int added = 0;
        int skipped = 0;

        foreach (MeshFilter mf in FindSceneObjects<MeshFilter>(activeScene))
        {
            GameObject go = mf.gameObject;

            if (go.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (go.GetComponent<Collider>() != null)
            {
                skipped++;
                continue;
            }

            if (mf.sharedMesh == null)
                continue;

            MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            EditorUtility.SetDirty(go);
            log.AppendLine($"✓ Added MeshCollider to '{go.name}'");
            added++;
        }

        if (added == 0 && skipped == 0)
        {
            EditorUtility.DisplayDialog("Add Wall Colliders",
                "No GameObjects with 'wall' in their name were found.\n" +
                "Check that your wall objects are named with 'Wall' (e.g. 'Wall', 'Wall_Left', 'Side Wall').",
                "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Add Wall Colliders — Complete",
            $"Added MeshCollider to {added} wall object(s). {skipped} already had colliders.\n\n" +
            log.ToString() + "\nSave the scene when done.",
            "OK");
    }

    // =========================================================
    // Internal helpers
    // =========================================================

    private static void AutoWireEnemyL3Internal(Scene scene, Transform playerTransform)
    {
        RobotAI[] enemyRobots = FindSceneObjects<RobotAI>(scene)
            .Where(r => !r.gameObject.name.StartsWith("GoodRob", StringComparison.Ordinal))
            .ToArray();

        if (enemyRobots.Length == 0)
            return;

        foreach (RobotAI enemy in enemyRobots)
        {
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("playerOverride").objectReferenceValue = playerTransform;
            so.FindProperty("chaseRange").floatValue = Mathf.Max(DefaultEnemyChaseRange, so.FindProperty("chaseRange").floatValue);
            so.FindProperty("attackRange").floatValue = Mathf.Max(DefaultEnemyAttackRange, so.FindProperty("attackRange").floatValue);
            so.FindProperty("canRun").boolValue = true;
            so.FindProperty("startActiveOnAwake").boolValue = false;
            so.FindProperty("attackDamage").intValue = EnemyAttackDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);

            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Undo.RecordObject(agent, "Configure LevelThree Enemy NavMeshAgent");
                agent.enabled = true;
                agent.acceleration = Mathf.Max(agent.acceleration, 8f);
                agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.6f);
                agent.autoRepath = true;
                EditorUtility.SetDirty(agent);
            }
        }

        // Wire into Level1Manager.enemyRobots[]
        Level1Manager levelManager = FindSceneObjects<Level1Manager>(scene).FirstOrDefault();
        if (levelManager != null)
        {
            SerializedObject lmSO = new SerializedObject(levelManager);
            SerializedProperty enemiesProp = lmSO.FindProperty("enemyRobots");
            if (enemiesProp != null)
            {
                enemiesProp.arraySize = enemyRobots.Length;
                for (int i = 0; i < enemyRobots.Length; i++)
                    enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyRobots[i];
                lmSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(levelManager);
            }
        }
    }

    private static List<GameObject> FindSceneFloorTiles(Scene scene)
    {
        return FindSceneObjects<Transform>(scene)
            .Select(t => t.gameObject)
            .Where(go =>
                go.name.StartsWith("Floor (", StringComparison.Ordinal) &&
                go.GetComponent<MeshRenderer>() != null &&
                go.GetComponent<MeshFilter>() != null)
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void ConfigureColliderFromMesh(GameObject floorTile, BoxCollider col)
    {
        MeshFilter mf = floorTile.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            col.center = mf.sharedMesh.bounds.center;
            col.size   = mf.sharedMesh.bounds.size;
        }
        col.isTrigger = false;
    }

    private static void ConfigureNavMeshObstacle(GameObject floorTile, BoxCollider col)
    {
        NavMeshObstacle obs = floorTile.GetComponent<NavMeshObstacle>();
        if (obs == null)
            obs = Undo.AddComponent<NavMeshObstacle>(floorTile);

        obs.shape             = NavMeshObstacleShape.Box;
        obs.center            = col.center;
        obs.size              = col.size;
        obs.carving           = true;
        obs.carveOnlyStationary = false;
        obs.enabled           = false;
    }

    private static Transform FindPlayerTransform(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag("Player"))
                    return t;
        return null;
    }

    private static TextMeshProUGUI FindTimerText(Scene scene)
    {
        TextMeshProUGUI[] texts = FindSceneObjects<TextMeshProUGUI>(scene).ToArray();
        TextMeshProUGUI named = texts.FirstOrDefault(t =>
            t.name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0);
        if (named != null) return named;
        return texts.FirstOrDefault(t =>
            string.Equals(t.text, "1:30", StringComparison.Ordinal) ||
            string.Equals(t.text, "0:00", StringComparison.Ordinal));
    }

    private static T FindOrCreateSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        T existing = FindSceneObjects<T>(scene)
            .FirstOrDefault(c => c.gameObject.name == objectName);
        if (existing != null) return existing;

        GameObject go = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
        return Undo.AddComponent<T>(go);
    }

    private static IEnumerable<T> FindSceneObjects<T>(Scene scene) where T : UnityEngine.Object
    {
        return Resources.FindObjectsOfTypeAll<T>().Where(obj =>
        {
            if (obj is Component c) return c.gameObject.scene == scene;
            if (obj is GameObject go) return go.scene == scene;
            return false;
        });
    }

    private static bool ValidateLevelThreeScene(Scene activeScene)
    {
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("LevelThree Setup",
                "Open the LevelThree scene before running setup.", "OK");
            return false;
        }

        if (string.Equals(activeScene.name, LevelThreeSceneName, StringComparison.Ordinal))
            return true;

        return EditorUtility.DisplayDialog("LevelThree Setup",
            $"Active scene is '{activeScene.name}', not '{LevelThreeSceneName}'. Continue anyway?",
            "Continue", "Cancel");
    }

    private static void BuildNavMeshSurfaces(Scene scene)
    {
        foreach (NavMeshSurface surface in FindSceneObjects<NavMeshSurface>(scene))
        {
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
        }
    }

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

    private static string Truncate(string s, int maxLength)
    {
        if (s == null) return string.Empty;
        s = s.Replace("\n", " ").Replace("\r", "");
        return s.Length <= maxLength ? s : s.Substring(0, maxLength) + "…";
    }
}
#endif
