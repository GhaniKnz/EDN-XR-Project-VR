using System.Collections.Generic;
using UnityEngine;

public class SpaceFoodLabSceneBuilder : MonoBehaviour
{
    [Header("Scene globale")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool clearChildrenBeforeBuild = true;
    [SerializeField] private bool useSkyboxBackground = true;
    [SerializeField] private Material skyboxMaterial;

    [Header("Palette")]
    [SerializeField] private Color floorColor = new Color(0.10f, 0.12f, 0.16f, 1f);
    [SerializeField] private Color wallColor = new Color(0.14f, 0.18f, 0.24f, 1f);
    [SerializeField] private Color accentColor = new Color(0.20f, 0.80f, 1f, 1f);
    [SerializeField] private Color glassColor = new Color(0.45f, 0.85f, 1f, 0.16f);
    [SerializeField] private Color neonColor = new Color(0.18f, 0.95f, 1f, 1f);
    [SerializeField] private Color foodColorA = new Color(1.00f, 0.45f, 0.20f, 1f);
    [SerializeField] private Color foodColorB = new Color(0.55f, 1.00f, 0.35f, 1f);
    [SerializeField] private Color foodColorC = new Color(1.00f, 0.85f, 0.25f, 1f);

    [Header("Dimensions")]
    [SerializeField] private float roomRadius = 12f;
    [SerializeField] private float roomHeight = 5.5f;
    [SerializeField] private int wallSegments = 18;

    private Material floorMat;
    private Material wallMat;
    private Material accentMat;
    private Material glassMat;
    private Material darkMetalMat;
    private Material foodMatA;
    private Material foodMatB;
    private Material foodMatC;
    private Material spaceMat;
    private readonly List<Material> runtimeMaterials = new();

    private void Start()
    {
        if (buildOnStart)
            BuildScene();
    }

    [ContextMenu("Build Space Food Lab Scene")]
    public void BuildScene()
    {
        PrepareScene();
        ApplySkybox();
        CreateMaterials();

        GameObject root = new GameObject("SpaceFoodLab_Root");
        root.transform.SetParent(transform, false);

        CreateFloor(root.transform);
        CreateCeiling(root.transform);
        CreateRingLights(root.transform);
        CreateWallSegments(root.transform);
        CreateWindowArc(root.transform);
        CreateExteriorSpace(root.transform);
        CreateCentralPlatform(root.transform);
        CreateMainGameScreen(root.transform);
        CreateLabStations(root.transform);
        CreateFoodDisplayPods(root.transform);
        CreateCeilingCore(root.transform);
        CreateAmbientLights(root.transform);
    }

    private void PrepareScene()
    {
        if (!clearChildrenBeforeBuild)
            return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void ApplySkybox()
    {
        if (!useSkyboxBackground || skyboxMaterial == null)
            return;

        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }

    private void CreateMaterials()
    {
        floorMat = CreateLitMaterial("FloorMat", floorColor, 0.25f, 0.7f);
        wallMat = CreateLitMaterial("WallMat", wallColor, 0.35f, 0.45f);
        darkMetalMat = CreateLitMaterial("DarkMetalMat", new Color(0.07f, 0.08f, 0.10f, 1f), 0.15f, 0.85f);
        accentMat = CreateEmissiveMaterial("AccentMat", new Color(0.12f, 0.20f, 0.28f, 1f), accentColor, 2.2f);
        foodMatA = CreateEmissiveMaterial("FoodMatA", new Color(0.25f, 0.14f, 0.08f, 1f), foodColorA, 1.6f);
        foodMatB = CreateEmissiveMaterial("FoodMatB", new Color(0.10f, 0.20f, 0.08f, 1f), foodColorB, 1.6f);
        foodMatC = CreateEmissiveMaterial("FoodMatC", new Color(0.25f, 0.22f, 0.06f, 1f), foodColorC, 1.4f);
        glassMat = CreateTransparentMaterial("GlassMat", glassColor);
        spaceMat = CreateLitMaterial("SpaceMat", new Color(0.01f, 0.01f, 0.03f, 1f), 0f, 1f);
    }

    private void CreateFloor(Transform parent)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(parent, false);
        ground.transform.localScale = new Vector3(2f, 2f, 2f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "MainFloor";
        floor.transform.SetParent(ground.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(roomRadius, 0.25f, roomRadius);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        GameObject innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerRing.name = "InnerRing";
        innerRing.transform.SetParent(ground.transform, false);
        innerRing.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        innerRing.transform.localScale = new Vector3(roomRadius * 0.72f, 0.05f, roomRadius * 0.72f);
        innerRing.GetComponent<Renderer>().sharedMaterial = accentMat;
    }

    private void CreateCeiling(Transform parent)
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(parent, false);
        ceiling.transform.localPosition = new Vector3(0f, roomHeight, 0f);
        ceiling.transform.localScale = new Vector3(roomRadius, 0.18f, roomRadius);
        ceiling.GetComponent<Renderer>().sharedMaterial = darkMetalMat;
    }

    private void CreateRingLights(Transform parent)
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            float r = roomRadius * 0.78f;

            GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightBar.name = "RingLight_" + i;
            lightBar.transform.SetParent(parent, false);
            lightBar.transform.localPosition = new Vector3(Mathf.Cos(angle) * r, roomHeight - 0.12f, Mathf.Sin(angle) * r);
            lightBar.transform.LookAt(new Vector3(0f, roomHeight - 0.12f, 0f));
            lightBar.transform.Rotate(0f, 90f, 0f);
            lightBar.transform.localScale = new Vector3(0.18f, 0.04f, 1.3f);
            lightBar.GetComponent<Renderer>().sharedMaterial = accentMat;
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

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall_" + i;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.LookAt(new Vector3(0f, roomHeight / 2f, 0f));
            wall.transform.Rotate(0f, 180f, 0f);
            wall.transform.localScale = new Vector3(3.0f, roomHeight, 0.45f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;

            GameObject verticalAccent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            verticalAccent.name = "WallAccent_" + i;
            verticalAccent.transform.SetParent(wall.transform, false);
            verticalAccent.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            verticalAccent.transform.localScale = new Vector3(0.10f, 3.8f, 0.08f);
            verticalAccent.GetComponent<Renderer>().sharedMaterial = accentMat;
        }
    }

    private void CreateWindowArc(Transform parent)
    {
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            float angle = Mathf.Lerp(120f, 240f, t) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * roomRadius, roomHeight / 2f, Mathf.Sin(angle) * roomRadius);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "WindowFrame_" + i;
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = pos;
            frame.transform.LookAt(new Vector3(0f, roomHeight / 2f, 0f));
            frame.transform.Rotate(0f, 180f, 0f);
            frame.transform.localScale = new Vector3(2.8f, roomHeight, 0.25f);
            frame.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass_" + i;
            glass.transform.SetParent(frame.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            glass.transform.localScale = new Vector3(0.88f, 0.90f, 0.15f);
            glass.GetComponent<Renderer>().sharedMaterial = glassMat;
            DestroyCollider(glass);
        }
    }

