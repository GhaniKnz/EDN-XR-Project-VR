#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor menu: Space Food Lab → Validate Setup
/// Checks that all scenes are in Build Settings, the Player tag exists,
/// and reports any missing configuration.
/// </summary>
public static class SpaceGameSetupMenu
{
    private const string MenuRoot = "Space Food Lab/";

    [MenuItem(MenuRoot + "Validate Setup")]
    public static void ValidateSetup()
    {
        int errors = 0;
        int warnings = 0;

        Debug.Log("=== Space Food Lab — Validation ===");

        // ── Build Settings ──────────────────────────────────────────────────
        string[] required = { "MainMenu", "Hubstation", "Spaceship" };
        foreach (string sceneName in required)
        {
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    found = true;
                    break;
                }
            }
            if (found)
                Debug.Log($"  [OK] Scene '{sceneName}' est dans Build Settings");
            else
            {
                Debug.LogError($"  [ERREUR] Scene '{sceneName}' manquante dans Build Settings");
                errors++;
            }
        }

        // ── Tags ─────────────────────────────────────────────────────────────
        bool playerTagExists = false;
        foreach (string tag in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (tag == "Player") { playerTagExists = true; break; }
        }
        if (playerTagExists)
            Debug.Log("  [OK] Tag 'Player' existe");
        else
        {
            Debug.LogError("  [ERREUR] Tag 'Player' manquant dans TagManager");
            errors++;
        }

        // ── Scripts présents ─────────────────────────────────────────────────
        string[] scriptNames =
        {
            "GameData", "GameManager", "SpaceshipController", "SpaceshipShooter",
            "FoodItem", "AsteroidController", "SpaceHUD", "SpaceshipSceneBuilder",
            "ShipCameraFollow"
        };

        foreach (string script in scriptNames)
        {
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {script}");
            if (guids.Length > 0)
                Debug.Log($"  [OK] Script '{script}' trouvé");
            else
            {
                Debug.LogWarning($"  [WARNING] Script '{script}' introuvable dans le projet");
                warnings++;
            }
        }

        // ── Spaceship scene contient SpaceshipSetup ──────────────────────────
        string[] spaceshipGuids = AssetDatabase.FindAssets("Spaceship t:Scene");
        if (spaceshipGuids.Length > 0)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(spaceshipGuids[0]);
            string content = System.IO.File.ReadAllText(scenePath);
            if (content.Contains("SpaceshipSetup"))
                Debug.Log("  [OK] GameObject 'SpaceshipSetup' trouvé dans Spaceship.unity");
            else
            {
                Debug.LogError("  [ERREUR] 'SpaceshipSetup' absent de Spaceship.unity");
                errors++;
            }
            if (content.Contains("3e4021ac18cb6014abd740bc29ad6a9c"))
                Debug.Log("  [OK] SpaceshipSceneBuilder référencé dans Spaceship.unity");
            else
            {
                Debug.LogWarning("  [WARNING] SpaceshipSceneBuilder GUID absent dans Spaceship.unity (peut être normal si scripts pas encore compilés)");
                warnings++;
            }
        }

        // ── Cinematic next scene ──────────────────────────────────────────────
        string[] cinematicGuids = AssetDatabase.FindAssets("SpaceFoodLabCinematic t:MonoScript");
        if (cinematicGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(cinematicGuids[0]);
            string src = System.IO.File.ReadAllText(path);
            if (src.Contains("\"Spaceship\""))
                Debug.Log("  [OK] SpaceFoodLabCinematic pointe vers 'Spaceship'");
            else
            {
                Debug.LogWarning("  [WARNING] SpaceFoodLabCinematic: nextSceneName n'est peut-être pas 'Spaceship' (vérifier la valeur Inspector)");
                warnings++;
            }
        }

        // ── Résumé ────────────────────────────────────────────────────────────
        if (errors == 0 && warnings == 0)
            Debug.Log("=== TOUT EST OK — Le projet est prêt ! ===");
        else
            Debug.Log($"=== Validation terminée : {errors} erreur(s), {warnings} avertissement(s) ===");

        EditorUtility.DisplayDialog(
            "Space Food Lab — Validation",
            errors == 0
                ? $"Validation OK !\n{warnings} avertissement(s) — voir la Console pour les détails."
                : $"{errors} erreur(s) trouvée(s) !\nVoir la Console (Ctrl+Shift+C) pour les détails.",
            "OK"
        );
    }

    [MenuItem(MenuRoot + "Ouvrir MainMenu")]
    public static void OpenMainMenu() =>
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

    [MenuItem(MenuRoot + "Ouvrir Hubstation")]
    public static void OpenHubstation() =>
        EditorSceneManager.OpenScene("Assets/Scenes/Hubstation.unity", OpenSceneMode.Single);

    [MenuItem(MenuRoot + "Ouvrir Spaceship")]
    public static void OpenSpaceship() =>
        EditorSceneManager.OpenScene("Assets/Scenes/Spaceship.unity", OpenSceneMode.Single);
}
#endif
