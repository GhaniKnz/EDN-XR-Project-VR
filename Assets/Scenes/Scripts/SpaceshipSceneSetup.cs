using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Construit toute la scène Spaceship au démarrage.
/// À attacher sur un GameObject vide dans la scène Spaceship.unity.
/// Ne requiert aucune assignation dans l'Inspector.
///
/// Différences clés vs ancienne version :
/// - Zéro DestroyImmediate → on désactive les colliders visuels (c.enabled = false)
/// - Zéro reflection → Init() publics pour passer les références
/// - UI WorldSpace uniquement (visible sur casque)
/// - Camera VR : XR root suit la position du vaisseau, pas la rotation
/// </summary>
public class SpaceshipSceneSetup : MonoBehaviour
{
    [Header("Auto-build")]
    [SerializeField] private bool buildOnStart = true;

    [Header("Environnement")]
    [SerializeField] private int   starCount = 180;
    [SerializeField] private float skyRadius = 200f;

    [Header("Couleurs")]
    [SerializeField] private Color skyColor        = new Color(0.005f, 0.005f, 0.02f);
    [SerializeField] private Color shipBodyColor   = new Color(0.18f,  0.22f,  0.30f);
    [SerializeField] private Color shipAccentColor = new Color(0.25f,  0.85f,  1f);
    [SerializeField] private Color engineGlowColor = new Color(0.4f,   0.7f,   1f);

    [Header("Food Prefabs")]
    [SerializeField] private GameObject[] foodPrefabs;
    [SerializeField] private float foodPrefabScale = 1f;
    [SerializeField] private float proceduralFoodScale = 1.35f;

    // Matériaux créés au runtime
    private Material _skyMat, _starMat, _bodyMat, _accentMat, _engineMat, _asteroidMat, _foodMat;

    private void Start()
    {
        if (buildOnStart) Build();
    }

    [ContextMenu("Build Spaceship Scene")]
    public void Build()
    {
        CleanupGeneratedScene();
        EnsureEventSystem();
        CreateMaterials();

        Transform env = NewRoot("Environment");
        BuildSpaceSphere(env);
        BuildStars(env);
        SetupLighting();

        Transform shipRoot = NewRoot("ShipRoot");
        GameObject ship = BuildShip(shipRoot);
        env.gameObject.AddComponent<SpaceEnvironmentFollow>().Init(ship.transform);

        GameObject[] foodTemplates = BuildFoodTemplates();
        GameObject hazardTemplate  = BuildHazardTemplate();

        SetupGameManager(ship, foodTemplates, hazardTemplate);
        SetupCameraFollow(ship.transform);
        SetupHUD();
    }

    private void CleanupGeneratedScene()
    {
        string[] generatedRoots = { "Environment", "ShipRoot", "SpaceGameManager" };
        foreach (string rootName in generatedRoots)
        {
            GameObject existing = GameObject.Find(rootName);
            if (existing != null)
                Destroy(existing);
        }
    }

    // ─── Matériaux ────────────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        Shader lit   = Shader.Find("Universal Render Pipeline/Lit")   ?? Shader.Find("Standard");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? lit;

