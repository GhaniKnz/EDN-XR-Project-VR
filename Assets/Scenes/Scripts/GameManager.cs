using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lives")]
    [SerializeField] private int maxLives = 5;

    [Header("Prefabs")]
    [SerializeField] private FoodItem foodPrefab;
    [SerializeField] private AsteroidController asteroidPrefab;

    [Header("Spawn Zone")]
    [SerializeField] private Transform playerShip;
    [SerializeField] private float spawnDistance = 85f;
    [SerializeField] private float spawnWidth = 26f;
    [SerializeField] private float spawnHeight = 18f;
    [SerializeField] private int initialFoodCount = 8;
    [SerializeField] private int initialAsteroidCount = 6;

    [Header("Speed Ramp")]
    [SerializeField] private float startShipSpeed = 14f;
    [SerializeField] private float speedIncreasePerSecond = 0.18f;
    [SerializeField] private float maxShipSpeed = 42f;

    public int Lives { get; private set; }
    public int Score { get; private set; }
    public float ElapsedTime { get; private set; }
    public float DistanceKm { get; private set; }
    public float CurrentShipSpeed { get; private set; }
    public bool IsGameOver { get; private set; }

    public static event System.Action<int> OnLivesChanged;
    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<float> OnTimeChanged;
    public static event System.Action<float> OnDistanceChanged;
    public static event System.Action<float> OnSpeedChanged;
    public static event System.Action OnGameOver;

    private float _foodTimer;
    private float _asteroidTimer;
    private float _foodInterval;
    private float _asteroidInterval;
    private float _asteroidSpeed;

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
                _foodInterval = 4.5f;
                _asteroidInterval = 3.4f;
                _asteroidSpeed = 10f;
                initialAsteroidCount = 4;
                startShipSpeed = 13f;
                speedIncreasePerSecond = 0.14f;
                break;
            case DifficultyLevel.Hard:
                _foodInterval = 2.2f;
                _asteroidInterval = 1.2f;
                _asteroidSpeed = 18f;
                initialAsteroidCount = 14;
                startShipSpeed = 18f;
                speedIncreasePerSecond = 0.26f;
                break;
            default:
                _foodInterval = 3.2f;
                _asteroidInterval = 2.0f;
                _asteroidSpeed = 14f;
                initialAsteroidCount = 8;
                startShipSpeed = 15f;
                speedIncreasePerSecond = 0.20f;
                break;
        }
    }

    private void Start()
    {
        Lives = maxLives;
        Score = 0;
        ElapsedTime = 0f;
        DistanceKm = 0f;
        CurrentShipSpeed = startShipSpeed;
        OnLivesChanged?.Invoke(Lives);
        OnScoreChanged?.Invoke(Score);
        OnTimeChanged?.Invoke(ElapsedTime);
        OnDistanceChanged?.Invoke(DistanceKm);
        OnSpeedChanged?.Invoke(CurrentShipSpeed);

        for (int i = 0; i < initialFoodCount; i++)
            SpawnFood();

        for (int i = 0; i < initialAsteroidCount; i++)
            SpawnAsteroid();
    }

    private void Update()
    {
        if (IsGameOver) return;

        _foodTimer += Time.deltaTime;
        _asteroidTimer += Time.deltaTime;
        ElapsedTime += Time.deltaTime;
        CurrentShipSpeed = Mathf.Min(maxShipSpeed, startShipSpeed + ElapsedTime * speedIncreasePerSecond);
        DistanceKm += CurrentShipSpeed * Time.deltaTime * 0.001f;

        OnTimeChanged?.Invoke(ElapsedTime);
        OnDistanceChanged?.Invoke(DistanceKm);
        OnSpeedChanged?.Invoke(CurrentShipSpeed);

        if (_foodTimer >= _foodInterval) { _foodTimer = 0f; SpawnFood(); }
        if (_asteroidTimer >= _asteroidInterval) { _asteroidTimer = 0f; SpawnAsteroid(); }
    }

    public void AddScore(int points)
    {
        Score += points;
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
        if (foodPrefab == null) return;
        Vector3 pos = GetSpawnPosition(Random.Range(spawnDistance * 0.45f, spawnDistance * 0.85f));
        FoodItem food = Instantiate(foodPrefab, pos, Quaternion.identity);
        food.gameObject.SetActive(true);
    }

    private void SpawnAsteroid()
    {
        if (asteroidPrefab == null) return;
        Vector3 spawnPos = GetSpawnPosition(spawnDistance);
        Vector3 target = playerShip != null
            ? playerShip.position + Random.insideUnitSphere * 6f
            : Random.insideUnitSphere * 10f;

        AsteroidController ast = Instantiate(asteroidPrefab, spawnPos, Random.rotation);
        ast.gameObject.SetActive(true);
        ast.Initialize((target - spawnPos).normalized, _asteroidSpeed + CurrentShipSpeed * 0.35f);
    }

    private Vector3 GetSpawnPosition(float forwardDistance)
    {
        if (playerShip == null)
        {
            return new Vector3(
                Random.Range(-spawnWidth, spawnWidth),
                Random.Range(-spawnHeight * 0.5f, spawnHeight * 0.5f),
                forwardDistance
            );
        }

        Vector3 lateral = playerShip.right * Random.Range(-spawnWidth, spawnWidth);
        Vector3 vertical = Vector3.up * Random.Range(-spawnHeight * 0.5f, spawnHeight * 0.5f);
        return playerShip.position + playerShip.forward * forwardDistance + lateral + vertical;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
