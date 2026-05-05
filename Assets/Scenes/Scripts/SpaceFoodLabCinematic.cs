using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpaceFoodLabCinematic : MonoBehaviour
{
    [Header("Scene suivante")]
    [SerializeField] private string nextSceneName = "Spaceship";

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float fadeDuration = 1.25f;
    [SerializeField] private float endHoldDuration = 0.5f;

    [Header("Texte d'introduction")]
    [SerializeField] private bool playIntroCrawl = true;
    [SerializeField] private float introCrawlDuration = 32f;
    [SerializeField] private string introTitle = "SPACE FOOD WAR";
    [TextArea(8, 16)]
    [SerializeField] private string introText =
        "Il y a deux cents ans, sur la planete Terre, un scientifique fou mena des experiences que personne n'aurait jamais du autoriser.\n\n" +
        "Dans son laboratoire, il chercha a creer une source de vie nouvelle, capable de nourrir les peuples sans fin. Mais la decouverte echappa a tout controle : la nourriture devint vivante.\n\n" +
        "Puis elle apprit a chasser.\n\n" +
        "Ces especes carnivores devasterent la Terre, ville apres ville. Les derniers survivants fuirent vers les stations orbitales et les colonies lointaines, emportant avec eux la memoire d'un monde perdu.\n\n" +
        "Aujourd'hui, leur mission est claire : nettoyer l'espace de ces creations affamees avant qu'elles ne trouvent une nouvelle planete a devorer.";

    [Header("Contr�le")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private bool allowSkip = true;

    [Header("Cam�ra")]
    [SerializeField] private float defaultFov = 55f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);

    private Camera cam;
    private CanvasGroup fadeGroup;
    private CanvasGroup crawlGroup;
    private bool isPlaying;
    private bool skipRequested;
    private readonly List<Shot> shots = new();

    private struct Shot
    {
        public Vector3 from;
        public Vector3 to;
        public Vector3 lookAt;
        public float duration;
        public float fov;

        public Shot(Vector3 from, Vector3 to, Vector3 lookAt, float duration, float fov)
        {
            this.from = from;
            this.to = to;
            this.lookAt = lookAt;
            this.duration = duration;
            this.fov = fov;
        }
    }

    private void Start()
    {
        StartCoroutine(Bootstrap());
    }

    private void Update()
    {
        if (!allowSkip || !isPlaying || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            skipRequested = true;
        }
    }

    private IEnumerator Bootstrap()
    {
        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("SpaceFoodLabCinematic : aucune Main Camera trouv�e.");
            yield break;
        }

        cam.fieldOfView = defaultFov;

        CreateFadeOverlay();

        yield return null;
        yield return null;

        BuildShots();

        if (autoPlayOnStart)
            yield return PlayCinematic();
    }

    [ContextMenu("Play Cinematic")]
    public void PlayFromInspector()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(PlayCinematic());
    }

    private IEnumerator PlayCinematic()
    {
        if (isPlaying)
            yield break;

        isPlaying = true;
        skipRequested = false;

        if (fadeGroup != null)
            fadeGroup.alpha = 1f;

        yield return new WaitForSeconds(initialDelay);
        yield return Fade(1f, 0f, fadeDuration);

        Coroutine crawlCoroutine = null;
        if (playIntroCrawl)
        {
            crawlCoroutine = StartCoroutine(PlayIntroCrawl());
            yield return PlayShotsForDuration(introCrawlDuration);
        }
        else
        {
            yield return PlayShotsForDuration(GetTotalShotsDuration());
        }

        if (crawlCoroutine != null)
            StopCoroutine(crawlCoroutine);

        CleanupIntroCrawl();

        yield return new WaitForSeconds(endHoldDuration);
        yield return Fade(0f, 1f, fadeDuration);

        LoadSceneSafe(nextSceneName);
    }

    private IEnumerator PlayShotsForDuration(float duration)
    {
        if (shots.Count == 0)
            yield break;

        float elapsed = 0f;
        int shotIndex = 0;

        while (elapsed < duration)
        {
            if (skipRequested)
                yield break;

            Shot shot = shots[shotIndex % shots.Count];
            yield return PlayShot(shot);
            elapsed += shot.duration;
            shotIndex++;
        }
    }

    private float GetTotalShotsDuration()
    {
        float total = 0f;
        for (int i = 0; i < shots.Count; i++)
            total += shots[i].duration;

        return total;
    }

    private IEnumerator PlayIntroCrawl()
    {
        RectTransform crawlContent = CreateIntroCrawl();
        float time = 0f;

        Vector2 start = new Vector2(0f, -760f);
        Vector2 end = new Vector2(0f, 1220f);
        crawlContent.anchoredPosition = start;

        while (time < introCrawlDuration)
        {
            if (skipRequested)
                yield break;

            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / introCrawlDuration);
            crawlContent.anchoredPosition = Vector2.Lerp(start, end, t);

            if (crawlGroup != null)
                crawlGroup.alpha = t > 0.92f ? Mathf.InverseLerp(1f, 0.92f, t) : 1f;

            yield return null;
        }
    }

    private RectTransform CreateIntroCrawl()
    {
        GameObject canvasGO = new GameObject("IntroCrawlCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        crawlGroup = canvasGO.GetComponent<CanvasGroup>();
        crawlGroup.alpha = 1f;

        GameObject readabilityPanel = new GameObject("CrawlReadabilityPanel", typeof(RectTransform), typeof(Image));
        readabilityPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = readabilityPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -40f);
        panelRect.sizeDelta = new Vector2(1260f, 1080f);

        Image panelImage = readabilityPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.38f);
        panelImage.raycastTarget = false;

        GameObject content = new GameObject("CrawlContent", typeof(RectTransform));
        content.transform.SetParent(canvasGO.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1120f, 1400f);
        contentRect.localRotation = Quaternion.Euler(62f, 0f, 0f);

        GameObject titleGO = new GameObject("CrawlTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(contentRect, false);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(1120f, 150f);

        TextMeshProUGUI title = titleGO.GetComponent<TextMeshProUGUI>();
        title.text = introTitle;
        title.fontSize = 72f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.84f, 0.28f, 1f);
        title.characterSpacing = 8f;
        title.raycastTarget = false;

        GameObject bodyGO = new GameObject("CrawlBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGO.transform.SetParent(contentRect, false);
        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = new Vector2(0f, -170f);
        bodyRect.sizeDelta = new Vector2(980f, 1220f);

        TextMeshProUGUI body = bodyGO.GetComponent<TextMeshProUGUI>();
        body.text = introText;
        body.fontSize = 46f;
        body.fontStyle = FontStyles.Bold;
        body.alignment = TextAlignmentOptions.Justified;
        body.color = new Color(1f, 0.84f, 0.28f, 1f);
        body.lineSpacing = 12f;
        body.enableWordWrapping = true;
        body.raycastTarget = false;

        return contentRect;
    }

    private void CleanupIntroCrawl()
    {
        if (crawlGroup != null)
            Destroy(crawlGroup.gameObject);

        crawlGroup = null;
    }

    private IEnumerator PlayShot(Shot shot)
    {
        float time = 0f;
        Vector3 pos = shot.from;
        cam.transform.position = pos;

        Vector3 dir = (shot.lookAt - pos).normalized;
        if (dir.sqrMagnitude > 0.0001f)
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        cam.fieldOfView = shot.fov;

        while (time < shot.duration)
        {
            if (skipRequested)
                yield break;

            time += Time.deltaTime;
            yield return null;
        }
    }

    private void BuildShots()
    {
        shots.Clear();

        Transform centralPlatform = FindTransformByName("CentralPlatform");
        Transform mainScreen = FindTransformByName("MainGameScreen");
        Transform foodPod0 = FindTransformByName("FoodPod_0");

        Vector3 center = centralPlatform != null ? centralPlatform.position + lookOffset : new Vector3(0f, 1.2f, 0f);
        Vector3 screen = mainScreen != null ? mainScreen.position + new Vector3(0f, 0.3f, 0f) : new Vector3(0f, 2f, -4f);
        Vector3 podA = foodPod0 != null ? foodPod0.position + new Vector3(0f, 1.3f, 0f) : new Vector3(-2.2f, 1.4f, 4.5f);

        shots.Add(new Shot(
            new Vector3(0f, 3.6f, 10.5f),
            new Vector3(0f, 3.6f, 10.5f),
            center,
            4.5f,
            56f
        ));

        shots.Add(new Shot(
            new Vector3(-3.8f, 2.4f, 6.2f),
            new Vector3(-3.8f, 2.4f, 6.2f),
            podA,
            3.5f,
            48f
        ));

        shots.Add(new Shot(
            new Vector3(0f, 2.3f, 2.5f),
            new Vector3(0f, 2.3f, 2.5f),
            screen,
            3.5f,
            42f
        ));
    }

    private Transform FindTransformByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.transform : null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeGroup == null)
            yield break;

        float t = 0f;
        fadeGroup.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            fadeGroup.alpha = Mathf.Lerp(from, to, EaseInOut(p));
            yield return null;
        }

        fadeGroup.alpha = to;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasGO = new GameObject("CinematicFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject fadeGO = new GameObject("Fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        fadeGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = fadeGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = fadeGO.GetComponent<Image>();
        img.color = Color.black;

        fadeGroup = fadeGO.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 1f;
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Nom de sc�ne vide.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