    private void CreateExteriorSpace(Transform parent)
    {
        if (!useSkyboxBackground)
        {
            GameObject starSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            starSphere.name = "SpaceSphere";
            starSphere.transform.SetParent(parent, false);
            starSphere.transform.localPosition = new Vector3(0f, roomHeight * 0.45f, 0f);
            starSphere.transform.localScale = new Vector3(120f, 120f, 120f);
            starSphere.GetComponent<Renderer>().sharedMaterial = spaceMat;
            DestroyCollider(starSphere);
        }

        CreateStars(parent, 140);

        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "ExteriorPlanet";
        planet.transform.SetParent(parent, false);
        planet.transform.localPosition = new Vector3(0f, 8f, -38f);
        planet.transform.localScale = new Vector3(10f, 10f, 10f);
        planet.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial(
            "PlanetMat",
            new Color(0.07f, 0.15f, 0.22f, 1f),
            new Color(0.20f, 0.75f, 1f, 1f),
            0.7f
        );
        planet.AddComponent<SlowRotate>().rotationSpeed = new Vector3(0f, 4f, 0f);

        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        moon.transform.SetParent(parent, false);
        moon.transform.localPosition = new Vector3(10f, 12f, -30f);
        moon.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        moon.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial("MoonMat", new Color(0.55f, 0.62f, 0.70f, 1f), 0.15f, 0.7f);
        moon.AddComponent<SlowRotate>().rotationSpeed = new Vector3(0f, -7f, 0f);
    }

    private void CreateStars(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star_" + i;
            star.transform.SetParent(parent, false);

            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) + 0.15f;
            dir.Normalize();

            star.transform.localPosition = dir * Random.Range(35f, 55f);
            float s = Random.Range(0.10f, 0.28f);
            star.transform.localScale = Vector3.one * s;

