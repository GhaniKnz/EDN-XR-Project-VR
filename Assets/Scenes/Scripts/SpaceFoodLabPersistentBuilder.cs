using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class SpaceFoodLabPersistentBuilder : MonoBehaviour
{
    [Header("Génération")]
    [SerializeField] private bool clearGeneratedBeforeBuild = true;
    [SerializeField] private string generatedRootName = "_Generated_SpaceFoodLab";

    [Header("Dimensions")]
    [SerializeField] private float roomRadius = 12f;
    [SerializeField] private float roomHeight = 5.5f;
    [SerializeField] private int wallSegments = 18;

    [Header("Palette")]
    [SerializeField] private Color floorColor = new Color(0.10f, 0.12f, 0.16f, 1f);
    [SerializeField] private Color wallColor = new Color(0.14f, 0.18f, 0.24f, 1f);
    [SerializeField] private Color accentColor = new Color(0.20f, 0.80f, 1f, 1f);
    [SerializeField] private Color glassColor = new Color(0.45f, 0.85f, 1f, 0.16f);

    [Header("Planètes")]
    [SerializeField] private Color planetColorA = new Color(0.25f, 0.85f, 1f, 1f);
    [SerializeField] private Color planetColorB = new Color(1f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color planetColorC = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color planetColorD = new Color(0.55f, 1f, 0.40f, 1f);

    private Material floorMat;
    private Material wallMat;
    private Material accentMat;
    private Material glassMat;
    private Material darkMetalMat;
    private Material screenMat;
    private Material starMat;

    public void BuildPersistentScene()
    {
        if (clearGeneratedBeforeBuild)
            ClearGeneratedScene();

        Transform root = GetOrCreateGeneratedRoot();

        CreateMaterials();

        CreateFloor(root);
        CreateCeiling(root);
        CreateRingLights(root);
        CreateWallSegments(root);
        CreateWindowArc(root);
        CreateExteriorStars(root);
        CreateColoredPlanets(root);
        CreateCentralPlatform(root);
        CreateMainScreen(root);
        CreateLabStations(root);
        CreateFoodPods(root);
        CreateAmbientLights(root);

        MarkSceneDirty();
    }

    public void ClearGeneratedScene()
    {
        Transform existing = transform.Find(generatedRootName);
        if (existing == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif

        MarkSceneDirty();
    }

    private Transform GetOrCreateGeneratedRoot()
    {
        Transform existing = transform.Find(generatedRootName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(generatedRootName);
        root.transform.SetParent(transform, false);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(root, "Create Generated Space Food Lab");
#endif

        return root.transform;
    }

    private void CreateMaterials()
    {
        floorMat = GetOrCreateLitMaterial("SFL_FloorMat", floorColor, 0.25f, 0.70f);
        wallMat = GetOrCreateLitMaterial("SFL_WallMat", wallColor, 0.35f, 0.45f);
        darkMetalMat = GetOrCreateLitMaterial("SFL_DarkMetalMat", new Color(0.07f, 0.08f, 0.10f, 1f), 0.12f, 0.90f);
        accentMat = GetOrCreateEmissiveMaterial("SFL_AccentMat", new Color(0.08f, 0.14f, 0.20f, 1f), accentColor, 2.2f);
        glassMat = GetOrCreateTransparentMaterial("SFL_GlassMat", glassColor, 0.95f);
        screenMat = GetOrCreateEmissiveMaterial("SFL_ScreenMat", new Color(0.03f, 0.08f, 0.14f, 1f), new Color(0.15f, 0.60f, 1f, 1f), 1.0f);
        starMat = GetOrCreateEmissiveMaterial("SFL_StarMat", Color.black, new Color(0.8f, 0.95f, 1f, 1f), 2.8f);
    }

    private void CreateFloor(Transform parent)
    {
        GameObject floor = CreatePrimitive("MainFloor", PrimitiveType.Cylinder, parent);
        floor.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(roomRadius, 0.25f, roomRadius);
        SetSharedMaterial(floor, floorMat);

        GameObject innerRing = CreatePrimitive("InnerRing", PrimitiveType.Cylinder, parent);
        innerRing.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        innerRing.transform.localScale = new Vector3(roomRadius * 0.72f, 0.05f, roomRadius * 0.72f);
        SetSharedMaterial(innerRing, accentMat);
    }

    private void CreateCeiling(Transform parent)
    {
        GameObject ceiling = CreatePrimitive("Ceiling", PrimitiveType.Cylinder, parent);
        ceiling.transform.localPosition = new Vector3(0f, roomHeight, 0f);
        ceiling.transform.localScale = new Vector3(roomRadius, 0.18f, roomRadius);
        SetSharedMaterial(ceiling, darkMetalMat);
    }

    private void CreateRingLights(Transform parent)
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            float r = roomRadius * 0.78f;

            GameObject lightBar = CreatePrimitive("RingLight_" + i, PrimitiveType.Cube, parent);
            lightBar.transform.localPosition = new Vector3(Mathf.Cos(angle) * r, roomHeight - 0.12f, Mathf.Sin(angle) * r);
            lightBar.transform.LookAt(new Vector3(0f, roomHeight - 0.12f, 0f));
            lightBar.transform.Rotate(0f, 90f, 0f);
            lightBar.transform.localScale = new Vector3(0.18f, 0.04f, 1.3f);
            SetSharedMaterial(lightBar, accentMat);
        }
    }

    private void CreateWallSegments(Transform parent)
    {
        for (int i = 0; i < wallSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / wallSegments;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * roomRadius, roomHeight / 2f, Mathf.Sin(angle) * roomRadius);

            bool isWindowZone = i >= 6 && i <= 11;
            if (isWindowZone)
                continue;

            GameObject wall = CreatePrimitive("Wall_" + i, PrimitiveType.Cube, parent);
            wall.transform.localPosition = pos;
            wall.transform.LookAt(new Vector3(0f, roomHeight / 2f, 0f));
            wall.transform.Rotate(0f, 180f, 0f);
            wall.transform.localScale = new Vector3(3.0f, roomHeight, 0.45f);
            SetSharedMaterial(wall, wallMat);

            GameObject accent = CreatePrimitive("WallAccent_" + i, PrimitiveType.Cube, wall.transform);
            accent.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            accent.transform.localScale = new Vector3(0.10f, 3.8f, 0.08f);
            SetSharedMaterial(accent, accentMat);
        }
    }

    private void CreateWindowArc(Transform parent)
    {
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            float angle = Mathf.Lerp(120f, 240f, t) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * roomRadius, roomHeight / 2f, Mathf.Sin(angle) * roomRadius);

            GameObject frame = CreatePrimitive("WindowFrame_" + i, PrimitiveType.Cube, parent);
            frame.transform.localPosition = pos;
            frame.transform.LookAt(new Vector3(0f, roomHeight / 2f, 0f));
            frame.transform.Rotate(0f, 180f, 0f);
            frame.transform.localScale = new Vector3(2.8f, roomHeight, 0.25f);
            SetSharedMaterial(frame, darkMetalMat);

            GameObject glass = CreatePrimitive("Glass_" + i, PrimitiveType.Cube, frame.transform);
            glass.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            glass.transform.localScale = new Vector3(0.88f, 0.90f, 0.15f);
            SetSharedMaterial(glass, glassMat);
            RemoveCollider(glass);
        }
    }

    private void CreateExteriorStars(Transform parent)
    {
        Transform starsRoot = new GameObject("Stars").transform;
        starsRoot.SetParent(parent, false);

        for (int i = 0; i < 160; i++)
        {
            GameObject star = CreatePrimitive("Star_" + i, PrimitiveType.Sphere, starsRoot);
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) + 0.15f;
            dir.Normalize();

            star.transform.localPosition = dir * Random.Range(34f, 52f);
            float s = Random.Range(0.08f, 0.22f);
            star.transform.localScale = Vector3.one * s;

            SetSharedMaterial(star, starMat);
            RemoveCollider(star);
        }
    }

    private void CreateColoredPlanets(Transform parent)
    {
        Transform planetsRoot = new GameObject("Planets").transform;
        planetsRoot.SetParent(parent, false);

        CreatePlanet(planetsRoot, "Planet_A", new Vector3(-8f, 8f, -34f), 7.5f, planetColorA, new Vector3(0f, 10f, 0f), 0.35f, 0.55f);
        CreatePlanet(planetsRoot, "Planet_B", new Vector3(10f, 11f, -42f), 5.0f, planetColorB, new Vector3(0f, -16f, 0f), 0.28f, 0.70f);
        CreatePlanet(planetsRoot, "Planet_C", new Vector3(0f, 14f, -52f), 10.0f, planetColorC, new Vector3(0f, 7f, 0f), 0.22f, 0.40f);
        CreatePlanet(planetsRoot, "Planet_D", new Vector3(15f, 6f, -28f), 3.5f, planetColorD, new Vector3(0f, 20f, 0f), 0.30f, 0.85f);

        GameObject moon = CreatePrimitive("MiniMoon", PrimitiveType.Sphere, planetsRoot);
        moon.transform.localPosition = new Vector3(-13f, 12f, -24f);
        moon.transform.localScale = Vector3.one * 1.6f;
        SetSharedMaterial(moon, GetOrCreateEmissiveMaterial("SFL_MoonMat", new Color(0.15f, 0.20f, 0.25f, 1f), new Color(0.60f, 0.90f, 1f, 1f), 0.5f));
        moon.AddComponent<PlanetFloatRotate>().Setup(new Vector3(0f, -12f, 0f), 0.18f, 1.10f);
    }

    private void CreatePlanet(Transform parent, string name, Vector3 localPos, float size, Color color, Vector3 rotationSpeed, float floatAmplitude, float floatSpeed)
    {
        Material planetMat = GetOrCreateEmissiveMaterial(
            "SFL_" + name + "_Mat",
            color * 0.20f,
            color,
            0.75f
        );

        GameObject planet = CreatePrimitive(name, PrimitiveType.Sphere, parent);
        planet.transform.localPosition = localPos;
        planet.transform.localScale = Vector3.one * size;
        SetSharedMaterial(planet, planetMat);
        RemoveCollider(planet);

        PlanetFloatRotate fx = planet.AddComponent<PlanetFloatRotate>();
        fx.Setup(rotationSpeed, floatAmplitude, floatSpeed);
    }

    private void CreateCentralPlatform(Transform parent)
    {
        GameObject basePlatform = CreatePrimitive("CentralPlatform", PrimitiveType.Cylinder, parent);
        basePlatform.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        basePlatform.transform.localScale = new Vector3(3.8f, 0.2f, 3.8f);
        SetSharedMaterial(basePlatform, darkMetalMat);

        GameObject topDisk = CreatePrimitive("CentralTopDisk", PrimitiveType.Cylinder, parent);
        topDisk.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        topDisk.transform.localScale = new Vector3(2.8f, 0.05f, 2.8f);
        SetSharedMaterial(topDisk, accentMat);
    }

    private void CreateMainScreen(Transform parent)
    {
        Transform root = new GameObject("MainGameScreen").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(0f, 2.0f, -4.0f);

        GameObject stand = CreatePrimitive("ScreenStand", PrimitiveType.Cube, root);
        stand.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        stand.transform.localScale = new Vector3(0.35f, 2.0f, 0.35f);
        SetSharedMaterial(stand, darkMetalMat);

        GameObject panel = CreatePrimitive("ScreenPanel", PrimitiveType.Cube, root);
        panel.transform.localScale = new Vector3(3.6f, 2.0f, 0.12f);
        SetSharedMaterial(panel, screenMat);

        GameObject border = CreatePrimitive("ScreenBorder", PrimitiveType.Cube, root);
        border.transform.localScale = new Vector3(3.9f, 2.3f, 0.05f);
        border.transform.localPosition = new Vector3(0f, 0f, 0.09f);
        SetSharedMaterial(border, accentMat);
        RemoveCollider(border);
    }

    private void CreateLabStations(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-4.8f, 0f, -1.8f),
            new Vector3( 4.8f, 0f, -1.8f),
            new Vector3(-5.6f, 0f,  3.0f),
            new Vector3( 5.6f, 0f,  3.0f)
        };

        for (int i = 0; i < positions.Length; i++)
            CreateSingleLabStation(parent, positions[i], i);
    }

    private void CreateSingleLabStation(Transform parent, Vector3 pos, int index)
    {
        Transform station = new GameObject("LabStation_" + index).transform;
        station.SetParent(parent, false);
        station.localPosition = pos;

        GameObject table = CreatePrimitive("Table", PrimitiveType.Cube, station);
        table.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        table.transform.localScale = new Vector3(2.3f, 0.16f, 1.2f);
        SetSharedMaterial(table, darkMetalMat);

        for (int i = 0; i < 4; i++)
        {
            GameObject leg = CreatePrimitive("Leg_" + i, PrimitiveType.Cube, station);
            leg.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);

            float x = i < 2 ? -0.95f : 0.95f;
            float z = i % 2 == 0 ? -0.42f : 0.42f;
            leg.transform.localPosition = new Vector3(x, 0.5f, z);
            SetSharedMaterial(leg, wallMat);
        }

        GameObject holo = CreatePrimitive("HoloScreen", PrimitiveType.Cube, station);
        holo.transform.localPosition = new Vector3(0f, 1.7f, -0.25f);
        holo.transform.localScale = new Vector3(1.1f, 0.7f, 0.05f);
        SetSharedMaterial(holo, glassMat);
        RemoveCollider(holo);

        GameObject cooker = CreatePrimitive("CookerCore", PrimitiveType.Cylinder, station);
        cooker.transform.localPosition = new Vector3(-0.55f, 1.15f, 0f);
        cooker.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
        SetSharedMaterial(cooker, accentMat);

        GameObject analyzer = CreatePrimitive("Analyzer", PrimitiveType.Cube, station);
        analyzer.transform.localPosition = new Vector3(0.55f, 1.18f, 0f);
        analyzer.transform.localScale = new Vector3(0.42f, 0.24f, 0.42f);
        SetSharedMaterial(analyzer, wallMat);
    }

    private void CreateFoodPods(Transform parent)
    {
        Vector3[] podPositions =
        {
            new Vector3(-2.2f, 0f, 4.5f),
            new Vector3( 0.0f, 0f, 5.0f),
            new Vector3( 2.2f, 0f, 4.5f)
        };

        Color[] podColors = { planetColorA, planetColorB, planetColorC };

        for (int i = 0; i < podPositions.Length; i++)
        {
            Transform pod = new GameObject("FoodPod_" + i).transform;
            pod.SetParent(parent, false);
            pod.localPosition = podPositions[i];

            GameObject basePart = CreatePrimitive("PodBase", PrimitiveType.Cylinder, pod);
            basePart.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            basePart.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
            SetSharedMaterial(basePart, darkMetalMat);

            GameObject glassTube = CreatePrimitive("GlassTube", PrimitiveType.Cylinder, pod);
            glassTube.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            glassTube.transform.localScale = new Vector3(0.42f, 0.90f, 0.42f);
            SetSharedMaterial(glassTube, glassMat);
            RemoveCollider(glassTube);

            Material specimenMat = GetOrCreateEmissiveMaterial(
                "SFL_FoodSpecimen_" + i + "_Mat",
                podColors[i] * 0.15f,
                podColors[i],
                1.1f
            );

            PrimitiveType type = i == 0 ? PrimitiveType.Sphere : i == 1 ? PrimitiveType.Capsule : PrimitiveType.Cube;
            GameObject specimen = CreatePrimitive("Specimen", type, pod);
            specimen.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            specimen.transform.localScale = i == 2 ? new Vector3(0.28f, 0.28f, 0.28f) : new Vector3(0.34f, 0.34f, 0.34f);
            SetSharedMaterial(specimen, specimenMat);
            specimen.AddComponent<PlanetFloatRotate>().Setup(new Vector3(0f, 25f, 0f), 0.08f, 1.4f);
        }
    }

    private void CreateAmbientLights(Transform parent)
    {
        Transform lightsRoot = new GameObject("AmbientLights").transform;
        lightsRoot.SetParent(parent, false);

        GameObject center = new GameObject("CenterLight");
        center.transform.SetParent(lightsRoot, false);
        center.transform.localPosition = new Vector3(0f, 3.2f, 0f);

        Light centerLight = center.AddComponent<Light>();
        centerLight.type = LightType.Point;
        centerLight.color = new Color(0.35f, 0.85f, 1f, 1f);
        centerLight.intensity = 14f;
        centerLight.range = 20f;
        centerLight.shadows = LightShadows.Soft;

        CreatePointLight(lightsRoot, new Vector3(-5f, 2.6f, -2f), new Color(0.20f, 0.90f, 1f), 6f, 10f);
        CreatePointLight(lightsRoot, new Vector3(5f, 2.6f, -2f), new Color(0.20f, 0.90f, 1f), 6f, 10f);
        CreatePointLight(lightsRoot, new Vector3(0f, 2.2f, 5f), new Color(0.35f, 1.00f, 0.45f), 5f, 9f);
    }

    private void CreatePointLight(Transform parent, Vector3 localPos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject("PointLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
    }

    private GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
#endif

        return go;
    }

    private void SetSharedMaterial(GameObject go, Material mat)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;
    }

    private void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(c);
        else
            Destroy(c);
