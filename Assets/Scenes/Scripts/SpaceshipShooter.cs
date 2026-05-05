using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fires a collection beam from the ship.
/// Input Action is created at runtime — no Inspector assignment needed.
/// Right trigger (or Space) shoots; if the ray hits a FoodItem it is collected.
/// </summary>
public class SpaceshipShooter : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private float beamRange = 40f;
    [SerializeField] private float beamDuration = 0.15f;
    [SerializeField] private float cooldown = 0.4f;

    [Header("Beam Visual")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Color beamColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private float beamWidth = 0.08f;

    private LineRenderer _line;
    private InputAction _shootAction;
    private float _cooldownTimer;
    private bool _wasFiring;

    private void Awake()
    {
        BuildInputAction();
        BuildLineRenderer();
    }

    private void BuildInputAction()
    {
        _shootAction = new InputAction("ShipShoot", InputActionType.Button);
        _shootAction.AddBinding("<XRController>{RightHand}/triggerButton");
        _shootAction.AddBinding("<XRController>{RightHand}/trigger");
        _shootAction.AddBinding("<Keyboard>/space");
        _shootAction.AddBinding("<Gamepad>/rightTrigger");
        _shootAction.Enable();
    }

    private void BuildLineRenderer()
    {
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = beamWidth;
        _line.endWidth = beamWidth * 0.3f;
        _line.useWorldSpace = true;
        _line.enabled = false;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material mat = new Material(unlit);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beamColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", beamColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", beamColor * 2.5f);
        }
        _line.material = mat;
    }

    private void OnDestroy()
    {
        _shootAction?.Dispose();
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        bool firing = IsFiring();
        if (firing && !_wasFiring && _cooldownTimer <= 0f)
        {
            _cooldownTimer = cooldown;
            Fire();
        }
        _wasFiring = firing;
    }

    private bool IsFiring()
    {
        if (_shootAction != null && _shootAction.IsPressed())
            return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            return true;
        return false;
    }

    private void Fire()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        Vector3 start = origin.position;
        Vector3 end = start + origin.forward * beamRange;

        if (Physics.Raycast(start, origin.forward, out RaycastHit hit, beamRange, ~0, QueryTriggerInteraction.Collide))
        {
            end = hit.point;
            FoodItem food = hit.collider.GetComponentInParent<FoodItem>();
            if (food != null)
            {
                food.Collect();
            }
            else
            {
                AsteroidController asteroid = hit.collider.GetComponentInParent<AsteroidController>();
                if (asteroid != null)
                    asteroid.HitByShot();
                else
                    hit.collider.GetComponentInParent<SpaceHazard>()?.HitByShot();
            }
        }

        StartCoroutine(ShowBeam(start, end));
    }

    private IEnumerator ShowBeam(Vector3 start, Vector3 end)
    {
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        _line.enabled = true;
        yield return new WaitForSeconds(beamDuration);
        _line.enabled = false;
    }
}