        _skyMat      = MakeMat(unlit, skyColor);
        _starMat     = MakeEmissive(lit, Color.black, new Color(0.85f, 0.92f, 1f) * 3f);
        _bodyMat     = MakeLit(lit, shipBodyColor, 0.65f, 0.7f);
        _accentMat   = MakeEmissive(lit, new Color(0.05f, 0.15f, 0.22f), shipAccentColor * 2f);
        _engineMat   = MakeEmissive(lit, new Color(0.05f, 0.1f,  0.2f),  engineGlowColor * 3f);
        _asteroidMat = MakeLit(lit, new Color(0.30f, 0.24f, 0.20f), 0.02f, 0.16f);
        _foodMat     = MakeEmissive(lit, new Color(0.15f, 0.15f, 0.15f), Color.white * 1.5f);
    }

    // ─── Environnement ────────────────────────────────────────────────────────────

    private void BuildSpaceSphere(Transform parent)
    {
        Prim("SkySphere", PrimitiveType.Sphere, parent,
            Vector3.zero, Vector3.zero, Vector3.one * skyRadius, _skyMat, keepCollider: false);
    }

    private void BuildStars(Transform parent)
    {
        Transform root = new GameObject("Stars").transform;
        root.SetParent(parent, false);
        for (int i = 0; i < starCount; i++)
        {
            Prim("Star_" + i, PrimitiveType.Sphere, root,
                Random.onUnitSphere * Random.Range(skyRadius * 0.65f, skyRadius * 0.88f),
                Vector3.zero, Vector3.one * Random.Range(0.08f, 0.45f), _starMat, keepCollider: false);
        }
    }

    private void BuildNebulae(Transform parent)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Color[] cols =
        {
            new Color(0.25f, 0.05f, 0.45f, 0.035f),
            new Color(0.04f, 0.18f, 0.45f, 0.040f)
        };
        for (int i = 0; i < 6; i++)
        {
            Material m = MakeTransparent(s, cols[i % 2]);
            float sz   = Random.Range(28f, 56f);
            Prim("Nebula_" + i, PrimitiveType.Sphere, parent,
                Random.onUnitSphere * Random.Range(55f, 95f),
                Vector3.zero,
                new Vector3(sz * Random.Range(0.8f, 1.4f), sz, sz * Random.Range(0.8f, 1.4f)),
                m, keepCollider: false);
        }
    }

    private void BuildPlanets(Transform parent)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Color[] cols = { new Color(0.25f, 0.85f, 1f), new Color(1f, 0.45f, 0.85f), new Color(1f, 0.80f, 0.25f) };
        for (int i = 0; i < 3; i++)
        {
            Material pm = MakeEmissive(lit, cols[i] * 0.15f, cols[i] * 0.6f);
            float sz    = Random.Range(8f, 18f);
            GameObject p = Prim("Planet_" + i, PrimitiveType.Sphere, parent,
                Random.onUnitSphere * Random.Range(80f, 140f),
                Vector3.zero, Vector3.one * sz, pm, keepCollider: true);
            p.AddComponent<SlowRotate>().rotationSpeed = new Vector3(0f, Random.Range(4f, 12f), 0f);
        }
    }

    private void SetupLighting()
    {
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.07f, 0.12f);
        RenderSettings.fog          = false;
    }

    // ─── Vaisseau ─────────────────────────────────────────────────────────────────

    private GameObject BuildShip(Transform parent)
    {
        GameObject ship = new GameObject("Ship");
        ship.tag = "Player";
        ship.transform.SetParent(parent, false);

        Rigidbody rb = ship.AddComponent<Rigidbody>();
        rb.useGravity              = false;
        rb.drag                    = 2.5f;
        rb.angularDrag             = 8f;
        rb.collisionDetectionMode  = CollisionDetectionMode.ContinuousDynamic;

        // Collider de jeu couvrant fuselage + ailes
        BoxCollider bc = ship.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0f, 0.25f);
        bc.size   = new Vector3(6f, 1.6f, 5.4f);

        // Root visuel (tilte lors du strafe, pas de collider)
        GameObject meshRoot = new GameObject("ShipMesh");
        meshRoot.transform.SetParent(ship.transform, false);
        BuildShipMesh(meshRoot.transform);

        // Point de tir au nez du vaisseau
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(meshRoot.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0f, 2.8f);

        // Scripts — Init() public, zéro reflection
        SpaceshipMover mover = ship.AddComponent<SpaceshipMover>();
        mover.Init(meshRoot.transform);

        ship.AddComponent<SpaceShipCollisionDamage>();

        SpaceBeamShooter shooter = ship.AddComponent<SpaceBeamShooter>();
        shooter.Init(firePoint.transform);

        return ship;
    }

    private void BuildShipMesh(Transform parent)
    {
        Prim("Fuselage", PrimitiveType.Capsule,  parent, new Vector3(0,0,0),      new Vector3(90,0,0),  new Vector3(0.7f,2.2f,0.7f), _bodyMat,   keepCollider: false);
        Prim("Cockpit",  PrimitiveType.Sphere,   parent, new Vector3(0,0.15f,1.5f), Vector3.zero,        new Vector3(0.7f,0.5f,0.8f), _accentMat, keepCollider: false);
        Prim("WingL",    PrimitiveType.Cube,      parent, new Vector3(-1.8f,0,-0.2f), new Vector3(0,0,8),  new Vector3(2f,0.12f,1.4f),  _bodyMat,   keepCollider: false);
        Prim("WingR",    PrimitiveType.Cube,      parent, new Vector3( 1.8f,0,-0.2f), new Vector3(0,0,-8), new Vector3(2f,0.12f,1.4f),  _bodyMat,   keepCollider: false);
        Prim("EngineL",  PrimitiveType.Cylinder,  parent, new Vector3(-1.4f,0,-1.4f), new Vector3(90,0,0), new Vector3(0.28f,0.6f,0.28f),_bodyMat,  keepCollider: false);
        Prim("EngineR",  PrimitiveType.Cylinder,  parent, new Vector3( 1.4f,0,-1.4f), new Vector3(90,0,0), new Vector3(0.28f,0.6f,0.28f),_bodyMat,  keepCollider: false);
        Prim("GlowL",    PrimitiveType.Sphere,    parent, new Vector3(-1.4f,0,-2.1f), Vector3.zero,        Vector3.one * 0.26f,          _engineMat, keepCollider: false);
        Prim("GlowR",    PrimitiveType.Sphere,    parent, new Vector3( 1.4f,0,-2.1f), Vector3.zero,        Vector3.one * 0.26f,          _engineMat, keepCollider: false);
        Prim("Stripe",   PrimitiveType.Cube,      parent, new Vector3(0,0.36f,0),     new Vector3(90,0,0), new Vector3(0.08f,3.8f,0.08f),_accentMat, keepCollider: false);
    }

    // ─── Templates food / astéroïde ───────────────────────────────────────────────

    private GameObject[] BuildFoodTemplates()
    {
        List<GameObject> templates = new List<GameObject>();

        if (foodPrefabs != null)
        {
            foreach (GameObject prefab in foodPrefabs)
            {
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                instance.name = prefab.name + "_FoodTemplate";
                instance.transform.localScale *= foodPrefabScale;
                bool isBurger = instance.name.ToLowerInvariant().Contains("burger");
                PrepareFoodTemplate(instance, isBurger, isBurger ? 0.18f : 0f);
                templates.Add(instance);
            }
        }

        if (templates.Count == 0)
        {
            templates.Add(BuildBurgerTemplate());
            templates.Add(BuildPizzaTemplate());
            templates.Add(BuildDonutTemplate());
            templates.Add(BuildEnergyCanTemplate());
        }

        return templates.ToArray();
    }

    private void PrepareFoodTemplate(GameObject go, bool growsShipOnCollision, float shipGrowthAmount)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.85f;
            collider = sphere;
        }

        collider.isTrigger = true;

        SpaceFoodPickup pickup = go.GetComponent<SpaceFoodPickup>();
        if (pickup == null)
            pickup = go.AddComponent<SpaceFoodPickup>();
        pickup.Configure(growsShipOnCollision, shipGrowthAmount);

        go.SetActive(false);
    }

    private GameObject BuildBurgerTemplate()
    {
        GameObject root = NewFoodRoot("BurgerFoodTemplate", 0.95f);

        Material bunMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.95f, 0.62f, 0.26f), 0f, 0.35f);
        Material pattyMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.22f, 0.09f, 0.04f), 0f, 0.22f);
        Material cheeseMat = MakeEmissive(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(1f, 0.72f, 0.08f), new Color(1f, 0.35f, 0.04f));
        Material saladMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.25f, 0.85f, 0.18f), 0f, 0.25f);

        Prim("TopBun", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.33f, 0f), Vector3.zero, new Vector3(0.9f, 0.28f, 0.9f), bunMat, false);
        Prim("Salad", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.82f, 0.08f, 0.82f), saladMat, false);
        Prim("Cheese", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0f, 45f, 0f), new Vector3(1f, 0.08f, 1f), cheeseMat, false);
        Prim("Patty", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.12f, 0f), Vector3.zero, new Vector3(0.78f, 0.16f, 0.78f), pattyMat, false);
        Prim("BottomBun", PrimitiveType.Sphere, root.transform, new Vector3(0f, -0.31f, 0f), Vector3.zero, new Vector3(0.84f, 0.2f, 0.84f), bunMat, false);

        PrepareFoodTemplate(root, growsShipOnCollision: true, shipGrowthAmount: 0.18f);
        return root;
    }

    private GameObject BuildPizzaTemplate()
    {
        GameObject root = NewFoodRoot("PizzaSliceFoodTemplate", 1f);

        Material crustMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.9f, 0.54f, 0.2f), 0f, 0.28f);
        Material cheeseMat = MakeEmissive(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(1f, 0.78f, 0.18f), new Color(1f, 0.35f, 0.05f));
        Material pepperoniMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.78f, 0.12f, 0.08f), 0f, 0.25f);

        Prim("Slice", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0f), new Vector3(0f, 25f, 0f), new Vector3(0.95f, 0.09f, 1.3f), cheeseMat, false);
        Prim("Crust", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.05f, -0.62f), new Vector3(90f, 0f, 0f), new Vector3(0.52f, 0.16f, 0.52f), crustMat, false);
        Prim("Pepperoni_A", PrimitiveType.Sphere, root.transform, new Vector3(-0.24f, 0.12f, 0.08f), Vector3.zero, Vector3.one * 0.16f, pepperoniMat, false);
        Prim("Pepperoni_B", PrimitiveType.Sphere, root.transform, new Vector3(0.22f, 0.12f, 0.28f), Vector3.zero, Vector3.one * 0.14f, pepperoniMat, false);
        Prim("Pepperoni_C", PrimitiveType.Sphere, root.transform, new Vector3(0.08f, 0.12f, -0.26f), Vector3.zero, Vector3.one * 0.13f, pepperoniMat, false);

        PrepareFoodTemplate(root, growsShipOnCollision: false, shipGrowthAmount: 0f);
        return root;
    }

    private GameObject BuildDonutTemplate()
    {
        GameObject root = NewFoodRoot("DonutFoodTemplate", 0.92f);

        Material doughMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.82f, 0.46f, 0.2f), 0f, 0.32f);
        Material glazeMat = MakeEmissive(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(1f, 0.35f, 0.68f), new Color(1f, 0.12f, 0.45f) * 1.5f);

        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * 0.38f, 0f, Mathf.Sin(a) * 0.38f);
            Prim("Dough_" + i, PrimitiveType.Sphere, root.transform, pos, Vector3.zero, new Vector3(0.34f, 0.18f, 0.34f), doughMat, false);
        }

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * 0.38f, 0.12f, Mathf.Sin(a) * 0.38f);
            Prim("Glaze_" + i, PrimitiveType.Sphere, root.transform, pos, Vector3.zero, new Vector3(0.22f, 0.07f, 0.22f), glazeMat, false);
        }

        PrepareFoodTemplate(root, growsShipOnCollision: false, shipGrowthAmount: 0f);
        return root;
    }

    private GameObject BuildEnergyCanTemplate()
    {
        GameObject root = NewFoodRoot("EnergyCanFoodTemplate", 0.9f);

        Material canMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.08f, 0.12f, 0.18f), 0.75f, 0.65f);
        Material labelMat = MakeEmissive(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.05f, 0.65f, 1f), new Color(0.05f, 0.55f, 1f) * 2f);
        Material capMat = MakeLit(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"), new Color(0.78f, 0.86f, 0.92f), 0.8f, 0.7f);

        Prim("CanBody", PrimitiveType.Cylinder, root.transform, Vector3.zero, Vector3.zero, new Vector3(0.38f, 0.82f, 0.38f), canMat, false);
        Prim("Label", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -0.39f), Vector3.zero, new Vector3(0.52f, 0.58f, 0.035f), labelMat, false);
        Prim("TopCap", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.44f, 0f), Vector3.zero, new Vector3(0.4f, 0.05f, 0.4f), capMat, false);
        Prim("BottomCap", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.44f, 0f), Vector3.zero, new Vector3(0.4f, 0.05f, 0.4f), capMat, false);

        PrepareFoodTemplate(root, growsShipOnCollision: false, shipGrowthAmount: 0f);
        return root;
    }

    private GameObject NewFoodRoot(string name, float colliderRadius)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * proceduralFoodScale;
        SphereCollider collider = go.AddComponent<SphereCollider>();
        collider.radius = colliderRadius;
        collider.isTrigger = true;
        return go;
    }

    private GameObject BuildHazardTemplate()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HazardTemplate";
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 0.85f;
        go.GetComponent<Renderer>().sharedMaterial = _asteroidMat;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity              = false;
        rb.isKinematic             = true;
        rb.collisionDetectionMode  = CollisionDetectionMode.ContinuousSpeculative;

        go.AddComponent<SpaceHazardItem>();
        go.SetActive(false);
        return go;
    }

    // ─── Systèmes ─────────────────────────────────────────────────────────────────

    private void SetupGameManager(GameObject ship, GameObject[] foodTemplates, GameObject hazardTemplate)
    {
        GameObject gmGO = new GameObject("SpaceGameManager");
        gmGO.transform.SetParent(transform, false);
        SpaceGameManager gm = gmGO.AddComponent<SpaceGameManager>();
        gm.Init(ship.transform, foodTemplates, hazardTemplate);
    }

    private void SetupCameraFollow(Transform ship)
    {
        Transform xrRoot = null;

        // Priorité : trouver le XROrigin par composant (indépendant du nom du GO)
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        if (origin != null)
        {
            xrRoot = origin.transform;
            Debug.Log("[SpaceshipSceneSetup] XROrigin trouvé : " + xrRoot.name);
        }

        // Fallback : recherche par nom
        if (xrRoot == null)
            xrRoot = FindXRRoot();

        if (xrRoot == null)
        {
            Debug.LogWarning("[SpaceshipSceneSetup] XR root introuvable. La caméra ne suivra pas le vaisseau.");
            return;
        }

        XRShipFollow follower = xrRoot.GetComponent<XRShipFollow>();
        if (follower == null) follower = xrRoot.gameObject.AddComponent<XRShipFollow>();
        follower.Init(ship);
    }

    private void SetupHUD()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[SpaceshipSceneSetup] Camera.main introuvable. HUD non créé.");
            return;
        }
        SpaceGameHUD hud = cam.GetComponent<SpaceGameHUD>();
        if (hud == null) hud = cam.gameObject.AddComponent<SpaceGameHUD>();
        hud.Init(cam);
    }

    // ─── Trouver le root XR ───────────────────────────────────────────────────────

    private Transform FindXRRoot()
    {
        string[] candidates = { "XR Origin (XR Rig)", "XR Origin", "XR Interaction Setup", "XRRig", "OVRCameraRig" };
        foreach (string n in candidates)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) return go.transform;
        }
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    // ─── EventSystem ─────────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<XRUIInputModule>();
    }

    // ─── Helpers primitives ───────────────────────────────────────────────────────

    private Transform NewRoot(string n)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    /// <summary>
    /// Crée une primitive Unity.
    /// keepCollider=false → désactive le collider (pas de DestroyImmediate).
    /// </summary>
    private GameObject Prim(string n, PrimitiveType type, Transform parent,
        Vector3 pos, Vector3 euler, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale       = scale;

        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;

        if (!keepCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = false; // Désactivé, jamais DestroyImmediate
        }
        return go;
    }

    // ─── Helpers matériaux ────────────────────────────────────────────────────────

    private static Material MakeMat(Shader s, Color c)
    {
        Material m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))     m.SetColor("_Color",     c);
        return m;
    }

    private static Material MakeLit(Shader s, Color c, float metallic, float smooth)
    {
        Material m = MakeMat(s, c);
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    private static Material MakeEmissive(Shader s, Color baseCol, Color emission)
    {
        Material m = MakeMat(s, baseCol);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        return m;
    }

    private static Material MakeTransparent(Shader s, Color c)
    {
        Material m = MakeMat(s, c);
        if (m.shader.name.Contains("Universal Render Pipeline"))
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend"))   m.SetFloat("_Blend",   0f);
            if (m.HasProperty("_ZWrite"))  m.SetFloat("_ZWrite",  0f);
            m.renderQueue = 3000;
        }
        return m;
    }
}

class SpaceShipCollisionDamage : MonoBehaviour
{
    private float _lastDamageTime;
    private float _readyTime;

    private void Start()
    {
        _readyTime = Time.time + 1.5f;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider other)
    {
        if (Time.time < _readyTime) return;
        if (other == null || other.transform.IsChildOf(transform)) return;
        if (other.GetComponentInParent<XROrigin>() != null) return;
        if (Camera.main != null && other.transform.root == Camera.main.transform.root) return;
        if (other.GetComponentInParent<SpaceFoodPickup>() != null) return;
        if (other.GetComponentInParent<SpaceHazardItem>() != null) return;
        if (Time.time - _lastDamageTime < 0.4f) return;

        _lastDamageTime = Time.time;
        SpaceGameManager.Instance?.TakeDamage();
    }
}

class SpaceEnvironmentFollow : MonoBehaviour
{
    private Transform _target;

    public void Init(Transform target)
    {
        _target = target;
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        transform.position = _target.position;
    }
}
