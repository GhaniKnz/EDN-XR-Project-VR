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
        }
    }

    private void Awake()
    {
        // Bouton X (manette gauche) ou R clavier → restart
        _resetAction = new InputAction("Btn_Reset", InputActionType.Button);
        _resetAction.AddBinding("<XRController>{LeftHand}/primaryButton");
        _resetAction.AddBinding("<Keyboard>/r");
        _resetAction.Enable();

        // Bouton Y (manette gauche) ou Escape → menu principal
        _menuAction = new InputAction("Btn_Menu", InputActionType.Button);
        _menuAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
        _menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
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
        SpaceGameManager.OnTimeChanged     += SetTimer;
        SpaceGameManager.OnDistanceChanged += SetDistance;
        SpaceGameManager.OnSpeedChanged    += SetSpeed;
        SpaceGameManager.OnGameOver        += ShowGameOver;
    }

    private void OnDisable()
    {
        SpaceGameManager.OnLivesChanged    -= SetLives;
        SpaceGameManager.OnScoreChanged    -= SetScore;
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
        canvasGO.transform.localPosition = new Vector3(0.30f, 0.17f, 1.8f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = Vector3.one * 0.001f;

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = _cam;
        canvas.sortingOrder = 100;

        RectTransform cr = canvasGO.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(400f, 260f);

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(400f, 260f);

        Image bg = canvasGO.AddComponent<Image>();
        bg.sprite        = BuildRoundedSprite(64, 64, 14);
        bg.type          = Image.Type.Sliced;
        bg.color         = new Color(0f, 0.03f, 0.10f, 0.82f);
        bg.raycastTarget = false;

        GameObject container = UIGo("Content", canvasGO.transform);
        RectTransform ct = container.GetComponent<RectTransform>();
        ct.anchorMin = Vector2.zero;
        ct.anchorMax = Vector2.one;
        ct.offsetMin = new Vector2(16f, 10f);
        ct.offsetMax = new Vector2(-16f, -10f);
        VerticalLayoutGroup vl = container.AddComponent<VerticalLayoutGroup>();
        vl.spacing              = 5f;
        vl.childAlignment       = TextAnchor.UpperLeft;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // ── Ligne 1 : cœurs ──────────────────────────────────────────────────────
        GameObject heartsRow = UIGo("Hearts", container.transform);
        heartsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
        HorizontalLayoutGroup hl = heartsRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 5f;
        hl.childAlignment       = TextAnchor.MiddleLeft;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;

        _hearts = new TextMeshProUGUI[5];
        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI h = MakeTMP(heartsRow.transform, "♥", 34, HeartFull, TextAlignmentOptions.Center);
            h.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 40f);
            _hearts[i] = h;
        }

        // ── Ligne 2 : score ───────────────────────────────────────────────────────
        _scoreTMP = MakeTMP(container.transform, "0 pts", 42, Accent, TextAlignmentOptions.Left);
        _scoreTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

        // ── Ligne 3 : stats ───────────────────────────────────────────────────────
        GameObject statsRow = UIGo("Stats", container.transform);
        statsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);
        HorizontalLayoutGroup sl = statsRow.AddComponent<HorizontalLayoutGroup>();
        sl.spacing = 14f;
        sl.childAlignment       = TextAnchor.MiddleLeft;
        sl.childForceExpandWidth  = false;
        sl.childForceExpandHeight = false;

        Color statCol = new Color(0.60f, 0.88f, 1f);
        _timerTMP = MiniStat(statsRow.transform, "00:00",  statCol);
        _distTMP  = MiniStat(statsRow.transform, "0.0 km", statCol);
        _speedTMP = MiniStat(statsRow.transform, "0 km/h", statCol);

        // ── Ligne 4 : boutons Reset / Menu ────────────────────────────────────────
        GameObject btnRow = UIGo("Buttons", container.transform);
        btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
        HorizontalLayoutGroup bl = btnRow.AddComponent<HorizontalLayoutGroup>();
        bl.spacing = 10f;
        bl.childAlignment       = TextAnchor.MiddleLeft;
        bl.childForceExpandWidth  = false;
        bl.childForceExpandHeight = false;

        MakeButton(btnRow.transform, "[X] Restart", BtnReset, 160f);
        MakeButton(btnRow.transform, "[Y] Menu",    BtnMenu,  130f);

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
        MakeButton(goBtnRow.transform, "[X] Rejouer", BtnReset, 320f, 70f, 44);
        MakeButton(goBtnRow.transform, "[Y] Menu",    BtnMenu,  230f, 70f, 44);

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

    private TextMeshProUGUI MiniStat(Transform parent, string txt, Color col)
    {
        TextMeshProUGUI t = MakeTMP(parent, txt, 22, col, TextAlignmentOptions.Left);
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(114f, 30f);
        return t;
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
