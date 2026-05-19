using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpaceshipSceneCreator
{
    private static readonly string ScenePath = "Assets/Scenes/Spaceship.unity";

    [MenuItem("Tools/Spaceship/Create Spaceship Scene")]
    public static void CreateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Scène déjà existante",
                $"La scène {ScenePath} existe déjà. Écraser ?",
                "Oui", "Annuler");
            if (!overwrite) return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── XR Rig ──────────────────────────────────────────────────────────────
        GameObject xrRig = FindAndInstantiateXRPrefab();
        if (xrRig != null)
            Debug.Log("[SpaceshipScene] XR rig ajouté : " + xrRig.name);
        else
            Debug.LogWarning("[SpaceshipScene] XR rig non trouvé — ajoute-le manuellement.");

        // ── SpaceshipSetup ───────────────────────────────────────────────────────
        GameObject setup = new GameObject("SpaceshipSetup");
        setup.AddComponent<SpaceshipSceneSetup>();
        Debug.Log("[SpaceshipScene] SpaceshipSetup créé avec SpaceshipSceneSetup.");

        // ── Lumière directionnelle minimale ──────────────────────────────────────
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 0.3f;
        lightGO.transform.eulerAngles = new Vector3(50f, -30f, 0f);

        // ── Tag Player sur le setup (le ship l'aura à runtime) ───────────────────
        EnsureTagExists("Player");

        // ── Sauvegarder ───────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // ── Ajouter aux Build Settings ────────────────────────────────────────────
        AddSceneToBuild(ScenePath);

        Debug.Log($"[SpaceshipScene] Scène créée et sauvegardée : {ScenePath}");
        EditorUtility.DisplayDialog("Succès", $"Scène créée :\n{ScenePath}\n\nAjoutée aux Build Settings.", "OK");
    }

    private static GameObject FindAndInstantiateXRPrefab()
    {
        string[] candidates = {
            "XR Interaction Setup",
            "XR Origin (XR Rig)",
            "XR Origin",
            "XRRig",
            "OVRCameraRig",
            "PICO XR Rig",
        };

        foreach (string name in candidates)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.name == name)
                    return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
        }
        return null;
    }

    private static void EnsureTagExists(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[SpaceshipScene] Tag '{tag}' ajouté.");
    }

    private static void AddSceneToBuild(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[SpaceshipScene] Scène ajoutée aux Build Settings.");
    }
}
