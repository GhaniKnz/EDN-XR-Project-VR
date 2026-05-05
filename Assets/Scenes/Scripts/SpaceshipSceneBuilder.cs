using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Builds the Spaceship gameplay scene: space environment, ship, HUD, game manager.
/// Attach to an empty GameObject in the Spaceship scene and call Build via the Inspector button or on Start.
/// </summary>
public class SpaceshipSceneBuilder : MonoBehaviour
{
    [Header("Auto-build")]
    [SerializeField] private bool buildOnStart = true;

    [Header("Environment")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private float skyboxRadius = 200f;
    [SerializeField] private int starCount = 200;
    [SerializeField] private Color skyboxColor = new Color(0.01f, 0.01f, 0.04f, 1f);
    [SerializeField] private Color starColor = new Color(0.85f, 0.92f, 1f, 1f);
    [SerializeField] private Color nebulaColorA = new Color(0.25f, 0.05f, 0.45f, 1f);
    [SerializeField] private Color nebulaColorB = new Color(0.04f, 0.18f, 0.45f, 1f);

    [Header("Ship Visual")]
    [SerializeField] private Color shipBodyColor = new Color(0.18f, 0.22f, 0.30f, 1f);
    [SerializeField] private Color shipAccentColor = new Color(0.25f, 0.85f, 1f, 1f);
    [SerializeField] private Color engineGlowColor = new Color(0.4f, 0.7f, 1f, 1f);

    [Header("XR Origin Reference")]
    [SerializeField] private Transform xrOrigin;

    [Header("Scene Cleanup")]
    [SerializeField] private bool cleanupHubScene = true;

    private Material _skyMat;
    private Material _starMat;
    private Material _shipBodyMat;
    private Material _shipAccentMat;
    private Material _engineMat;
    private Material _asteroidMat;
    private Material _foodMat;

    private void Start()
    {
        if (buildOnStart)
            Build();
    }

    [ContextMenu("Build Spaceship Scene")]
    public void Build()
    {
        if (cleanupHubScene)
            CleanupHubScene();

        ClearChildren();
        ApplySkybox();
        CreateMaterials();

        Transform envRoot = CreateRoot("Environment");
        if (skyboxMaterial == null)
            CreateSpaceSphere(envRoot);
        CreateStarField(envRoot);
        CreateNebula(envRoot);
        CreatePlanets(envRoot);
        CreateAmbientLighting(envRoot);

        Transform shipRoot = CreateRoot("Spaceship");
        GameObject ship = CreateShip(shipRoot);

        SetupGameManager(ship);
        SetupHUD(ship);
        SetupCamera(ship);
    }

    private void CleanupHubScene()
    {
        // Names of objects that should NOT be destroyed (XR infra)
        System.Collections.Generic.HashSet<string> keep = new System.Collections.Generic.HashSet<string>
        {
            "XR Interaction Setup",
            "XR Origin (XR Rig)",
            "XR Origin",
            "XR Interaction Manager",
            "Input Action Manager",
            "EventSystem",
            "SpaceshipSetup",
            "Main Camera",
            "Directional Light"
        };

        GameObject[] all = FindObjectsOfType<GameObject>(true);
        foreach (GameObject go in all)
        {
            if (go == null || go == gameObject) continue;
            if (go.transform.parent != null) continue; // only root
            if (keep.Contains(go.name)) continue;
            Destroy(go);
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private Transform CreateRoot(string rootName)
    {
        GameObject go = new GameObject(rootName);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    // ─── Materials ────────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        _skyMat = MakeMat(unlit, skyboxColor);
        _skyMat.name = "SkyMat";

        _starMat = MakeEmissive(lit, Color.black, starColor * 3f);
        _starMat.name = "StarMat";

        _shipBodyMat = MakeLit(lit, shipBodyColor, 0.6f, 0.7f);
        _shipBodyMat.name = "ShipBodyMat";

        _shipAccentMat = MakeEmissive(lit, new Color(0.05f, 0.15f, 0.22f), shipAccentColor * 2f);
        _shipAccentMat.name = "ShipAccentMat";

        _engineMat = MakeEmissive(lit, new Color(0.05f, 0.1f, 0.2f), engineGlowColor * 3f);
        _engineMat.name = "EngineMat";

        _asteroidMat = MakeLit(lit, new Color(0.015f, 0.014f, 0.018f), 0.02f, 0.08f);
        _asteroidMat.name = "AsteroidMat";

        _foodMat = MakeEmissive(lit, new Color(0.15f, 0.15f, 0.15f), Color.white * 1.5f);
        _foodMat.name = "FoodMat";
    }

    private void ApplySkybox()
    {
        if (skyboxMaterial == null)
            return;

        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.07f, 0.12f, 1f);
        DynamicGI.UpdateEnvironment();
    }

    // ─── Environment ─────────────────────────────────────────────────────────

    private void CreateSpaceSphere(Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "SpaceSkybox";
        sphere.transform.SetParent(parent, false);
        sphere.transform.localScale = Vector3.one * skyboxRadius;
        sphere.GetComponent<Renderer>().sharedMaterial = _skyMat;
        DestroyCollider(sphere);
    }

    private void CreateStarField(Transform parent)
    {
        Transform starsRoot = new GameObject("Stars").transform;
        starsRoot.SetParent(parent, false);

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star_" + i;
            star.transform.SetParent(starsRoot, false);

            Vector3 dir = Random.onUnitSphere;
            star.transform.localPosition = dir * Random.Range(skyboxRadius * 0.7f, skyboxRadius * 0.9f);
            float s = Random.Range(0.1f, 0.5f);
            star.transform.localScale = Vector3.one * s;
            star.GetComponent<Renderer>().sharedMaterial = _starMat;
            DestroyCollider(star);
        }
    }

    private void CreateNebula(Transform parent)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        for (int i = 0; i < 6; i++)
        {
            Material nebMat = MakeTransparent(s,
                i % 2 == 0 ? new Color(nebulaColorA.r, nebulaColorA.g, nebulaColorA.b, 0.035f)
                           : new Color(nebulaColorB.r, nebulaColorB.g, nebulaColorB.b, 0.04f));

            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "Nebula_" + i;
            cloud.transform.SetParent(parent, false);
            cloud.transform.localPosition = Random.onUnitSphere * Random.Range(60f, 100f);
            float s2 = Random.Range(30f, 60f);
            cloud.transform.localScale = new Vector3(s2 * Random.Range(0.8f, 1.4f), s2, s2 * Random.Range(0.8f, 1.4f));
            cloud.GetComponent<Renderer>().sharedMaterial = nebMat;
            DestroyCollider(cloud);
        }
    }

