using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpaceGameManager : MonoBehaviour
{
    public static SpaceGameManager Instance { get; private set; }
    private const string BestScorePrefsKey = "SpaceFood_BestScore";

    [Header("Vies")]
    public int maxLives = 5;

    [Header("Spawn")]
    public float spawnDistance = 80f;
    public float spawnWidth    = 38f;
    public float spawnHeight   = 24f;
    public int   initialFood   = 14;
    public int   initialHazards = 5;

    [Header("Vitesse")]
    public float startSpeed    = 14f;
    public float speedIncrease = 0.18f;
    public float maxSpeed      = 40f;

    // État public
    public int   Lives        { get; private set; }
    public int   Score        { get; private set; }
    public int   BestScore    { get; private set; }
    public float ElapsedTime  { get; private set; }
    public float DistanceKm   { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool  IsGameOver   { get; private set; }
    public Transform Ship     => _ship;

    // Événements
    public static event System.Action<int>   OnLivesChanged;
    public static event System.Action<int>   OnScoreChanged;
    public static event System.Action<int>   OnBestScoreChanged;
    public static event System.Action<float> OnTimeChanged;
    public static event System.Action<float> OnDistanceChanged;
    public static event System.Action<float> OnSpeedChanged;
    public static event System.Action        OnGameOver;

    // Internes
    private Transform    _ship;
    private GameObject[] _foodTemplates;
    private GameObject   _hazardTemplate;
    private float _foodTimer;
    private float _hazardTimer;
    private float _foodInterval;
    private float _hazardInterval;
    private float _hazardSpeed;
    private readonly List<Vector3> _recentFoodPositions = new List<Vector3>();

    public void Init(Transform ship, GameObject foodTemplate, GameObject hazardTemplate)
    {
        Init(ship, foodTemplate != null ? new[] { foodTemplate } : null, hazardTemplate);
    }

    public void Init(Transform ship, GameObject[] foodTemplates, GameObject hazardTemplate)
    {
        _ship           = ship;
        _foodTemplates  = foodTemplates;
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
                _foodInterval = 2.3f; _hazardInterval = 3.2f; _hazardSpeed = 9f;
                initialHazards = 3;   startSpeed = 12f;        speedIncrease = 0.12f;
                break;
            case DifficultyLevel.Hard:
                _foodInterval = 0.9f; _hazardInterval = 1.1f;  _hazardSpeed = 18f;
                initialHazards = 12;  startSpeed = 18f;        speedIncrease = 0.25f;
                break;
            default:
                _foodInterval = 1.4f; _hazardInterval = 1.8f;  _hazardSpeed = 13f;
                initialHazards = 6;   startSpeed = 14f;        speedIncrease = 0.18f;
                break;
        }
    }

    private void Start()
    {
        Lives        = maxLives;
        Score        = 0;
        BestScore    = PlayerPrefs.GetInt(BestScorePrefsKey, 0);
        ElapsedTime  = 0f;
        DistanceKm   = 0f;
        CurrentSpeed = startSpeed;

        OnLivesChanged?.Invoke(Lives);
        OnScoreChanged?.Invoke(Score);
        OnBestScoreChanged?.Invoke(BestScore);
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
        UpdateBestScore(true);
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
        UpdateBestScore(true);
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
        if (_foodTemplates == null || _foodTemplates.Length == 0) return;
        GameObject foodTemplate = _foodTemplates[Random.Range(0, _foodTemplates.Length)];
        if (foodTemplate == null) return;

        RollFoodSpawn(out float scale, out int requiredShots, out bool isGiant);
        Vector3 pos = GetSeparatedFoodSpawnPos(scale, isGiant);
        GameObject food = Instantiate(foodTemplate, pos, Random.rotation);
        ApplyFoodScale(food, scale, requiredShots);
        food.SetActive(true);
        TrackFoodPosition(pos);
    }

    private void RollFoodSpawn(out float scale, out int requiredShots, out bool isGiant)
    {
        requiredShots = 1;
        isGiant = false;
        float roll = Random.value;

        if (roll < 0.26f)
        {
            isGiant = true;
            scale = Random.Range(5.2f, 9.2f);
            requiredShots = Mathf.RoundToInt(Random.Range(4f, 9f));
        }
        else if (roll < 0.84f)
        {
            scale = Random.Range(1.8f, 3.6f);
        }
        else
        {
            scale = Random.Range(0.85f, 1.25f);
        }
    }

    private void ApplyFoodScale(GameObject food, float scale, int requiredShots)
    {
        SpaceFoodPickup pickup = food.GetComponent<SpaceFoodPickup>();

        food.transform.localScale = Vector3.Scale(food.transform.localScale, Vector3.one * scale);

        if (pickup != null)
            pickup.ConfigureShotHealth(requiredShots);
    }

    private void UpdateBestScore(bool save)
    {
        if (Score <= BestScore) return;

        BestScore = Score;
        OnBestScoreChanged?.Invoke(BestScore);

        if (!save) return;
        PlayerPrefs.SetInt(BestScorePrefsKey, BestScore);
        PlayerPrefs.Save();
    }

    private Vector3 GetSeparatedFoodSpawnPos(float scale, bool isGiant)
    {
        float minSpacing = isGiant ? Mathf.Max(22f, scale * 5f) : Mathf.Max(7f, scale * 3f);
        Vector3 best = Vector3.zero;
        float bestDistance = -1f;

        for (int i = 0; i < 24; i++)
        {
            float minDist = isGiant ? spawnDistance * 0.75f : spawnDistance * 0.45f;
            float maxDist = isGiant ? spawnDistance * 1.2f : spawnDistance * 0.95f;
            Vector3 candidate = GetSpawnPos(Random.Range(minDist, maxDist));
            float nearest = NearestFoodDistance(candidate);

            if (nearest >= minSpacing)
                return candidate;

            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        return bestDistance >= 0f ? best : GetSpawnPos(spawnDistance);
    }

    private float NearestFoodDistance(Vector3 candidate)
    {
        if (_recentFoodPositions.Count == 0)
            return float.MaxValue;

        float nearest = float.MaxValue;
        for (int i = 0; i < _recentFoodPositions.Count; i++)
        {
            float d = Vector3.Distance(candidate, _recentFoodPositions[i]);
            if (d < nearest)
                nearest = d;
        }

        return nearest;
    }

    private void TrackFoodPosition(Vector3 pos)
    {
        _recentFoodPositions.Add(pos);
        if (_recentFoodPositions.Count > 42)
            _recentFoodPositions.RemoveAt(0);
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
