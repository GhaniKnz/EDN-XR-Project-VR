using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tire un rayon depuis le nez du vaisseau.
/// Trigger droit ou Espace pour tirer.
/// Touche un SpaceFoodPickup  → collecte.
/// Touche un SpaceHazardItem  → détruit.
/// </summary>
public class SpaceBeamShooter : MonoBehaviour
{
    [Header("Beam")]
    public float beamRange    = 45f;
    public float beamDuration = 0.15f;
    public float cooldown     = 0.35f;
    public Color beamColor    = new Color(0.3f, 0.9f, 1f);
    public float beamWidth    = 0.07f;

    private Transform    _firePoint;
    private LineRenderer _line;
    private InputAction  _shootAction;
    private float        _cooldownTimer;
    private bool         _wasFiring;

    public void Init(Transform firePoint) => _firePoint = firePoint;

    private void Awake()
    {
        _shootAction = new InputAction("Shoot", InputActionType.Button);
        _shootAction.AddBinding("<XRController>{RightHand}/triggerButton");
        _shootAction.AddBinding("<XRController>{RightHand}/trigger");
        _shootAction.AddBinding("<Keyboard>/space");
        _shootAction.AddBinding("<Gamepad>/rightTrigger");
        _shootAction.Enable();

        BuildLineRenderer();
    }

    private void BuildLineRenderer()
    {
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth    = beamWidth;
        _line.endWidth      = beamWidth * 0.25f;
        _line.useWorldSpace = true;
        _line.enabled       = false;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (sh == null) return;

        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beamColor);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     beamColor);
        _line.material = mat;
    }

    private void OnDestroy() => _shootAction?.Dispose();

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        bool firing = (_shootAction != null && _shootAction.IsPressed())
                   || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);

        if (firing && !_wasFiring && _cooldownTimer <= 0f)
        {
            _cooldownTimer = cooldown;
            Fire();
        }
        _wasFiring = firing;
    }

    private void Fire()
    {
        Transform origin = _firePoint != null ? _firePoint : transform;
        Vector3 start    = origin.position;
        Vector3 end      = start + origin.forward * beamRange;

        if (Physics.Raycast(start, origin.forward, out RaycastHit hit, beamRange, ~0, QueryTriggerInteraction.Collide))
        {
            end = hit.point;
            SpaceFoodPickup food = hit.collider.GetComponentInParent<SpaceFoodPickup>();
            if (food != null)
                food.HitByShot();
            else
                hit.collider.GetComponentInParent<SpaceHazardItem>()?.HitByShot();
        }

        StartCoroutine(ShowBeam(start, end));
    }

    private IEnumerator ShowBeam(Vector3 from, Vector3 to)
    {
        _line.SetPosition(0, from);
        _line.SetPosition(1, to);
        _line.enabled = true;
        yield return new WaitForSeconds(beamDuration);
        _line.enabled = false;
    }
}
