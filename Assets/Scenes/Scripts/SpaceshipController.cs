using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3rd-person spaceship controller for VR.
/// Input Actions are created at runtime — no Inspector assignment needed.
///
/// Left stick  : strafe left/right (X) + elevation up/down (Y)
/// Right stick : yaw left/right (X)
/// Keyboard    : WASD = move, QE = yaw (editor testing)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float strafeSpeed = 8f;
    [SerializeField] private float elevationSpeed = 8f;
    [SerializeField] private float yawSpeed = 60f;
    [SerializeField] private float autoSpeedFallback = 14f;

    [Header("Tilt Visual")]
    [SerializeField] private Transform shipMesh;
    [SerializeField] private float tiltAmount = 15f;
    [SerializeField] private float pitchAmount = 10f;
    [SerializeField] private float tiltSmoothing = 6f;

    private Rigidbody _rb;
    private InputAction _moveAction;
    private InputAction _lookAction;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.drag = 2f;
        _rb.angularDrag = 5f;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BuildInputActions();
    }

    private void BuildInputActions()
    {
        // Left thumbstick — forward/strafe
        _moveAction = new InputAction("ShipMove", InputActionType.Value);
        _moveAction.expectedControlType = "Vector2";
        _moveAction.AddBinding("<XRController>{LeftHand}/Primary2DAxis");
        _moveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        _moveAction.AddBinding("<Gamepad>/leftStick");
        _moveAction.Enable();

        // Right thumbstick — yaw / elevation
        _lookAction = new InputAction("ShipLook", InputActionType.Value);
        _lookAction.expectedControlType = "Vector2";
        _lookAction.AddBinding("<XRController>{RightHand}/Primary2DAxis");
        _lookAction.AddBinding("<XRController>{RightHand}/thumbstick");
        _lookAction.AddBinding("<Gamepad>/rightStick");
        _lookAction.Enable();
    }

    private void OnDestroy()
    {
        _moveAction?.Dispose();
        _lookAction?.Dispose();
    }

    private void Update()
    {
        Vector2 move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 look = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        // Keyboard fallback for editor testing
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move.y = Mathf.Max(move.y, 1f);
            if (Keyboard.current.sKey.isPressed) move.y = Mathf.Min(move.y, -1f);
            if (Keyboard.current.upArrowKey.isPressed) move.y = Mathf.Max(move.y, 1f);
            if (Keyboard.current.downArrowKey.isPressed) move.y = Mathf.Min(move.y, -1f);
            if (Keyboard.current.aKey.isPressed) move.x = Mathf.Min(move.x, -1f);
            if (Keyboard.current.dKey.isPressed) move.x = Mathf.Max(move.x, 1f);
            if (Keyboard.current.leftArrowKey.isPressed) move.x = Mathf.Min(move.x, -1f);
            if (Keyboard.current.rightArrowKey.isPressed) move.x = Mathf.Max(move.x, 1f);
            if (Keyboard.current.qKey.isPressed) look.x = Mathf.Min(look.x, -1f);
            if (Keyboard.current.eKey.isPressed) look.x = Mathf.Max(look.x, 1f);
        }

        // Yaw rotation
        transform.Rotate(Vector3.up, look.x * yawSpeed * Time.deltaTime, Space.World);

        // Visual tilt on strafe
        if (shipMesh != null)
        {
            float targetRoll = -move.x * tiltAmount;
            float targetPitch = move.y * pitchAmount;
            Vector3 euler = shipMesh.localEulerAngles;
            float roll = Mathf.LerpAngle(euler.z, targetRoll, tiltSmoothing * Time.deltaTime);
            float pitch = Mathf.LerpAngle(euler.x, targetPitch, tiltSmoothing * Time.deltaTime);
            shipMesh.localEulerAngles = new Vector3(pitch, 0f, roll);
        }

        // Store for FixedUpdate
        _cachedMove = move;
    }

    private Vector2 _cachedMove;

    private void FixedUpdate()
    {
        float forwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentShipSpeed : autoSpeedFallback;
        Vector3 desiredVelocity = transform.forward * forwardSpeed
                                + transform.right * (_cachedMove.x * strafeSpeed)
                                + Vector3.up * (_cachedMove.y * elevationSpeed);

        _rb.velocity = Vector3.Lerp(_rb.velocity, desiredVelocity, 8f * Time.fixedDeltaTime);
    }

}