#else
        Destroy(c);
#endif
    }

    private Shader FindBestShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        return shader;
    }

    private Material GetOrCreateLitMaterial(string assetName, Color baseColor, float metallic, float smoothness)
    {
        Material mat = GetOrCreateMaterialAsset(assetName, FindBestShader());

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

#if UNITY_EDITOR
        EditorUtility.SetDirty(mat);
#endif
        return mat;
    }

    private Material GetOrCreateEmissiveMaterial(string assetName, Color baseColor, Color emissionColor, float intensity)
    {
        Material mat = GetOrCreateMaterialAsset(assetName, FindBestShader());
        Color finalEmission = emissionColor * intensity;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", finalEmission);
        }
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.15f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);

#if UNITY_EDITOR
        EditorUtility.SetDirty(mat);
#endif
        return mat;
    }

    private Material GetOrCreateTransparentMaterial(string assetName, Color color, float smoothness)
    {
        Material mat = GetOrCreateMaterialAsset(assetName, FindBestShader());

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        else
        {
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            mat.renderQueue = 3000;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(mat);
#endif
        return mat;
    }

    private Material GetOrCreateMaterialAsset(string assetName, Shader shader)
    {
#if UNITY_EDITOR
        string root = "Assets/Generated";
        string sub1 = root + "/SpaceFoodLab";
        string sub2 = sub1 + "/Materials";

        EnsureFolder("Assets", "Generated");
        EnsureFolder(root, "SpaceFoodLab");
        EnsureFolder(sub1, "Materials");

        string path = sub2 + "/" + assetName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = assetName;
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        return mat;
#else
        Material mat = new Material(shader);
        mat.name = assetName;
        return mat;
#endif
    }

#if UNITY_EDITOR
    private void EnsureFolder(string parent, string name)
    {
        string full = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, name);
    }
#endif

    private void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}

public class PlanetFloatRotate : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 8f, 0f);
    [SerializeField] private float floatAmplitude = 0.20f;
    [SerializeField] private float floatSpeed = 0.70f;

    private Vector3 baseLocalPosition;
    private float seed;

    public void Setup(Vector3 newRotationSpeed, float newFloatAmplitude, float newFloatSpeed)
    {
        rotationSpeed = newRotationSpeed;
        floatAmplitude = newFloatAmplitude;
        floatSpeed = newFloatSpeed;
    }

    private void Start()
    {
        baseLocalPosition = transform.localPosition;
        seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float y = Mathf.Sin((Time.time + seed) * floatSpeed) * floatAmplitude;
        transform.localPosition = baseLocalPosition + new Vector3(0f, y, 0f);
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}