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
    [SerializeField] private string levelSelectSceneName = "";

    [Header("Placement")]
    [SerializeField] private float distanceFromCamera = 2.2f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.05f, 0f);
    [SerializeField] private float worldScale = 0.0015f;

    [Header("Textes")]
    [SerializeField] private string gameTitle = "GALACTIC FOOD LAB";
    [SerializeField] private string subtitle = "Science, nourriture et exploration spatiale";

    [Header("Couleurs")]
    [SerializeField] private Color backgroundTint = new Color(0.02f, 0.05f, 0.10f, 0.55f);
    [SerializeField] private Color cardColor = new Color(0.05f, 0.10f, 0.18f, 0.94f);
    [SerializeField] private Color cardSecondaryColor = new Color(0.08f, 0.15f, 0.26f, 0.65f);
    [SerializeField] private Color buttonColor = new Color(0.07f, 0.14f, 0.24f, 1f);
    [SerializeField] private Color buttonHighlightColor = new Color(0.14f, 0.28f, 0.46f, 1f);
    [SerializeField] private Color accentColor = new Color(0.30f, 0.82f, 1f, 1f);

    private GameObject canvasGO;
    private GameObject mainPanel;
    private GameObject settingsPanel;
    private TextMeshProUGUI volumeText;
    private CanvasGroup fadeGroup;
    private bool isLoading;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private void Start()
    {
        BuildMenu();
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
            {
                Destroy(module);
            }

            existing.gameObject.AddComponent<XRUIInputModule>();
        }
    }

    private bool TryPlaceMenuInFrontOfCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("VRMainMenuBuilder : aucune MainCamera trouvée. Vérifie le XR Origin et le tag MainCamera.");
            return false;
        }

        Vector3 worldOffset =
            cam.transform.right * localOffset.x +
            cam.transform.up * localOffset.y +
            cam.transform.forward * localOffset.z;

        transform.position = cam.transform.position + cam.transform.forward * distanceFromCamera + worldOffset;
        transform.LookAt(cam.transform.position, cam.transform.up);
        transform.Rotate(0f, 180f, 0f);

        return true;
    }

    private void ClearExistingChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
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
        scaler.referenceResolution = new Vector2(1600f, 900f);

        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1600f, 900f);

        canvasGO.transform.localScale = Vector3.one * worldScale;
    }

    private void CreateBackgroundDecor()
    {
        GameObject dim = CreateUIObject("DimBackground", canvasGO.transform);
        RectTransform dimRect = dim.GetComponent<RectTransform>();
        StretchFull(dimRect);

        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = backgroundTint;
        dimImage.raycastTarget = false;

        CreateGlow(new Vector2(0f, 40f), new Vector2(980f, 980f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.07f), 490f);
        CreateGlow(new Vector2(-420f, 250f), new Vector2(260f, 260f), new Color(0.45f, 0.85f, 1f, 0.08f), 130f);
        CreateGlow(new Vector2(450f, -220f), new Vector2(180f, 180f), new Color(0.25f, 0.55f, 1f, 0.09f), 90f);

        CreatePlanet(new Vector2(-470f, 180f), 120f, new Color(0.40f, 0.95f, 1f, 0.11f));
        CreatePlanet(new Vector2(500f, -160f), 85f, new Color(0.30f, 0.55f, 1f, 0.10f));

        CreateHorizontalLine(new Vector2(0f, 0f), new Vector2(1200f, 2f), new Color(0.7f, 0.95f, 1f, 0.08f));
        CreateHorizontalLine(new Vector2(0f, -250f), new Vector2(1000f, 2f), new Color(0.5f, 0.8f, 1f, 0.05f));

        CreateStars(34);
    }

    private void CreateCardAndPanels()
    {
        GameObject card = CreateUIObject("MainCard", canvasGO.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(930f, 760f);

        Image cardImage = card.AddComponent<Image>();
        cardImage.sprite = GetRoundedSprite(1024, 768, 70);
        cardImage.type = Image.Type.Sliced;
        cardImage.color = cardColor;

        Shadow outerShadow = card.AddComponent<Shadow>();
        outerShadow.effectColor = new Color(0f, 0f, 0f, 0.40f);
        outerShadow.effectDistance = new Vector2(0f, -18f);

        UIFloatingFx floatFx = card.AddComponent<UIFloatingFx>();
        floatFx.amplitude = 4f;
        floatFx.speed = 0.8f;

        GameObject innerCard = CreateUIObject("InnerCard", card.transform);
        RectTransform innerRect = innerCard.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.pivot = new Vector2(0.5f, 0.5f);
        innerRect.sizeDelta = new Vector2(880f, 715f);

        Image innerImage = innerCard.AddComponent<Image>();
        innerImage.sprite = GetRoundedSprite(1024, 768, 58);
        innerImage.type = Image.Type.Sliced;
        innerImage.color = cardSecondaryColor;
        innerImage.raycastTarget = false;

        CreateTopAccent(card.transform);

        GameObject contentRoot = CreateUIObject("ContentRoot", card.transform);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(42f, 36f);
        contentRect.offsetMax = new Vector2(-42f, -36f);

        mainPanel = CreatePanel(contentRoot.transform, "MainPanel");
        settingsPanel = CreatePanel(contentRoot.transform, "SettingsPanel");
        settingsPanel.SetActive(false);

        BuildMainPanel();
        BuildSettingsPanel();
    }

    private void CreateTopAccent(Transform parent)
    {
        GameObject barGlow = CreateUIObject("TopGlow", parent);
        RectTransform glowRect = barGlow.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 1f);
        glowRect.anchorMax = new Vector2(0.5f, 1f);
        glowRect.pivot = new Vector2(0.5f, 1f);
        glowRect.anchoredPosition = new Vector2(0f, -8f);
        glowRect.sizeDelta = new Vector2(720f, 22f);

        Image glowImage = barGlow.AddComponent<Image>();
        glowImage.sprite = GetRoundedSprite(512, 64, 32);
        glowImage.type = Image.Type.Sliced;
        glowImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.28f);
        glowImage.raycastTarget = false;

        GameObject bar = CreateUIObject("TopBar", parent);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -12f);
        barRect.sizeDelta = new Vector2(440f, 10f);

        Image barImage = bar.AddComponent<Image>();
        barImage.sprite = GetRoundedSprite(512, 64, 32);
        barImage.type = Image.Type.Sliced;
        barImage.color = accentColor;
        barImage.raycastTarget = false;
    }

    private GameObject CreatePanel(Transform parent, string panelName)
    {
        GameObject panel = CreateUIObject(panelName, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        StretchFull(rect);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 16f;
        layout.padding = new RectOffset(18, 18, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel;
    }

    private void BuildMainPanel()
    {
        CreateBadge(mainPanel.transform, "VR SPACE EXPERIENCE");

        TextMeshProUGUI title = CreateTextBlock(mainPanel.transform, gameTitle, 58, 88f, FontStyles.Bold, Color.white);
        title.characterSpacing = 2f;

        CreateTextBlock(mainPanel.transform, subtitle, 24, 50f, FontStyles.Normal, new Color(0.84f, 0.93f, 1f, 1f));
        CreateSpacer(mainPanel.transform, 10f);

        CreateInfoPill(mainPanel.transform, "Laboratoire spatial • cuisine futuriste • exploration immersive");

        CreateSpacer(mainPanel.transform, 14f);

        CreateButton(mainPanel.transform, "Nouvelle partie", StartNewGame);
        CreateButton(mainPanel.transform, "Sélection niveau", OpenLevelSelection);
        CreateButton(mainPanel.transform, "Réglages", ShowSettings);
        CreateButton(mainPanel.transform, "Quitter", QuitGame);

        CreateSpacer(mainPanel.transform, 18f);

        CreateTextBlock(
            mainPanel.transform,
            "Test clavier : N = Nouvelle partie | R = Réglages | Échap = Retour | Q = Quitter",
            20,
            42f,
            FontStyles.Italic,
            new Color(0.70f, 0.83f, 0.96f, 1f)
        );
    }

    private void BuildSettingsPanel()
    {
        CreateBadge(settingsPanel.transform, "SETTINGS");

        TextMeshProUGUI title = CreateTextBlock(settingsPanel.transform, "Réglages", 54, 80f, FontStyles.Bold, Color.white);
        title.characterSpacing = 1f;

        CreateTextBlock(settingsPanel.transform, "Menu de test hors casque VR.", 24, 42f, FontStyles.Normal, new Color(0.84f, 0.93f, 1f, 1f));
        CreateSpacer(settingsPanel.transform, 10f);

        volumeText = CreateTextBlock(settingsPanel.transform, "", 30, 54f, FontStyles.Bold, accentColor);

        CreateButton(settingsPanel.transform, "Volume +", IncreaseVolume);
        CreateButton(settingsPanel.transform, "Volume -", DecreaseVolume);
        CreateButton(settingsPanel.transform, "Recentrer le menu", RecenterMenu);
        CreateButton(settingsPanel.transform, "Retour", ShowMainMenu);

        CreateSpacer(settingsPanel.transform, 14f);

        CreateTextBlock(
            settingsPanel.transform,
            "Quand tu auras le casque, tu pourras interagir avec ce menu en VR via les ray interactors.",
            19,
            50f,
            FontStyles.Italic,
            new Color(0.70f, 0.83f, 0.96f, 1f)
        );
    }

    private void CreateBadge(Transform parent, string text)
    {
        GameObject badge = CreateUIObject("Badge", parent);

        LayoutElement layout = badge.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
        layout.preferredWidth = 290f;

        Image badgeImage = badge.AddComponent<Image>();
        badgeImage.sprite = GetRoundedSprite(512, 128, 64);
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.14f);

        Shadow badgeShadow = badge.AddComponent<Shadow>();
        badgeShadow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f);
        badgeShadow.effectDistance = new Vector2(0f, 0f);

        GameObject labelGO = CreateUIObject("BadgeText", badge.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = accentColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 2f;
    }

    private void CreateInfoPill(Transform parent, string text)
    {
        GameObject pill = CreateUIObject("InfoPill", parent);

        LayoutElement layout = pill.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;
        layout.preferredWidth = 600f;

        Image pillImage = pill.AddComponent<Image>();
        pillImage.sprite = GetRoundedSprite(1024, 128, 64);
        pillImage.type = Image.Type.Sliced;
        pillImage.color = new Color(1f, 1f, 1f, 0.05f);
        pillImage.raycastTarget = false;

        GameObject labelGO = CreateUIObject("InfoText", pill.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = new Color(0.86f, 0.95f, 1f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick)
    {
        GameObject buttonGO = CreateUIObject(label + "_Button", parent);

        LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 88f;

        Image image = buttonGO.AddComponent<Image>();
        image.sprite = GetRoundedSprite(1024, 256, 54);
        image.type = Image.Type.Sliced;
        image.color = buttonColor;

        Shadow shadow = buttonGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
        shadow.effectDistance = new Vector2(0f, -10f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.selectedColor = buttonHighlightColor;
        colors.pressedColor = accentColor * 0.85f;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        UIButtonHoverFx hoverFx = buttonGO.AddComponent<UIButtonHoverFx>();
        hoverFx.hoverScale = 1.035f;
        hoverFx.normalScale = 1f;
        hoverFx.speed = 10f;

        button.onClick.AddListener(onClick);

        GameObject accentLeft = CreateUIObject("AccentLeft", buttonGO.transform);
        RectTransform accentRect = accentLeft.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0.5f);
        accentRect.anchorMax = new Vector2(0f, 0.5f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = new Vector2(14f, 0f);
        accentRect.sizeDelta = new Vector2(12f, 48f);

        Image accentImage = accentLeft.AddComponent<Image>();
        accentImage.sprite = GetRoundedSprite(128, 512, 64);
        accentImage.type = Image.Type.Sliced;
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;

        GameObject labelGO = CreateUIObject("Label", buttonGO.transform);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        StretchFull(labelRect);
        labelRect.offsetMin = new Vector2(40f, 0f);
        labelRect.offsetMax = new Vector2(-70f, 0f);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        GameObject arrowGO = CreateUIObject("Arrow", buttonGO.transform);
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-24f, 0f);
        arrowRect.sizeDelta = new Vector2(28f, 28f);

        TextMeshProUGUI arrow = arrowGO.AddComponent<TextMeshProUGUI>();
        arrow.text = "›";
        arrow.fontSize = 42;
        arrow.fontStyle = FontStyles.Bold;
        arrow.color = accentColor;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.raycastTarget = false;

        return button;
    }

    private TextMeshProUGUI CreateTextBlock(
        Transform parent,
        string text,
        int fontSize,
        float preferredHeight,
        FontStyles style,
        Color color)
    {
        GameObject go = CreateUIObject("Text", parent);

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;

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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = glow.AddComponent<Image>();
        image.sprite = GetRoundedSprite((int)size.x, (int)size.y, Mathf.RoundToInt(radius));
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
    }

    private void CreatePlanet(Vector2 anchoredPosition, float size, Color color)
    {
        GameObject planet = CreateUIObject("Planet", canvasGO.transform);
        RectTransform rect = planet.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);

        Image image = planet.AddComponent<Image>();
        image.sprite = GetRoundedSprite(256, 256, 128);
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        UIFloatingFx fx = planet.AddComponent<UIFloatingFx>();
        fx.amplitude = 6f;
        fx.speed = Random.Range(0.4f, 1.2f);
    }

    private void CreateHorizontalLine(Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject line = CreateUIObject("Line", canvasGO.transform);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

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
            rect.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(4f, 10f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(Random.Range(-720f, 720f), Random.Range(-380f, 380f));

            Image image = star.AddComponent<Image>();
            image.sprite = GetRoundedSprite(64, 64, 32);
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, Random.Range(0.08f, 0.25f));
            image.raycastTarget = false;

            UIFloatingFx fx = star.AddComponent<UIFloatingFx>();
            fx.amplitude = Random.Range(1f, 5f);
            fx.speed = Random.Range(0.4f, 1.5f);
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

    private void ShowSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

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
        StartCoroutine(FadeAndLoadScene(newGameSceneName));
    }

    private void OpenLevelSelection()
    {
        if (string.IsNullOrWhiteSpace(levelSelectSceneName))
        {
            Debug.LogWarning("Aucune scène de sélection de niveau n'est renseignée.");
            return;
        }

        StartCoroutine(FadeAndLoadScene(levelSelectSceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (isLoading)
            yield break;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Nom de scène vide.");
            yield break;
        }

        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError("La scène '" + sceneName + "' n'est pas dans la Scene List / Build Settings.");
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

    private static Sprite GetRoundedSprite(int width, int height, int radius)
    {
        string key = width + "x" + height + "_r" + radius;

        if (SpriteCache.TryGetValue(key, out Sprite cachedSprite) && cachedSprite != null)
            return cachedSprite;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "RoundedTex_" + key;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[width * height];
        Color32 transparent = new Color32(255, 255, 255, 0);
        Color32 white = new Color32(255, 255, 255, 255);

        float r = radius - 0.5f;
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
                    float cx = x < radius ? radius - 1 : width - radius;
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
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );

        SpriteCache[key] = sprite;
        return sprite;
    }
}

public class UIButtonHoverFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float normalScale = 1f;
    public float hoverScale = 1.03f;
    public float pressedScale = 0.98f;
    public float speed = 10f;

    private RectTransform rectTransform;
    private float targetScale = 1f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetScale = normalScale;
        rectTransform.localScale = Vector3.one * normalScale;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        Vector3 target = Vector3.one * targetScale;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, target, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hoverScale;
    }
}

public class UIFloatingFx : MonoBehaviour
{
    public float amplitude = 5f;
    public float speed = 1f;

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private float seed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        basePosition = rectTransform.anchoredPosition;
        seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float offsetY = Mathf.Sin((Time.time + seed) * speed) * amplitude;
        rectTransform.anchoredPosition = basePosition + new Vector2(0f, offsetY);
    }
}