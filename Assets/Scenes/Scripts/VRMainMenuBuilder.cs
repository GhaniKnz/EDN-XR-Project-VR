using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VRMainMenuBuilder : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string newGameSceneName = "Hubstation";

    [Header("Placement")]
    [SerializeField] private float distanceFromCamera = 2.2f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.02f, 0f);
    [SerializeField] private float worldScale = 0.00165f;

    [Header("Textes")]
    [SerializeField] private string gameTitle = "GALACTIC FOOD LAB";
    [SerializeField] private string subtitle = "Science, nourriture et exploration spatiale";

    [Header("Couleurs")]
    [SerializeField] private Color backgroundTint = new Color(0.02f, 0.05f, 0.09f, 0.42f);
    [SerializeField] private Color cardColor = new Color(0.035f, 0.075f, 0.13f, 0.96f);
    [SerializeField] private Color cardSecondaryColor = new Color(0.09f, 0.16f, 0.24f, 0.28f);
    [SerializeField] private Color buttonColor = new Color(0.045f, 0.085f, 0.14f, 0.98f);
    [SerializeField] private Color buttonHighlightColor = new Color(0.10f, 0.22f, 0.34f, 1f);
    [SerializeField] private Color accentColor = new Color(0.24f, 0.82f, 1f, 1f);

    private GameObject canvasGO;
    private GameObject mainPanel;
    private GameObject settingsPanel;
    private GameObject levelSelectPanel;
    private TextMeshProUGUI volumeText;
    private CanvasGroup fadeGroup;
    private bool isLoading;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private void Start()
    {
        SanitizeSceneNames();
        BuildMenu();
    }

    private void SanitizeSceneNames()
    {
        if (!IsSceneInBuildSettings(newGameSceneName))
        {
            Debug.LogWarning("[VRMainMenuBuilder] newGameSceneName '" + newGameSceneName + "' absent de Build Settings — correction automatique vers 'Hubstation'.");
            newGameSceneName = "Hubstation";
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || isLoading)
            return;

        if (Keyboard.current.nKey.wasPressedThisFrame)
            StartNewGame();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            ShowSettings();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ShowMainMenu();

        if (Keyboard.current.qKey.wasPressedThisFrame)
            QuitGame();
    }

    [ContextMenu("Build VR Main Menu")]
    public void BuildMenu()
    {
        EnsureXrEventSystem();

        if (!TryPlaceMenuInFrontOfCamera())
            return;

        ClearExistingChildren();
        CreateCanvas();
        CreateBackgroundDecor();
        CreateCardAndPanels();
        CreateFadeOverlay();
        SetVolumeLabel();
    }

    private void EnsureXrEventSystem()
    {
        EventSystem existing = FindFirstObjectByType<EventSystem>();

        if (existing == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<XRUIInputModule>();
            return;
        }

        if (existing.GetComponent<XRUIInputModule>() == null)
        {
            BaseInputModule[] modules = existing.GetComponents<BaseInputModule>();
            foreach (BaseInputModule module in modules)
                Destroy(module);

            existing.gameObject.AddComponent<XRUIInputModule>();
        }
    }

    private bool TryPlaceMenuInFrontOfCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("VRMainMenuBuilder : aucune MainCamera trouvee.");
            return false;
        }

        Vector3 worldOffset =
            cam.transform.right   * localOffset.x +
            cam.transform.up      * localOffset.y +
            cam.transform.forward * localOffset.z;

        transform.position = cam.transform.position + cam.transform.forward * distanceFromCamera + worldOffset;
        transform.LookAt(cam.transform.position, cam.transform.up);
        transform.Rotate(0f, 180f, 0f);

        return true;
    }

    private void ClearExistingChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void CreateCanvas()
    {
        canvasGO = new GameObject(
            "VRMainMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster)
        );

        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);

        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2560f, 1440f);

        canvasGO.transform.localScale = Vector3.one * worldScale;
    }

    private void CreateBackgroundDecor()
    {
        GameObject dim = CreateUIObject("DimBackground", canvasGO.transform);
        RectTransform dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = new Vector2(0.5f, 0.5f);
        dimRect.anchorMax = new Vector2(0.5f, 0.5f);
        dimRect.pivot = new Vector2(0.5f, 0.5f);
        dimRect.anchoredPosition = new Vector2(0f, -16f);
        dimRect.sizeDelta = new Vector2(2080f, 1400f);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.sprite = GetRoundedSprite(2048, 1536, 84);
        dimImage.type = Image.Type.Sliced;
        dimImage.color = backgroundTint;
        dimImage.raycastTarget = false;

        CreateGlow(new Vector2(0f, 110f), new Vector2(1520f, 1520f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.06f), 760f);
        CreateGlow(new Vector2(-780f, 400f), new Vector2(440f, 440f), new Color(0.45f, 0.85f, 1f, 0.065f), 220f);
        CreateGlow(new Vector2(800f, -350f), new Vector2(380f, 380f), new Color(0.25f, 0.55f, 1f, 0.075f), 190f);

        CreatePlanet(new Vector2(-800f, 340f), 230f, new Color(0.40f, 0.95f, 1f, 0.09f));
        CreatePlanet(new Vector2(840f, -340f), 172f, new Color(0.30f, 0.55f, 1f, 0.09f));

        CreateHorizontalLine(new Vector2(0f, 600f), new Vector2(1300f, 4f), new Color(0.7f, 0.95f, 1f, 0.12f));
        CreateHorizontalLine(new Vector2(0f, -600f), new Vector2(1200f, 4f), new Color(0.5f, 0.80f, 1f, 0.07f));

        CreateStars(22);
    }

    private void CreateCardAndPanels()
    {
        GameObject card = CreateUIObject("MainCard", canvasGO.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot     = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1520f, 1440f);

        Image cardImage = card.AddComponent<Image>();
        cardImage.sprite = GetRoundedSprite(2048, 1536, 92);
        cardImage.type   = Image.Type.Sliced;
        cardImage.color  = cardColor;

        Shadow outerShadow = card.AddComponent<Shadow>();
        outerShadow.effectColor    = new Color(0f, 0f, 0f, 0.48f);
        outerShadow.effectDistance = new Vector2(0f, -24f);

        UIFloatingFx floatFx = card.AddComponent<UIFloatingFx>();
        floatFx.amplitude = 5f;
        floatFx.speed     = 0.8f;

        GameObject innerCard = CreateUIObject("InnerCard", card.transform);
        RectTransform innerRect = innerCard.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.pivot     = new Vector2(0.5f, 0.5f);
        innerRect.sizeDelta = new Vector2(1420f, 1340f);

        Image innerImage = innerCard.AddComponent<Image>();
        innerImage.sprite = GetRoundedSprite(2048, 1536, 76);
        innerImage.type   = Image.Type.Sliced;
        innerImage.color  = cardSecondaryColor;
        innerImage.raycastTarget = false;

        CreateTopAccent(card.transform);

        GameObject contentRoot = CreateUIObject("ContentRoot", card.transform);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(96f, 72f);
        contentRect.offsetMax = new Vector2(-96f, -96f);
        contentRect.localScale = new Vector3(1.2f, 1.2f, 1.5f);

        mainPanel        = CreatePanel(contentRoot.transform, "MainPanel");
        settingsPanel    = CreatePanel(contentRoot.transform, "SettingsPanel");
        levelSelectPanel = CreatePanel(contentRoot.transform, "LevelSelectPanel");
        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);

        BuildMainPanel();
        BuildSettingsPanel();
        BuildLevelSelectPanel();
    }

    private void CreateTopAccent(Transform parent)
    {
        GameObject barGlow = CreateUIObject("TopGlow", parent);
        RectTransform glowRect = barGlow.GetComponent<RectTransform>();
        glowRect.anchorMin       = new Vector2(0.5f, 1f);
        glowRect.anchorMax       = new Vector2(0.5f, 1f);
        glowRect.pivot           = new Vector2(0.5f, 1f);
        glowRect.anchoredPosition = new Vector2(0f, -20f);
        glowRect.sizeDelta        = new Vector2(1180f, 40f);

        Image glowImage = barGlow.AddComponent<Image>();
        glowImage.sprite = GetRoundedSprite(512, 64, 32);
        glowImage.type   = Image.Type.Sliced;
        glowImage.color  = new Color(accentColor.r, accentColor.g, accentColor.b, 0.28f);
        glowImage.raycastTarget = false;

        GameObject bar = CreateUIObject("TopBar", parent);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin        = new Vector2(0.5f, 1f);
        barRect.anchorMax        = new Vector2(0.5f, 1f);
        barRect.pivot            = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -28f);
        barRect.sizeDelta        = new Vector2(780f, 16f);

        Image barImage = bar.AddComponent<Image>();
        barImage.sprite = GetRoundedSprite(512, 64, 32);
        barImage.type   = Image.Type.Sliced;
        barImage.color  = accentColor;
        barImage.raycastTarget = false;
    }

    private GameObject CreatePanel(Transform parent, string panelName)
    {
        GameObject panel = CreateUIObject(panelName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        StretchFull(rect);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment    = TextAnchor.UpperCenter;
        layout.spacing           = 16f;
        layout.padding           = new RectOffset(20, 20, 28, 24);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        return panel;
    }

    private void BuildMainPanel()
    {
        CreateBadge(mainPanel.transform, "VR SPACE EXPERIENCE");

        TextMeshProUGUI title = CreateTextBlock(mainPanel.transform, gameTitle, 108, 140f, FontStyles.Bold, Color.white);
        title.characterSpacing = 1.2f;

        CreateTextBlock(mainPanel.transform, subtitle, 50, 76f, FontStyles.Normal, new Color(0.84f, 0.93f, 1f, 1f));
        CreateSpacer(mainPanel.transform, 12f);

        CreateInfoPill(mainPanel.transform, "Laboratoire spatial - cuisine futuriste - exploration immersive");

        CreateSpacer(mainPanel.transform, 20f);

        CreateButton(mainPanel.transform, "Nouvelle partie",  StartNewGame);
        CreateButton(mainPanel.transform, "Selection niveau", OpenLevelSelection);
        CreateButton(mainPanel.transform, "Reglages",         ShowSettings);
        CreateButton(mainPanel.transform, "Quitter",          QuitGame);
    }

    private void BuildSettingsPanel()
    {
        CreateBadge(settingsPanel.transform, "SETTINGS");

        TextMeshProUGUI title = CreateTextBlock(settingsPanel.transform, "Reglages", 108, 140f, FontStyles.Bold, Color.white);
        title.characterSpacing = 1f;

        CreateTextBlock(settingsPanel.transform, "Parametres audio et affichage", 50, 84f, FontStyles.Normal, new Color(0.84f, 0.93f, 1f, 1f));
        CreateSpacer(settingsPanel.transform, 20f);

        volumeText = CreateTextBlock(settingsPanel.transform, "", 68, 108f, FontStyles.Bold, accentColor);

        CreateButton(settingsPanel.transform, "Volume +",          IncreaseVolume);
        CreateButton(settingsPanel.transform, "Volume -",          DecreaseVolume);
        CreateButton(settingsPanel.transform, "Recentrer le menu", RecenterMenu);
        CreateButton(settingsPanel.transform, "Retour",            ShowMainMenu);

    }

    private void BuildLevelSelectPanel()
    {
        CreateBadge(levelSelectPanel.transform, "SELECTION NIVEAU");

        TextMeshProUGUI title = CreateTextBlock(levelSelectPanel.transform, "Difficulte", 108, 140f, FontStyles.Bold, Color.white);
        title.characterSpacing = 1f;

        CreateTextBlock(levelSelectPanel.transform, "Choisis ton niveau avant de lancer la partie", 50, 84f, FontStyles.Normal, new Color(0.84f, 0.93f, 1f, 1f));
        CreateSpacer(levelSelectPanel.transform, 28f);

        CreateButton(levelSelectPanel.transform, "Facile",   () => SelectDifficulty(DifficultyLevel.Easy));
        CreateButton(levelSelectPanel.transform, "Moyen",    () => SelectDifficulty(DifficultyLevel.Medium));
        CreateButton(levelSelectPanel.transform, "Difficile",() => SelectDifficulty(DifficultyLevel.Hard));
        CreateButton(levelSelectPanel.transform, "Retour",   ShowMainMenu);
    }

    private void SelectDifficulty(DifficultyLevel difficulty)
    {
        GameData.Difficulty = difficulty;
        StartCoroutine(FadeAndLoadScene(newGameSceneName));
    }

    private void CreateBadge(Transform parent, string text)
    {
        GameObject badge = CreateUIObject("Badge", parent);

        LayoutElement layout = badge.AddComponent<LayoutElement>();
        layout.preferredHeight = 88f;
        layout.preferredWidth  = 640f;

        Image badgeImage = badge.AddComponent<Image>();
        badgeImage.sprite = GetRoundedSprite(512, 128, 64);
        badgeImage.type   = Image.Type.Sliced;
        badgeImage.color  = new Color(accentColor.r, accentColor.g, accentColor.b, 0.16f);

        Shadow badgeShadow = badge.AddComponent<Shadow>();
        badgeShadow.effectColor    = new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f);
        badgeShadow.effectDistance = new Vector2(0f, 0f);

        GameObject labelGO = CreateUIObject("BadgeText", badge.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = accentColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 1.5f;
    }

    private void CreateInfoPill(Transform parent, string text)
    {
        GameObject pill = CreateUIObject("InfoPill", parent);

        LayoutElement layout = pill.AddComponent<LayoutElement>();
        layout.preferredHeight = 96f;
        layout.preferredWidth  = 1240f;

        Image pillImage = pill.AddComponent<Image>();
        pillImage.sprite = GetRoundedSprite(1024, 128, 64);
        pillImage.type   = Image.Type.Sliced;
        pillImage.color  = new Color(1f, 1f, 1f, 0.05f);
        pillImage.raycastTarget = false;

        GameObject labelGO = CreateUIObject("InfoText", pill.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 40;
        tmp.color     = new Color(0.86f, 0.95f, 1f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick)
    {
        GameObject buttonGO = CreateUIObject(label + "_Button", parent);

        LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 144f;
        layoutElement.preferredWidth = 1240f;

        Image image = buttonGO.AddComponent<Image>();
        image.sprite = GetRoundedSprite(2048, 512, 72);
        image.type   = Image.Type.Sliced;
        image.color  = buttonColor;

        Shadow shadow = buttonGO.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -12f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor      = buttonColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.selectedColor    = buttonHighlightColor;
        colors.pressedColor     = accentColor * 0.85f;
        colors.disabledColor    = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        colors.colorMultiplier  = 1f;
        button.colors = colors;

        UIButtonHoverFx hoverFx = buttonGO.AddComponent<UIButtonHoverFx>();
        hoverFx.hoverScale  = 1.025f;
        hoverFx.normalScale = 1f;
        hoverFx.speed       = 10f;

        button.onClick.AddListener(onClick);

        GameObject accentLeft = CreateUIObject("AccentLeft", buttonGO.transform);
        RectTransform accentRect = accentLeft.GetComponent<RectTransform>();
        accentRect.anchorMin        = new Vector2(0f, 0.5f);
        accentRect.anchorMax        = new Vector2(0f, 0.5f);
        accentRect.pivot            = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = new Vector2(28f, 0f);
        accentRect.sizeDelta        = new Vector2(18f, 88f);

        Image accentImage = accentLeft.AddComponent<Image>();
        accentImage.sprite = GetRoundedSprite(128, 512, 64);
        accentImage.type   = Image.Type.Sliced;
        accentImage.color  = accentColor;
        accentImage.raycastTarget = false;

        GameObject labelGO = CreateUIObject("Label", buttonGO.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);
        labelRect.offsetMin = new Vector2(88f, 0f);
        labelRect.offsetMax = new Vector2(-140f, 0f);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 64;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 44f;
        tmp.fontSizeMax = 64f;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        GameObject arrowGO = CreateUIObject("Arrow", buttonGO.transform);
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin        = new Vector2(1f, 0.5f);
        arrowRect.anchorMax        = new Vector2(1f, 0.5f);
        arrowRect.pivot            = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-48f, 0f);
        arrowRect.sizeDelta        = new Vector2(64f, 64f);

        TextMeshProUGUI arrow = arrowGO.AddComponent<TextMeshProUGUI>();
        arrow.text      = ">";
        arrow.fontSize  = 80;
        arrow.fontStyle = FontStyles.Bold;
        arrow.color     = accentColor;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.raycastTarget = false;

        return button;
    }

    private TextMeshProUGUI CreateTextBlock(
        Transform parent, string text, int fontSize,
        float preferredHeight, FontStyles style, Color color)
    {
        GameObject go = CreateUIObject("Text", parent);

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.preferredWidth = 1300f;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Max(10f, fontSize * 0.72f);
        tmp.fontSizeMax = fontSize;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        LayoutElement layoutElement = spacer.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
    }

    private void CreateGlow(Vector2 anchoredPosition, Vector2 size, Color color, float radius)
    {
        GameObject glow = CreateUIObject("Glow", canvasGO.transform);
        RectTransform rect = glow.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta        = size;

        Image image = glow.AddComponent<Image>();
        image.sprite = GetRoundedSprite((int)size.x, (int)size.y, Mathf.RoundToInt(radius));
        image.type   = Image.Type.Sliced;
        image.color  = color;
        image.raycastTarget = false;
    }

    private void CreatePlanet(Vector2 anchoredPosition, float size, Color color)
    {
        GameObject planet = CreateUIObject("Planet", canvasGO.transform);
        RectTransform rect = planet.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta        = new Vector2(size, size);

        Image image = planet.AddComponent<Image>();
        image.sprite = GetRoundedSprite(256, 256, 128);
        image.type   = Image.Type.Sliced;
        image.color  = color;
        image.raycastTarget = false;

        UIFloatingFx fx = planet.AddComponent<UIFloatingFx>();
        fx.amplitude = 6f;
        fx.speed     = Random.Range(0.4f, 1.2f);
    }

    private void CreateHorizontalLine(Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject line = CreateUIObject("Line", canvasGO.transform);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta        = size;

        Image image = line.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void CreateStars(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject star = CreateUIObject("Star_" + i, canvasGO.transform);
            RectTransform rect = star.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);

            float size = Random.Range(8f, 20f);
            rect.sizeDelta        = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(Random.Range(-1440f, 1440f), Random.Range(-760f, 760f));

            Image image = star.AddComponent<Image>();
            image.sprite = GetRoundedSprite(64, 64, 32);
            image.type   = Image.Type.Sliced;
            image.color  = new Color(1f, 1f, 1f, Random.Range(0.08f, 0.25f));
            image.raycastTarget = false;

            UIFloatingFx fx = star.AddComponent<UIFloatingFx>();
            fx.amplitude = Random.Range(1f, 5f);
            fx.speed     = Random.Range(0.4f, 1.5f);
        }
    }

    private void CreateFadeOverlay()
    {
        GameObject fade = CreateUIObject("FadeOverlay", canvasGO.transform);
        RectTransform rect = fade.GetComponent<RectTransform>();
        StretchFull(rect);

        Image image = fade.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        fadeGroup = fade.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ─── Panel navigation ────────────────────────────────────────────────────

    private void ShowSettings()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private void ShowLevelSelect()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    private void OpenLevelSelection()
    {
        ShowLevelSelect();
    }

    // ─── Actions ─────────────────────────────────────────────────────────────

    private void IncreaseVolume()
    {
        AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 0.1f);
        SetVolumeLabel();
    }

    private void DecreaseVolume()
    {
        AudioListener.volume = Mathf.Clamp01(AudioListener.volume - 0.1f);
        SetVolumeLabel();
    }

    private void SetVolumeLabel()
    {
        if (volumeText != null)
        {
            int percent = Mathf.RoundToInt(AudioListener.volume * 100f);
            volumeText.text = "Volume : " + percent + "%";
        }
    }

    private void RecenterMenu()
    {
        TryPlaceMenuInFrontOfCamera();
    }

    private void StartNewGame()
    {
        GameData.Difficulty = DifficultyLevel.Medium;
        StartCoroutine(FadeAndLoadScene(newGameSceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (isLoading)
            yield break;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Nom de scene vide.");
            yield break;
        }

        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError("La scene '" + sceneName + "' n'est pas dans Build Settings. Verifie File > Build Settings.");
            yield break;
        }

        isLoading = true;

        if (fadeGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.8f;
                fadeGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Rounded sprite helper ───────────────────────────────────────────────

    private static Sprite GetRoundedSprite(int width, int height, int radius)
    {
        string key = width + "x" + height + "_r" + radius;

        if (SpriteCache.TryGetValue(key, out Sprite cachedSprite) && cachedSprite != null)
            return cachedSprite;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name       = "RoundedTex_" + key;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[width * height];
        Color32 transparent = new Color32(255, 255, 255, 0);
        Color32 white       = new Color32(255, 255, 255, 255);

        float r   = radius - 0.5f;
        float rSq = r * r;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = false;

                if (x >= radius && x < width - radius)
                    inside = true;
                else if (y >= radius && y < height - radius)
                    inside = true;
                else
                {
                    float cx = x < radius ? radius - 1 : width  - radius;
                    float cy = y < radius ? radius - 1 : height - radius;
                    float dx = x - cx;
                    float dy = y - cy;
                    inside = dx * dx + dy * dy <= rSq;
                }

                pixels[y * width + x] = inside ? white : transparent;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f, 0,
            SpriteMeshType.FullRect,
            border
        );

        SpriteCache[key] = sprite;
        return sprite;
    }
}

// ─── Helper components ────────────────────────────────────────────────────────

public class UIButtonHoverFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float normalScale  = 1f;
    public float hoverScale   = 1.03f;
    public float pressedScale = 0.98f;
    public float speed        = 10f;

    private RectTransform _rectTransform;
    private float _targetScale = 1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _targetScale   = normalScale;
        _rectTransform.localScale = Vector3.one * normalScale;
    }

    private void Update()
    {
        if (_rectTransform == null) return;
        Vector3 target = Vector3.one * _targetScale;
        _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, target, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData e) => _targetScale = hoverScale;
    public void OnPointerExit(PointerEventData e)  => _targetScale = normalScale;
    public void OnPointerDown(PointerEventData e)  => _targetScale = pressedScale;
    public void OnPointerUp(PointerEventData e)    => _targetScale = hoverScale;
}

public class UIFloatingFx : MonoBehaviour
{
    public float amplitude = 5f;
    public float speed     = 1f;

    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private float   _seed;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _basePosition  = _rectTransform.anchoredPosition;
        _seed          = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (_rectTransform == null) return;
        float offsetY = Mathf.Sin((Time.time + _seed) * speed) * amplitude;
        _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, offsetY);
    }
}
