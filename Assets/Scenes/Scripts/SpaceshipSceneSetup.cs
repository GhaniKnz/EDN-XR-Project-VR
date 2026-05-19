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

    // Matériaux créés au runtime
    private Material _skyMat, _starMat, _bodyMat, _accentMat, _engineMat, _asteroidMat, _foodMat;

    private void Start()
    {
        if (buildOnStart) Build();
    }

    [ContextMenu("Build Spaceship Scene")]
    public void Build()
    {
        EnsureEventSystem();
        CreateMaterials();

        Transform env = NewRoot("Environment");
        BuildSpaceSphere(env);
        BuildStars(env);
        BuildNebulae(env);
        BuildPlanets(env);
        SetupLighting();

        Transform shipRoot = NewRoot("ShipRoot");
        GameObject ship = BuildShip(shipRoot);

        GameObject foodTemplate    = BuildFoodTemplate();
        GameObject hazardTemplate  = BuildHazardTemplate();

        SetupGameManager(ship, foodTemplate, hazardTemplate);
        SetupCameraFollow(ship.transform);
        SetupHUD();
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
        _asteroidMat = MakeLit(lit, new Color(0.015f, 0.014f, 0.018f), 0.02f, 0.08f);
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

    private GameObject BuildFoodTemplate()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FoodTemplate";
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 0.75f;
        go.GetComponent<Renderer>().sharedMaterial = _foodMat;
        go.GetComponent<Collider>().isTrigger = true;
        go.AddComponent<SpaceFoodPickup>();
        go.SetActive(false);
        return go;
    }

    private GameObject BuildHazardTemplate()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HazardTemplate";
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 1.2f;
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

    private void SetupGameManager(GameObject ship, GameObject foodTemplate, GameObject hazardTemplate)
    {
        GameObject gmGO = new GameObject("SpaceGameManager");
        gmGO.transform.SetParent(transform, false);
        SpaceGameManager gm = gmGO.AddComponent<SpaceGameManager>();
        gm.Init(ship.transform, foodTemplate, hazardTemplate);
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
