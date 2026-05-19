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

    private const float FloatSpeed = 1.2f;
    private const float FloatAmp   = 0.3f;
    private const float RotSpeed   = 40f;

    private void Start()
    {
        _startPos = transform.position;
        _seed = Random.Range(0f, 10f);
        GetComponent<Collider>().isTrigger = true;

        int idx = Random.Range(0, Colors.Length);
        _score = Scores[idx];
        ApplyColor(Colors[idx]);
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
    }

    public void Collect()
    {
        if (_collected) return;
        _collected = true;
        SpaceGameManager.Instance?.AddScore(_score);
        Destroy(gameObject);
    }
}