            star.GetComponent<Renderer>().sharedMaterial = accentMat;
            DestroyCollider(star);
        }
    }

    private void CreateCentralPlatform(Transform parent)
    {
        GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePlatform.name = "CentralPlatform";
        basePlatform.transform.SetParent(parent, false);
        basePlatform.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        basePlatform.transform.localScale = new Vector3(3.8f, 0.2f, 3.8f);
        basePlatform.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        GameObject topDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topDisk.name = "CentralTopDisk";
        topDisk.transform.SetParent(parent, false);
        topDisk.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        topDisk.transform.localScale = new Vector3(2.8f, 0.05f, 2.8f);
        topDisk.GetComponent<Renderer>().sharedMaterial = accentMat;
    }

    private void CreateMainGameScreen(Transform parent)
    {
        GameObject screenRoot = new GameObject("MainGameScreen");
        screenRoot.transform.SetParent(parent, false);
        screenRoot.transform.localPosition = new Vector3(0f, 2.0f, -4.0f);

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "ScreenStand";
        stand.transform.SetParent(screenRoot.transform, false);
        stand.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        stand.transform.localScale = new Vector3(0.35f, 2.0f, 0.35f);
        stand.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "ScreenPanel";
        panel.transform.SetParent(screenRoot.transform, false);
        panel.transform.localScale = new Vector3(3.6f, 2.0f, 0.12f);
        panel.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial(
            "ScreenMat",
            new Color(0.04f, 0.10f, 0.16f, 1f),
            new Color(0.10f, 0.55f, 0.90f, 1f),
            0.9f
        );

        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = "ScreenBorder";
        border.transform.SetParent(screenRoot.transform, false);
        border.transform.localScale = new Vector3(3.9f, 2.3f, 0.05f);
        border.transform.localPosition = new Vector3(0f, 0f, 0.09f);
        border.GetComponent<Renderer>().sharedMaterial = accentMat;
        DestroyCollider(border);
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
        {
            CreateSingleLabStation(parent, positions[i], i);
        }
    }

    private void CreateSingleLabStation(Transform parent, Vector3 pos, int index)
    {
        GameObject station = new GameObject("LabStation_" + index);
        station.transform.SetParent(parent, false);
        station.transform.localPosition = pos;

        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Table";
        table.transform.SetParent(station.transform, false);
        table.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        table.transform.localScale = new Vector3(2.3f, 0.16f, 1.2f);
        table.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        for (int i = 0; i < 4; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg_" + i;
            leg.transform.SetParent(station.transform, false);
            leg.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);

            float x = i < 2 ? -0.95f : 0.95f;
            float z = i % 2 == 0 ? -0.42f : 0.42f;
            leg.transform.localPosition = new Vector3(x, 0.5f, z);
            leg.GetComponent<Renderer>().sharedMaterial = wallMat;
        }

        GameObject holoScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        holoScreen.name = "HoloScreen";
        holoScreen.transform.SetParent(station.transform, false);
        holoScreen.transform.localPosition = new Vector3(0f, 1.7f, -0.25f);
        holoScreen.transform.localScale = new Vector3(1.1f, 0.7f, 0.05f);
        holoScreen.GetComponent<Renderer>().sharedMaterial = glassMat;
        DestroyCollider(holoScreen);

        GameObject cookerCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cookerCore.name = "CookerCore";
        cookerCore.transform.SetParent(station.transform, false);
        cookerCore.transform.localPosition = new Vector3(-0.55f, 1.15f, 0f);
        cookerCore.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
        cookerCore.GetComponent<Renderer>().sharedMaterial = accentMat;

        GameObject analyzer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        analyzer.name = "Analyzer";
        analyzer.transform.SetParent(station.transform, false);
        analyzer.transform.localPosition = new Vector3(0.55f, 1.18f, 0f);
        analyzer.transform.localScale = new Vector3(0.42f, 0.24f, 0.42f);
        analyzer.GetComponent<Renderer>().sharedMaterial = wallMat;

        Material foodMat = index % 3 == 0 ? foodMatA : index % 3 == 1 ? foodMatB : foodMatC;

        GameObject foodSample = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foodSample.name = "FoodSample";
        foodSample.transform.SetParent(station.transform, false);
        foodSample.transform.localPosition = new Vector3(0f, 1.32f, 0.12f);
        foodSample.transform.localScale = new Vector3(0.26f, 0.26f, 0.26f);
        foodSample.GetComponent<Renderer>().sharedMaterial = foodMat;
        foodSample.AddComponent<FloatAndRotate>();
    }

    private void CreateFoodDisplayPods(Transform parent)
    {
        Vector3[] podPositions =
        {
            new Vector3(-2.2f, 0f, 4.5f),
            new Vector3( 0.0f, 0f, 5.0f),
            new Vector3( 2.2f, 0f, 4.5f)
        };

        for (int i = 0; i < podPositions.Length; i++)
        {
            CreateSingleFoodPod(parent, podPositions[i], i);
        }
    }

    private void CreateSingleFoodPod(Transform parent, Vector3 pos, int index)
    {
        GameObject pod = new GameObject("FoodPod_" + index);
        pod.transform.SetParent(parent, false);
        pod.transform.localPosition = pos;

        GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePart.name = "PodBase";
        basePart.transform.SetParent(pod.transform, false);
        basePart.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        basePart.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
        basePart.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

        GameObject glassTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glassTube.name = "GlassTube";
        glassTube.transform.SetParent(pod.transform, false);
        glassTube.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        glassTube.transform.localScale = new Vector3(0.42f, 0.90f, 0.42f);
        glassTube.GetComponent<Renderer>().sharedMaterial = glassMat;
        DestroyCollider(glassTube);

        Material foodMat = index == 0 ? foodMatA : index == 1 ? foodMatB : foodMatC;
        PrimitiveType type = index == 0 ? PrimitiveType.Sphere : index == 1 ? PrimitiveType.Capsule : PrimitiveType.Cube;

        GameObject specimen = GameObject.CreatePrimitive(type);
        specimen.name = "Specimen";
        specimen.transform.SetParent(pod.transform, false);
        specimen.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        specimen.transform.localScale = index == 2 ? new Vector3(0.28f, 0.28f, 0.28f) : new Vector3(0.34f, 0.34f, 0.34f);
        specimen.GetComponent<Renderer>().sharedMaterial = foodMat;
        specimen.AddComponent<FloatAndRotate>();
    }

    private void CreateCeilingCore(Transform parent)
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "CeilingCore";
        core.transform.SetParent(parent, false);
        core.transform.localPosition = new Vector3(0f, roomHeight - 0.45f, 0f);
        core.transform.localScale = new Vector3(2.0f, 0.15f, 2.0f);
        core.GetComponent<Renderer>().sharedMaterial = accentMat;

        GameObject hanging = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hanging.name = "HangingEmitter";
        hanging.transform.SetParent(parent, false);
        hanging.transform.localPosition = new Vector3(0f, roomHeight - 1.0f, 0f);
        hanging.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
        hanging.GetComponent<Renderer>().sharedMaterial = accentMat;
    }

    private void CreateAmbientLights(Transform parent)
    {
        GameObject lightRoot = new GameObject("AmbientLights");
        lightRoot.transform.SetParent(parent, false);

        Light centerLight = lightRoot.AddComponent<Light>();
        centerLight.type = LightType.Point;
        centerLight.color = new Color(0.35f, 0.85f, 1f, 1f);
        centerLight.intensity = 14f;
        centerLight.range = 20f;
        centerLight.shadows = LightShadows.Soft;

        CreatePointLight(lightRoot.transform, new Vector3(-5f, 2.6f, -2f), new Color(0.20f, 0.90f, 1f), 6f, 10f);
        CreatePointLight(lightRoot.transform, new Vector3(5f, 2.6f, -2f), new Color(0.20f, 0.90f, 1f), 6f, 10f);
        CreatePointLight(lightRoot.transform, new Vector3(0f, 2.2f, 5f), new Color(0.35f, 1.00f, 0.45f), 5f, 9f);
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

    private Material CreateLitMaterial(string matName, Color baseColor, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;
        mat.color = baseColor;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        runtimeMaterials.Add(mat);
        return mat;
    }

    private Material CreateEmissiveMaterial(string matName, Color baseColor, Color emissionColor, float emissionIntensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;
        mat.color = baseColor;

        Color finalEmission = emissionColor * emissionIntensity;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", finalEmission);
        }

        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.15f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);

        runtimeMaterials.Add(mat);
        return mat;
    }

    private Material CreateTransparentMaterial(string matName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        if (mat.shader.name.Contains("Universal Render Pipeline"))
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
            mat.renderQueue = 3000;
        }

        runtimeMaterials.Add(mat);
        return mat;
    }

    private void DestroyCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }
}

public class FloatAndRotate : MonoBehaviour
{
    public float floatAmplitude = 0.08f;
    public float floatSpeed = 1.4f;
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    private Vector3 startPos;
    private float seed;

    private void Start()
    {
        startPos = transform.localPosition;
        seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float y = Mathf.Sin((Time.time + seed) * floatSpeed) * floatAmplitude;
        transform.localPosition = startPos + new Vector3(0f, y, 0f);
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}

public class SlowRotate : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0f, 8f, 0f);

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}
