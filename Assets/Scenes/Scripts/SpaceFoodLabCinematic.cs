using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpaceFoodLabCinematic : MonoBehaviour
{
    [Header("Scene suivante")]
    [SerializeField] private string nextSceneName = "Vr_scene";

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float fadeDuration = 1.25f;
    [SerializeField] private float endHoldDuration = 0.5f;

    [Header("Contrôle")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private bool allowSkip = true;

    [Header("Caméra")]
    [SerializeField] private float defaultFov = 55f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);

    private Camera cam;
    private CanvasGroup fadeGroup;
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
            Debug.LogError("SpaceFoodLabCinematic : aucune Main Camera trouvée.");
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

        for (int i = 0; i < shots.Count; i++)
        {
            if (skipRequested)
                break;

            yield return PlayShot(shots[i]);
        }

        yield return new WaitForSeconds(endHoldDuration);
        yield return Fade(0f, 1f, fadeDuration);

        LoadSceneSafe(nextSceneName);
    }

    private IEnumerator PlayShot(Shot shot)
    {
        float time = 0f;
        Quaternion startRot = cam.transform.rotation;

        while (time < shot.duration)
        {
            if (skipRequested)
                yield break;

            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / shot.duration);
            float eased = EaseInOut(t);

            Vector3 pos = Vector3.Lerp(shot.from, shot.to, eased);

            float driftY = Mathf.Sin(Time.time * 0.9f) * 0.03f;
            float driftX = Mathf.Cos(Time.time * 0.7f) * 0.02f;
            pos += new Vector3(driftX, driftY, 0f);

            cam.transform.position = pos;

            Vector3 dir = (shot.lookAt - pos).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
            }

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, shot.fov, Time.deltaTime * 3f);

            yield return null;
        }

        cam.transform.position = shot.to;

        Vector3 finalDir = (shot.lookAt - shot.to).normalized;
        if (finalDir.sqrMagnitude > 0.0001f)
            cam.transform.rotation = Quaternion.LookRotation(finalDir, Vector3.up);

        cam.fieldOfView = shot.fov;
    }

    private void BuildShots()
    {
        shots.Clear();

        Transform centralPlatform = FindTransformByName("CentralPlatform");
        Transform mainScreen = FindTransformByName("MainGameScreen");
        Transform foodPod0 = FindTransformByName("FoodPod_0");
        Transform foodPod1 = FindTransformByName("FoodPod_1");
        Transform lab0 = FindTransformByName("LabStation_0");
        Transform lab1 = FindTransformByName("LabStation_1");

        Vector3 center = centralPlatform != null ? centralPlatform.position + lookOffset : new Vector3(0f, 1.2f, 0f);
        Vector3 screen = mainScreen != null ? mainScreen.position + new Vector3(0f, 0.3f, 0f) : new Vector3(0f, 2f, -4f);
        Vector3 podA = foodPod0 != null ? foodPod0.position + new Vector3(0f, 1.3f, 0f) : new Vector3(-2.2f, 1.4f, 4.5f);
        Vector3 podB = foodPod1 != null ? foodPod1.position + new Vector3(0f, 1.3f, 0f) : new Vector3(0f, 1.4f, 5f);
        Vector3 stationA = lab0 != null ? lab0.position + new Vector3(0f, 1.2f, 0f) : new Vector3(-4.8f, 1.2f, -1.8f);
        Vector3 stationB = lab1 != null ? lab1.position + new Vector3(0f, 1.2f, 0f) : new Vector3(4.8f, 1.2f, -1.8f);

        shots.Add(new Shot(
            new Vector3(0f, 4.6f, 13.5f),
            new Vector3(0f, 3.8f, 8.8f),
            center,
            4.0f,
            58f
        ));

        shots.Add(new Shot(
            new Vector3(-8.5f, 2.8f, 3.5f),
            new Vector3(-4.8f, 2.4f, 1.5f),
            stationA,
            4.2f,
            52f
        ));

        shots.Add(new Shot(
            new Vector3(6.4f, 2.6f, 4.8f),
            new Vector3(2.6f, 2.1f, 3.2f),
            podA,
            4.0f,
            48f
        ));

        shots.Add(new Shot(
            new Vector3(-1.8f, 2.0f, 6.8f),
            new Vector3(1.8f, 2.0f, 6.2f),
            podB,
            3.8f,
            46f
        ));

        shots.Add(new Shot(
            new Vector3(4.8f, 2.7f, -1.8f),
            new Vector3(1.8f, 2.2f, -2.8f),
            stationB,
            3.8f,
            50f
        ));

        shots.Add(new Shot(
            new Vector3(0f, 2.3f, 1.2f),
            new Vector3(0f, 2.0f, -1.8f),
            screen,
            4.2f,
            38f
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
            Debug.LogError("Nom de scène vide.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}