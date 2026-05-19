using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpaceFoodPickup : MonoBehaviour
{
    private static readonly Color[] Colors =
    {
        new Color(0.35f, 1f,    0.25f),
        new Color(1f,    0.85f, 0.2f),
        new Color(1f,    0.35f, 0.25f),
        new Color(0.8f,  0.35f, 1f)
    };
    private static readonly int[] Scores = { 10, 20, 35, 50 };

    private int   _score;
    private bool  _collected;
    private Vector3 _startPos;
    private float _seed;
    [SerializeField] private bool _growsShipOnCollision;
    [SerializeField] private float _shipGrowthAmount = 0.14f;
    [SerializeField] private int _maxShotHealth = 1;

    private int _shotHealth;
    private Transform _healthBarRoot;
    private Transform _healthFill;

    private const float FloatSpeed = 1.2f;
    private const float FloatAmp   = 0.3f;
    private const float RotSpeed   = 40f;

    private void Start()
    {
        _startPos = transform.position;
        _seed = Random.Range(0f, 10f);
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;

        int idx = Random.Range(0, Colors.Length);
        _score = Scores[idx];
        ApplyColor(Colors[idx]);

        _shotHealth = Mathf.Max(1, _shotHealth > 0 ? _shotHealth : _maxShotHealth);
        if (_maxShotHealth > 1)
            BuildHealthBar();
    }

    private void ApplyColor(Color c)
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;
        Material m = r.material;
        if (m.HasProperty("_BaseColor"))    m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))        m.SetColor("_Color", c);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
        }
    }

    private void Update()
    {
        if (_collected) return;
        transform.position = _startPos + Vector3.up * Mathf.Sin((Time.time + _seed) * FloatSpeed) * FloatAmp;
        transform.Rotate(Vector3.up, RotSpeed * Time.deltaTime, Space.World);

        if (_healthBarRoot != null && Camera.main != null)
            _healthBarRoot.rotation = Camera.main.transform.rotation;

        CleanupWhenBehindShip();
    }

    public void Collect()
    {
        if (_collected) return;
        _shotHealth = 0;
        FinishCollect();
    }

    public void HitByShot()
    {
        if (_collected) return;

        if (_shotHealth <= 0)
            _shotHealth = Mathf.Max(1, _maxShotHealth);

        _shotHealth--;
        UpdateHealthBar();

        if (_shotHealth <= 0)
            FinishCollect();
    }

    private void FinishCollect()
    {
        _collected = true;
        SpaceGameManager.Instance?.AddScore(_score);
        Destroy(gameObject);
    }

    public void Configure(bool growsShipOnCollision, float shipGrowthAmount)
    {
        _growsShipOnCollision = growsShipOnCollision;
        _shipGrowthAmount = shipGrowthAmount;
    }

    public void ConfigureShotHealth(int requiredShots)
    {
        _maxShotHealth = Mathf.Max(1, requiredShots);
        _shotHealth = _maxShotHealth;
    }

    public bool GrowsShipOnCollision => _growsShipOnCollision;

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag("Player")) return;

        if (_growsShipOnCollision)
            GrowShip(other.transform);

        _collected = true;
        SpaceGameManager.Instance?.TakeDamage();
        Destroy(gameObject);
    }

    private void GrowShip(Transform ship)
    {
        const float maxShipScale = 2.6f;
        Vector3 current = ship.localScale;
        float target = Mathf.Min(maxShipScale, current.x + _shipGrowthAmount);
        ship.localScale = Vector3.one * target;
    }

    private void BuildHealthBar()
    {
        if (_healthBarRoot != null) return;

        _healthBarRoot = new GameObject("FoodHealthBar").transform;
        _healthBarRoot.SetParent(transform, false);
        _healthBarRoot.localPosition = Vector3.up * 1.35f;

        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Back";
        back.transform.SetParent(_healthBarRoot, false);
        back.transform.localScale = new Vector3(1.2f, 0.12f, 0.025f);
        SetBarMaterial(back, new Color(0.02f, 0.02f, 0.025f, 0.92f));
        DisableCollider(back);

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Fill";
        fill.transform.SetParent(_healthBarRoot, false);
        fill.transform.localPosition = new Vector3(-0.54f, 0f, -0.02f);
        fill.transform.localScale = new Vector3(1.08f, 0.08f, 0.035f);
        SetBarMaterial(fill, new Color(0.25f, 1f, 0.35f, 0.95f));
        DisableCollider(fill);
        _healthFill = fill.transform;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_healthFill == null || _maxShotHealth <= 1) return;

        float pct = Mathf.Clamp01(_shotHealth / (float)_maxShotHealth);
        float width = 1.08f * pct;
        _healthFill.localScale = new Vector3(width, 0.08f, 0.035f);
        _healthFill.localPosition = new Vector3(-0.54f + width * 0.5f, 0f, -0.02f);
    }

    private static void SetBarMaterial(GameObject go, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Standard");
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null || shader == null) return;

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        renderer.sharedMaterial = material;
    }

    private static void DisableCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    private void CleanupWhenBehindShip()
    {
        Transform ship = SpaceGameManager.Instance != null ? SpaceGameManager.Instance.Ship : null;
        if (ship == null) return;

        Vector3 toFood = transform.position - ship.position;
        float forwardDistance = Vector3.Dot(toFood, ship.forward);
        if (forwardDistance < -45f || toFood.sqrMagnitude > 260f * 260f)
            Destroy(gameObject);
    }
}
