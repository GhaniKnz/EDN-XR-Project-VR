using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceGameManager : MonoBehaviour
{
    public static SpaceGameManager Instance { get; private set; }

    [Header("Vies")]
    public int maxLives = 5;

    [Header("Spawn")]
    public float spawnDistance = 80f;
    public float spawnWidth    = 22f;
    public float spawnHeight   = 14f;
    public int   initialFood   = 6;
    public int   initialHazards = 5;

    [Header("Vitesse")]
    public float startSpeed    = 14f;
    public float speedIncrease = 0.18f;
    public float maxSpeed      = 40f;

    // État public
    public int   Lives        { get; private set; }
    public int   Score        { get; private set; }
    public float ElapsedTime  { get; private set; }
    public float DistanceKm   { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool  IsGameOver   { get; private set; }

    // Événements
    public static event System.Action<int>   OnLivesChanged;
    public static event System.Action<int>   OnScoreChanged;
    public static event System.Action<float> OnTimeChanged;
    public static event System.Action<float> OnDistanceChanged;
    public static event System.Action<float> OnSpeedChanged;
    public static event System.Action        OnGameOver;

    // Internes
    private Transform  _ship;
    private GameObject _foodTemplate;
    private GameObject _hazardTemplate;
    private float _foodTimer;
    private float _hazardTimer;
    private float _foodInterval;
    private float _hazardInterval;
    private float _hazardSpeed;

    public void Init(Transform ship, GameObject foodTemplate, GameObject hazardTemplate)
    {
        _ship           = ship;
        _foodTemplate   = foodTemplate;
        _hazardTemplate = hazardTemplate;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        switch (GameData.Difficulty)
        {
            case DifficultyLevel.Easy:
                _foodInterval = 4.5f; _hazardInterval = 3.2f; _hazardSpeed = 9f;
                initialHazards = 3;   startSpeed = 12f;        speedIncrease = 0.12f;
                break;
            case DifficultyLevel.Hard:
                _foodInterval = 2f;   _hazardInterval = 1.1f;  _hazardSpeed = 18f;
                initialHazards = 12;  startSpeed = 18f;        speedIncrease = 0.25f;
                break;
            default:
                _foodInterval = 3f;   _hazardInterval = 1.8f;  _hazardSpeed = 13f;
                initialHazards = 6;   startSpeed = 14f;        speedIncrease = 0.18f;
                break;
        }
    }

    private void Start()
    {
        Lives        = maxLives;
        Score        = 0;
        ElapsedTime  = 0f;
        DistanceKm   = 0f;
        CurrentSpeed = startSpeed;

        OnLivesChanged?.Invoke(Lives);
        OnScoreChanged?.Invoke(Score);
        OnTimeChanged?.Invoke(0f);
        OnDistanceChanged?.Invoke(0f);
        OnSpeedChanged?.Invoke(CurrentSpeed);

        for (int i = 0; i < initialFood;    i++) SpawnFood();
        for (int i = 0; i < initialHazards; i++) SpawnHazard();
    }

    private void Update()
    {
        if (IsGameOver) return;

        ElapsedTime  += Time.deltaTime;
        CurrentSpeed  = Mathf.Min(maxSpeed, startSpeed + ElapsedTime * speedIncrease);
        DistanceKm   += CurrentSpeed * Time.deltaTime * 0.001f;

        OnTimeChanged?.Invoke(ElapsedTime);
        OnDistanceChanged?.Invoke(DistanceKm);
        OnSpeedChanged?.Invoke(CurrentSpeed);

        _foodTimer   += Time.deltaTime;
        _hazardTimer += Time.deltaTime;

        if (_foodTimer   >= _foodInterval)   { _foodTimer   = 0f; SpawnFood(); }
        if (_hazardTimer >= _hazardInterval) { _hazardTimer = 0f; SpawnHazard(); }
    }

    public void AddScore(int pts)
    {
        Score += pts;
        OnScoreChanged?.Invoke(Score);
    }

    public void TakeDamage()
    {
        if (IsGameOver) return;
        Lives = Mathf.Max(0, Lives - 1);
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0) TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        IsGameOver = true;
        OnGameOver?.Invoke();
        StartCoroutine(ReturnToMenu());
    }

    private IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("MainMenu");
    }

    private void SpawnFood()
    {
        if (_foodTemplate == null) return;
        Vector3 pos = GetSpawnPos(Random.Range(spawnDistance * 0.4f, spawnDistance * 0.8f));
        Instantiate(_foodTemplate, pos, Random.rotation).SetActive(true);
    }

    private void SpawnHazard()
    {
        if (_hazardTemplate == null) return;
        Vector3 spawnPos = GetSpawnPos(spawnDistance);
        Vector3 target   = _ship != null
            ? _ship.position + Random.insideUnitSphere * 5f
            : Vector3.zero;

        GameObject go = Instantiate(_hazardTemplate, spawnPos, Random.rotation);
        go.SetActive(true);
        go.GetComponent<SpaceHazardItem>()?.Init((target - spawnPos).normalized,
            _hazardSpeed + CurrentSpeed * 0.3f);
    }

    private Vector3 GetSpawnPos(float dist)
    {
        if (_ship == null)
            return new Vector3(
                Random.Range(-spawnWidth, spawnWidth),
                Random.Range(-spawnHeight * 0.5f, spawnHeight * 0.5f),
                dist);

        return _ship.position
             + _ship.forward  * dist
             + _ship.right    * Random.Range(-spawnWidth, spawnWidth)
             + Vector3.up     * Random.Range(-spawnHeight * 0.5f, spawnHeight * 0.5f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
