using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static gameplay HUD: score and hearts top-left, timer/distance/speed top-right.
/// </summary>
public class SpaceHUD : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color heartFullColor = new Color(1f, 0.18f, 0.28f, 1f);
    [SerializeField] private Color heartEmptyColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
    [SerializeField] private Color scoreColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color gameOverColor = new Color(1f, 0.2f, 0.2f, 1f);

    private readonly TextMeshProUGUI[] _hearts = new TextMeshProUGUI[5];
    private TextMeshProUGUI _scoreTMP;
    private TextMeshProUGUI _timerTMP;
    private TextMeshProUGUI _distanceTMP;
    private TextMeshProUGUI _speedTMP;
    private TextMeshProUGUI _gameOverTMP;

    private void Start()
    {
        BuildHUD();

        GameManager.OnLivesChanged += UpdateHearts;
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnTimeChanged += UpdateTimer;
        GameManager.OnDistanceChanged += UpdateDistance;
        GameManager.OnSpeedChanged += UpdateSpeed;
        GameManager.OnGameOver += ShowGameOver;

        if (GameManager.Instance != null)
        {
            UpdateHearts(GameManager.Instance.Lives);
            UpdateScore(GameManager.Instance.Score);
            UpdateTimer(GameManager.Instance.ElapsedTime);
            UpdateDistance(GameManager.Instance.DistanceKm);
            UpdateSpeed(GameManager.Instance.CurrentShipSpeed);
        }
    }

    private void OnDestroy()
    {
        GameManager.OnLivesChanged -= UpdateHearts;
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnTimeChanged -= UpdateTimer;
        GameManager.OnDistanceChanged -= UpdateDistance;
        GameManager.OnSpeedChanged -= UpdateSpeed;
        GameManager.OnGameOver -= ShowGameOver;
    }

    private void BuildHUD()
    {
        GameObject canvasGO = new GameObject("HUD_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1920f, 1080f);

        _scoreTMP = CreateText(canvasGO.transform, "Score : 0", 46, scoreColor, TextAlignmentOptions.Left);
        RectTransform scoreRect = _scoreTMP.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(42f, -34f);
        scoreRect.sizeDelta = new Vector2(560f, 70f);

        GameObject heartsRoot = CreateUIObject("Hearts", canvasGO.transform);
        RectTransform heartsRect = heartsRoot.GetComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0f, 1f);
        heartsRect.anchorMax = new Vector2(0f, 1f);
        heartsRect.pivot = new Vector2(0f, 1f);
        heartsRect.anchoredPosition = new Vector2(42f, -98f);
        heartsRect.sizeDelta = new Vector2(440f, 64f);

        HorizontalLayoutGroup heartsLayout = heartsRoot.AddComponent<HorizontalLayoutGroup>();
        heartsLayout.spacing = 8f;
        heartsLayout.childAlignment = TextAnchor.MiddleLeft;
        heartsLayout.childForceExpandWidth = false;
        heartsLayout.childForceExpandHeight = false;

        for (int i = 0; i < _hearts.Length; i++)
        {
            TextMeshProUGUI heart = CreateText(heartsRoot.transform, "\u2665", 50, heartFullColor, TextAlignmentOptions.Center);
            RectTransform rect = heart.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(58f, 58f);
            _hearts[i] = heart;
        }

        GameObject statsRoot = CreateUIObject("Stats", canvasGO.transform);
        RectTransform statsRect = statsRoot.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(1f, 1f);
        statsRect.anchorMax = new Vector2(1f, 1f);
        statsRect.pivot = new Vector2(1f, 1f);
        statsRect.anchoredPosition = new Vector2(-42f, -34f);
        statsRect.sizeDelta = new Vector2(620f, 180f);

        VerticalLayoutGroup statsLayout = statsRoot.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 6f;
        statsLayout.childAlignment = TextAnchor.UpperRight;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;

        _timerTMP = CreateStatsText(statsRoot.transform, "Temps : 00:00");
        _distanceTMP = CreateStatsText(statsRoot.transform, "Distance : 0.00 km");
        _speedTMP = CreateStatsText(statsRoot.transform, "Vitesse : 0 km/h");

        _gameOverTMP = CreateText(canvasGO.transform, "GAME OVER", 100, gameOverColor, TextAlignmentOptions.Center);
        RectTransform gameOverRect = _gameOverTMP.GetComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverRect.pivot = new Vector2(0.5f, 0.5f);
        gameOverRect.anchoredPosition = Vector2.zero;
        gameOverRect.sizeDelta = new Vector2(900f, 160f);
        _gameOverTMP.gameObject.SetActive(false);
    }

    private void UpdateHearts(int lives)
    {
        for (int i = 0; i < _hearts.Length; i++)
        {
            if (_hearts[i] != null)
                _hearts[i].color = i < lives ? heartFullColor : heartEmptyColor;
        }
    }

    private void UpdateScore(int score)
    {
        if (_scoreTMP != null)
            _scoreTMP.text = "Score : " + score;
    }

    private void UpdateTimer(float seconds)
    {
        if (_timerTMP == null)
            return;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        _timerTMP.text = "Temps : " + minutes.ToString("00") + ":" + secs.ToString("00");
    }

    private void UpdateDistance(float km)
    {
        if (_distanceTMP != null)
            _distanceTMP.text = "Distance : " + km.ToString("0.00") + " km";
    }

    private void UpdateSpeed(float speed)
    {
        if (_speedTMP != null)
            _speedTMP.text = "Vitesse : " + Mathf.RoundToInt(speed * 35f) + " km/h";
    }

    private void ShowGameOver()
    {
        if (_gameOverTMP != null)
            _gameOverTMP.gameObject.SetActive(true);

        if (_scoreTMP != null)
            _scoreTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    private TextMeshProUGUI CreateStatsText(Transform parent, string text)
    {
        TextMeshProUGUI tmp = CreateText(parent, text, 34, Color.white, TextAlignmentOptions.Right);
        tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 46f);
        return tmp;
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(text, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private GameObject CreateUIObject(string objName, Transform parent)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