    private void CreatePlanets(Transform parent)
    {
        Color[] colors =
        {
            new Color(0.25f, 0.85f, 1f),
            new Color(1f, 0.45f, 0.85f),
            new Color(1f, 0.80f, 0.25f)
        };

        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        for (int i = 0; i < 3; i++)
        {
            Material pm = MakeEmissive(lit, colors[i] * 0.15f, colors[i] * 0.6f);
            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planet_" + i;
            planet.transform.SetParent(parent, false);
            planet.transform.localPosition = Random.onUnitSphere * Random.Range(80f, 140f);
            float sz = Random.Range(8f, 18f);
            planet.transform.localScale = Vector3.one * sz;
            planet.GetComponent<Renderer>().sharedMaterial = pm;
            planet.AddComponent<SlowRotate>().rotationSpeed = new Vector3(0f, Random.Range(4f, 12f), 0f);
            SphereCollider collider = planet.GetComponent<SphereCollider>();
            collider.isTrigger = false;

            Rigidbody rb = planet.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            planet.AddComponent<SpaceHazard>();
        }
    }

    private void CreateAmbientLighting(Transform parent)
    {
        // Directional light (dim space sun)
        GameObject sun = new GameObject("SunLight");
        sun.transform.SetParent(parent, false);
        sun.transform.localEulerAngles = new Vector3(45f, 30f, 0f);
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(0.9f, 0.85f, 0.7f, 1f);
        sunLight.intensity = 0.6f;
        sunLight.shadows = LightShadows.None;

        // Ambient fill (blue tint from space)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.07f, 0.12f, 1f);
        RenderSettings.fog = false;
    }

