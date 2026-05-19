using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpaceGameHUD : MonoBehaviour
{
    private static readonly Color HeartFull  = new Color(1f,   0.18f, 0.28f);
    private static readonly Color HeartEmpty = new Color(0.45f,0.45f, 0.45f, 0.5f);
    private static readonly Color Accent     = new Color(0.3f, 0.9f,  1f);
    private static readonly Color GameOverC  = new Color(1f,   0.2f,  0.2f);
    private static readonly Color BtnReset   = new Color(0.15f,0.55f, 1f,   0.88f);
    private static readonly Color BtnMenu    = new Color(0.55f,0.55f, 0.55f,0.75f);

    private Camera              _cam;
    private TextMeshProUGUI[]   _hearts;
    private TextMeshProUGUI     _scoreTMP;
    private TextMeshProUGUI     _bestScoreTMP;
    private TextMeshProUGUI     _timerTMP;
    private TextMeshProUGUI     _distTMP;
    private TextMeshProUGUI     _speedTMP;
    private GameObject          _gameOverCanvas;

    private InputAction _resetAction;
    private InputAction _menuAction;

    // ─── Init ────────────────────────────────────────────────────────────────────

    public void Init(Camera cam)
    {
        if (cam == null) return;
        _cam = cam;
        BuildHUD();
        if (SpaceGameManager.Instance != null)
        {
            SetLives(SpaceGameManager.Instance.Lives);
            SetScore(SpaceGameManager.Instance.Score);
            SetBestScore(SpaceGameManager.Instance.BestScore);
        }
    }

    private void Awake()
    {
        // Bouton A (manette droite) ou R clavier -> restart
        _resetAction = new InputAction("Btn_Reset", InputActionType.Button);
        _resetAction.AddBinding("<XRController>{RightHand}/primaryButton");
        _resetAction.AddBinding("<Keyboard>/r");
        _resetAction.Enable();

        // Bouton B (manette droite) ou Escape -> menu principal
        _menuAction = new InputAction("Btn_Menu", InputActionType.Button);
        _menuAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        _menuAction.AddBinding("<Keyboard>/escape");
        _menuAction.Enable();
    }

    private void OnDestroy()
    {
        _resetAction?.Dispose();
        _menuAction?.Dispose();
    }

    private void OnEnable()
    {
        SpaceGameManager.OnLivesChanged    += SetLives;
        SpaceGameManager.OnScoreChanged    += SetScore;
        SpaceGameManager.OnBestScoreChanged += SetBestScore;
        SpaceGameManager.OnTimeChanged     += SetTimer;
        SpaceGameManager.OnDistanceChanged += SetDistance;
        SpaceGameManager.OnSpeedChanged    += SetSpeed;
        SpaceGameManager.OnGameOver        += ShowGameOver;
    }

    private void OnDisable()
    {
        SpaceGameManager.OnLivesChanged    -= SetLives;
        SpaceGameManager.OnScoreChanged    -= SetScore;
        SpaceGameManager.OnBestScoreChanged -= SetBestScore;
        SpaceGameManager.OnTimeChanged     -= SetTimer;
        SpaceGameManager.OnDistanceChanged -= SetDistance;
        SpaceGameManager.OnSpeedChanged    -= SetSpeed;
        SpaceGameManager.OnGameOver        -= ShowGameOver;
    }

    private void Update()
    {
        if (_resetAction != null && _resetAction.WasPressedThisFrame())
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (_menuAction != null && _menuAction.WasPressedThisFrame())
            SceneManager.LoadScene("MainMenu");
    }

    // ─── Construction ────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        // ── Canvas HUD – haut-droite ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("HUD_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(_cam.transform, false);
        canvasGO.transform.localPosition = new Vector3(0.52f, 0.28f, 1.6f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = Vector3.one * 0.00145f;

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = _cam;
        canvas.sortingOrder = 100;

        RectTransform cr = canvasGO.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(430f, 340f);

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(430f, 340f);

        Image bg = canvasGO.AddComponent<Image>();
        bg.sprite        = BuildRectSprite(4, 4);
        bg.type          = Image.Type.Simple;
        bg.color         = new Color(0f, 0.02f, 0.06f, 0.58f);
        bg.raycastTarget = false;

        GameObject container = UIGo("Content", canvasGO.transform);
        RectTransform ct = container.GetComponent<RectTransform>();
        ct.anchorMin = Vector2.zero;
        ct.anchorMax = Vector2.one;
        ct.offsetMin = new Vector2(16f, 12f);
        ct.offsetMax = new Vector2(-16f, -12f);
        VerticalLayoutGroup vl = container.AddComponent<VerticalLayoutGroup>();
        vl.spacing              = 8f;
        vl.childAlignment       = TextAnchor.UpperLeft;
        vl.childControlWidth    = true;
        vl.childControlHeight   = false;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // ── Ligne 1 : cœurs ──────────────────────────────────────────────────────
        GameObject heartsRow = UIGo("Hearts", container.transform);
        heartsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 42f);
        HorizontalLayoutGroup hl = heartsRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 4f;
        hl.childAlignment       = TextAnchor.MiddleLeft;
        hl.childControlWidth    = false;
        hl.childControlHeight   = false;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;

        _hearts = new TextMeshProUGUI[5];
        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI h = MakeTMP(heartsRow.transform, "♥", 28, HeartFull, TextAlignmentOptions.Center);
            h.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 38f);
            _hearts[i] = h;
        }

        // ── Ligne 2 : score ───────────────────────────────────────────────────────
        TextMeshProUGUI scoreLabel = MakeTMP(container.transform, "SCORE", 16, new Color(0.68f, 0.92f, 1f, 0.78f), TextAlignmentOptions.Left);
        scoreLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);

        _scoreTMP = MakeTMP(container.transform, "0 pts", 40, Accent, TextAlignmentOptions.Left);
        _scoreTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

        // ── Ligne 3 : stats ───────────────────────────────────────────────────────
        GameObject statsRow = UIGo("Stats", container.transform);
        statsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 92f);
        GridLayoutGroup sl = statsRow.AddComponent<GridLayoutGroup>();
        sl.cellSize = new Vector2(194f, 40f);
        sl.spacing = new Vector2(8f, 8f);
        sl.childAlignment = TextAnchor.UpperLeft;
        sl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        sl.constraintCount = 2;

        Color statCol = new Color(0.60f, 0.88f, 1f);
        _timerTMP = MiniStat(statsRow.transform, "TEMPS", "00:00",  statCol);
        _distTMP  = MiniStat(statsRow.transform, "DISTANCE", "0.0 km", statCol);
        _speedTMP = MiniStat(statsRow.transform, "VITESSE", "0 km/h", statCol);
        _bestScoreTMP = MiniStat(statsRow.transform, "MAX", "0 pts", statCol);

        // ── Ligne 4 : boutons Reset / Menu ────────────────────────────────────────
        GameObject btnRow = UIGo("Buttons", container.transform);
        btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);
        HorizontalLayoutGroup bl = btnRow.AddComponent<HorizontalLayoutGroup>();
        bl.spacing = 6f;
        bl.childAlignment       = TextAnchor.MiddleLeft;
        bl.childControlWidth    = false;
        bl.childControlHeight   = false;
        bl.childForceExpandWidth  = false;
        bl.childForceExpandHeight = false;

        MakeButton(btnRow.transform, "[A] Restart", BtnReset, 154f, 42f, 20);
        MakeButton(btnRow.transform, "[B] Menu",    BtnMenu,  122f, 42f, 20);

        // ── Canvas Game Over ──────────────────────────────────────────────────────
        _gameOverCanvas = new GameObject("GameOver_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _gameOverCanvas.transform.SetParent(_cam.transform, false);
        _gameOverCanvas.transform.localPosition = new Vector3(0f, 0f, 2f);
        _gameOverCanvas.transform.localRotation = Quaternion.identity;
        _gameOverCanvas.transform.localScale    = Vector3.one * 0.0013f;

        Canvas goCanvas = _gameOverCanvas.GetComponent<Canvas>();
        goCanvas.renderMode  = RenderMode.WorldSpace;
        goCanvas.worldCamera = _cam;
        goCanvas.sortingOrder = 200;

        RectTransform goRect = _gameOverCanvas.GetComponent<RectTransform>();
        goRect.sizeDelta = new Vector2(1200f, 380f);

        GameObject goContent = UIGo("GOContent", _gameOverCanvas.transform);
        RectTransform goct = goContent.GetComponent<RectTransform>();
        goct.anchorMin = Vector2.zero; goct.anchorMax = Vector2.one;
        goct.offsetMin = goct.offsetMax = Vector2.zero;
        VerticalLayoutGroup govl = goContent.AddComponent<VerticalLayoutGroup>();
        govl.childAlignment       = TextAnchor.MiddleCenter;
        govl.childForceExpandWidth  = true;
        govl.childForceExpandHeight = false;
        govl.spacing = 14f;

        TextMeshProUGUI goTMP = MakeTMP(goContent.transform, "GAME OVER", 110, GameOverC, TextAlignmentOptions.Center);
        goTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 160f);

        // Boutons Game Over
        GameObject goBtnRow = UIGo("GOButtons", goContent.transform);
        goBtnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 80f);
        HorizontalLayoutGroup gobl = goBtnRow.AddComponent<HorizontalLayoutGroup>();
        gobl.spacing = 30f;
        gobl.childAlignment       = TextAnchor.MiddleCenter;
        gobl.childForceExpandWidth  = false;
        gobl.childForceExpandHeight = false;
        MakeButton(goBtnRow.transform, "[A] Rejouer", BtnReset, 320f, 70f, 44);
        MakeButton(goBtnRow.transform, "[B] Menu",    BtnMenu,  230f, 70f, 44);

        _gameOverCanvas.SetActive(false);
    }

    // ─── Mises à jour ────────────────────────────────────────────────────────────

    private void SetLives(int lives)
    {
        if (_hearts == null) return;
        for (int i = 0; i < _hearts.Length; i++)
            if (_hearts[i] != null)
                _hearts[i].color = i < lives ? HeartFull : HeartEmpty;
    }

    private void SetScore(int score)
    {
        if (_scoreTMP != null) _scoreTMP.text = score + " pts";
    }

    private void SetBestScore(int score)
    {
        if (_bestScoreTMP != null) _bestScoreTMP.text = score + " pts";
    }

    private void SetTimer(float s)
    {
        if (_timerTMP == null) return;
        int m   = Mathf.FloorToInt(s / 60f);
        int sec = Mathf.FloorToInt(s % 60f);
        _timerTMP.text = m.ToString("00") + ":" + sec.ToString("00");
    }

    private void SetDistance(float km)
    {
        if (_distTMP != null) _distTMP.text = km.ToString("0.0") + " km";
    }

    private void SetSpeed(float speed)
    {
        if (_speedTMP != null) _speedTMP.text = Mathf.RoundToInt(speed * 35f) + " km/h";
    }

    private void ShowGameOver()
    {
        if (_gameOverCanvas != null) _gameOverCanvas.SetActive(true);
        if (_scoreTMP != null)       _scoreTMP.color = Color.gray;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private void MakeButton(Transform parent, string label, Color col, float w, float h = 40f, int fontSize = 22)
    {
        GameObject go = UIGo(label, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);

        Image img = go.AddComponent<Image>();
        img.sprite        = BuildRoundedSprite(32, 32, 8);
        img.type          = Image.Type.Sliced;
        img.color         = col;
        img.raycastTarget = false;

        TextMeshProUGUI tmp = MakeTMP(go.transform, label, fontSize,
            Color.white, TextAlignmentOptions.Center);
        RectTransform tr = tmp.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI MiniStat(Transform parent, string label, string txt, Color col)
    {
        GameObject cell = UIGo(label, parent);
        cell.GetComponent<RectTransform>().sizeDelta = new Vector2(194f, 40f);

        VerticalLayoutGroup layout = cell.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI l = MakeTMP(cell.transform, label, 12, new Color(0.68f, 0.92f, 1f, 0.72f), TextAlignmentOptions.Left);
        l.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 14f);

        TextMeshProUGUI value = MakeTMP(cell.transform, txt, 21, col, TextAlignmentOptions.Left);
        value.enableAutoSizing = true;
        value.fontSizeMin = 15;
        value.fontSizeMax = 21;
        value.enableWordWrapping = false;
        value.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 25f);
        return value;
    }

    private TextMeshProUGUI MakeTMP(Transform parent, string txt, int size, Color col, TextAlignmentOptions align)
    {
        GameObject go = UIGo(txt, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = txt;
        tmp.fontSize      = size;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = col;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject UIGo(string n, Transform parent)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Sprite BuildRectSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = Color.white;
        tex.SetPixels(pix);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
            1f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildRoundedSprite(int w, int h, int r)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pix   = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int   cx = Mathf.Clamp(x, r, w - 1 - r);
                int   cy = Mathf.Clamp(y, r, h - 1 - r);
                float dx = x - cx, dy = y - cy;
                pix[y * w + x] = dx * dx + dy * dy <= (float)(r * r) ? Color.white : Color.clear;
            }
        tex.SetPixels(pix);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
            1f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }
}
