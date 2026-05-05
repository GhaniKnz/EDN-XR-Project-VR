using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FoodItem : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float rotationSpeed = 40f;

    private Vector3 _startPos;
    private float _seed;
    private bool _collected;

    private static readonly Color[] FoodColors =
    {
        new Color(0.35f, 1f, 0.25f, 1f),
        new Color(1f, 0.85f, 0.2f, 1f),
        new Color(1f, 0.35f, 0.25f, 1f),
        new Color(0.8f, 0.35f, 1f, 1f)
    };

    private static readonly int[] FoodScores = { 10, 20, 35, 50 };

    private void Start()
    {
        _startPos = transform.position;
        _seed = Random.Range(0f, 10f);
        GetComponent<Collider>().isTrigger = true;
        ApplyRandomValueAndColor();
    }

    private void ApplyRandomValueAndColor()
    {
        int index = Random.Range(0, FoodColors.Length);
        scoreValue = FoodScores[index];

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material mat = renderer.material;
        Color color = FoodColors[index];
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.2f);
        }
    }

    private void Update()
    {
        if (_collected) return;
        float y = Mathf.Sin((Time.time + _seed) * floatSpeed) * floatAmplitude;
        transform.position = _startPos + new Vector3(0f, y, 0f);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void Collect()
    {
        if (_collected) return;
        _collected = true;
        GameManager.Instance?.AddScore(scoreValue);
        Destroy(gameObject);
    }

    public int ScoreValue => scoreValue;
}