    // ─── Ship ─────────────────────────────────────────────────────────────────

    private GameObject CreateShip(Transform parent)
    {
        GameObject ship = new GameObject("Ship");
        ship.tag = "Player";
        ship.transform.SetParent(parent, false);
        ship.transform.localPosition = new Vector3(0f, 0f, 0f);

        // Rigidbody
        Rigidbody rb = ship.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2.5f;
        rb.angularDrag = 5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Solid ship bounds covering the body, wings, and nose.
        BoxCollider bc = ship.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0f, 0.25f);
        bc.size = new Vector3(6.0f, 1.6f, 5.4f);
        bc.isTrigger = false;

        // Ship mesh root (for visual tilt)
        GameObject meshRoot = new GameObject("ShipMesh");
        meshRoot.transform.SetParent(ship.transform, false);

        BuildShipMesh(meshRoot.transform);

        // Fire point at the front of the ship
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(meshRoot.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0f, 2.8f);

        // Controller & Shooter
        SpaceshipController ctrl = ship.AddComponent<SpaceshipController>();
        SetPrivateField(ctrl, "shipMesh", meshRoot.transform);

        SpaceshipShooter shooter = ship.AddComponent<SpaceshipShooter>();
        SetPrivateField(shooter, "firePoint", firePoint.transform);

        return ship;
    }

    private void BuildShipMesh(Transform parent)
    {
        // Fuselage (main body)
        Prim("Fuselage", PrimitiveType.Capsule, parent,
            new Vector3(0f, 0f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.7f, 2.2f, 0.7f),
            _shipBodyMat);

        // Cockpit (front dome)
        Prim("Cockpit", PrimitiveType.Sphere, parent,
            new Vector3(0f, 0.15f, 1.5f),
            Vector3.zero,
            new Vector3(0.7f, 0.5f, 0.8f),
            _shipAccentMat);

        // Left wing
        Prim("WingL", PrimitiveType.Cube, parent,
            new Vector3(-1.8f, 0f, -0.2f),
            new Vector3(0f, 0f, 8f),
            new Vector3(2.0f, 0.12f, 1.4f),
            _shipBodyMat);

        // Right wing
        Prim("WingR", PrimitiveType.Cube, parent,
            new Vector3(1.8f, 0f, -0.2f),
            new Vector3(0f, 0f, -8f),
            new Vector3(2.0f, 0.12f, 1.4f),
            _shipBodyMat);

        // Left engine
        Prim("EngineL", PrimitiveType.Cylinder, parent,
            new Vector3(-1.4f, 0f, -1.4f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.28f, 0.6f, 0.28f),
            _shipBodyMat);

        // Right engine
        Prim("EngineR", PrimitiveType.Cylinder, parent,
            new Vector3(1.4f, 0f, -1.4f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.28f, 0.6f, 0.28f),
            _shipBodyMat);

        // Engine glow (left)
        Prim("GlowL", PrimitiveType.Sphere, parent,
            new Vector3(-1.4f, 0f, -2.1f),
            Vector3.zero,
            new Vector3(0.26f, 0.26f, 0.26f),
            _engineMat);

        // Engine glow (right)
        Prim("GlowR", PrimitiveType.Sphere, parent,
            new Vector3(1.4f, 0f, -2.1f),
            Vector3.zero,
            new Vector3(0.26f, 0.26f, 0.26f),
            _engineMat);

        // Accent strip along fuselage
        Prim("Stripe", PrimitiveType.Cube, parent,
            new Vector3(0f, 0.36f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.08f, 3.8f, 0.08f),
            _shipAccentMat);
    }

    // ─── Gameplay objects ─────────────────────────────────────────────────────

    private void SetupGameManager(GameObject ship)
    {
        GameObject gmGO = new GameObject("GameManager");
        gmGO.transform.SetParent(transform, false);
        GameManager gm = gmGO.AddComponent<GameManager>();

        // Create food prefab and asteroid prefab as simple primitive GameObjects
        FoodItem foodPrefab = CreateFoodPrefab();
        AsteroidController asteroidPrefab = CreateAsteroidPrefab();

        SetPrivateField(gm, "foodPrefab", foodPrefab);
        SetPrivateField(gm, "asteroidPrefab", asteroidPrefab);
        SetPrivateField(gm, "playerShip", ship.transform);
    }

    private FoodItem CreateFoodPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FoodItem_Prefab";
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        go.transform.localScale = Vector3.one * 0.8f;
        go.GetComponent<Renderer>().sharedMaterial = _foodMat;

        SphereCollider sc = go.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        return go.AddComponent<FoodItem>();
    }

    private AsteroidController CreateAsteroidPrefab()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Asteroid_Prefab";
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        float s = Random.Range(0.8f, 2.0f);
        go.transform.localScale = new Vector3(s * 0.8f, s, s * 0.9f);
        go.GetComponent<Renderer>().sharedMaterial = _asteroidMat;

        SphereCollider sc = go.GetComponent<SphereCollider>();
        sc.isTrigger = false;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        return go.AddComponent<AsteroidController>();
    }

    private void SetupHUD(GameObject ship)
    {
        GameObject hudGO = new GameObject("SpaceHUD");
        hudGO.transform.SetParent(transform, false);
        hudGO.transform.localPosition = new Vector3(0f, 0f, 3f);
        hudGO.AddComponent<SpaceHUD>();
    }

    private void SetupCamera(GameObject ship)
    {
        // If xrOrigin is assigned, add ShipCameraFollow
        if (xrOrigin == null)
            xrOrigin = FindXROrigin();

        if (xrOrigin != null)
        {
            ShipCameraFollow follow = xrOrigin.GetComponent<ShipCameraFollow>();
            if (follow == null)
                follow = xrOrigin.gameObject.AddComponent<ShipCameraFollow>();
            follow.SetTarget(ship.transform);
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            ShipCameraFollow follow = cam.GetComponent<ShipCameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<ShipCameraFollow>();
            follow.SetTarget(ship.transform);
        }
    }

    private Transform FindXROrigin()
    {
        // Move the entire XR Interaction Setup root so camera follows ship
        GameObject xrSetup = GameObject.Find("XR Interaction Setup");
        if (xrSetup != null) return xrSetup.transform;

        // Fallback: find any root-level camera rig
        GameObject xrOriginGO = GameObject.Find("XR Origin (XR Rig)");
        if (xrOriginGO != null)
            return xrOriginGO.transform.parent != null ? xrOriginGO.transform.parent : xrOriginGO.transform;

        return null;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void Prim(string objName, PrimitiveType type, Transform parent,
        Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
    }

    private Material MakeMat(Shader shader, Color color)
    {
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        return m;
    }

    private Material MakeLit(Shader shader, Color baseColor, float metallic, float smoothness)
    {
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private Material MakeEmissive(Shader shader, Color baseColor, Color emission)
    {
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        return m;
    }

    private Material MakeTransparent(Shader shader, Color color)
    {
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.shader.name.Contains("Universal Render Pipeline"))
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        return m;
    }

    private void DestroyCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

#if UNITY_EDITOR
    private void MarkSceneDirty()
    {
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
