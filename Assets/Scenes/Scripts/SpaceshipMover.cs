using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplace le vaisseau.
/// Left stick  : strafe G/D + élévation H/B
/// Right stick : rotation yaw
/// Clavier     : WASD / flèches + QE (test éditeur)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpaceshipMover : MonoBehaviour
{
    [Header("Vitesses")]
    public float strafeSpeed    = 8f;
    public float elevationSpeed = 8f;
    public float yawSpeed       = 50f;

    [Header("Tilt visuel")]
    public float tiltAmount = 15f;
    public float tiltSmooth = 6f;

    private Rigidbody    _rb;
    private Transform    _meshRoot;
    private InputAction  _moveAction;
    private InputAction  _yawAction;
    private Vector2      _move;

    public void Init(Transform meshRoot) => _meshRoot = meshRoot;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity  = false;
        _rb.drag        = 2f;
        _rb.angularDrag = 8f;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _moveAction = new InputAction("ShipMove", InputActionType.Value, expectedControlType: "Vector2");
        _moveAction.AddBinding("<XRController>{LeftHand}/Primary2DAxis");
        _moveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        _moveAction.AddBinding("<Gamepad>/leftStick");
        _moveAction.Enable();

        _yawAction = new InputAction("ShipYaw", InputActionType.Value, expectedControlType: "Vector2");
        _yawAction.AddBinding("<XRController>{RightHand}/Primary2DAxis");
        _yawAction.AddBinding("<XRController>{RightHand}/thumbstick");
        _yawAction.AddBinding("<Gamepad>/rightStick");
        _yawAction.Enable();
    }

    private void OnDestroy()
    {
        _moveAction?.Dispose();
        _yawAction?.Dispose();
    }

    private void Update()
    {
        _move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 yaw = _yawAction?.ReadValue<Vector2>() ?? Vector2.zero;

        // Fallback clavier pour test en éditeur
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  _move.x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) _move.x =  1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    _move.y =  1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  _move.y = -1f;
            if (Keyboard.current.qKey.isPressed) yaw.x = -1f;
            if (Keyboard.current.eKey.isPressed) yaw.x =  1f;
        }

        transform.Rotate(Vector3.up, yaw.x * yawSpeed * Time.deltaTime, Space.World);

        // Tilt visuel sur le mesh (strafe uniquement)
        if (_meshRoot != null)
        {
            Vector3 euler   = _meshRoot.localEulerAngles;
            float targetRoll = -_move.x * tiltAmount;
            float roll       = Mathf.LerpAngle(euler.z, targetRoll, tiltSmooth * Time.deltaTime);
            _meshRoot.localEulerAngles = new Vector3(euler.x, 0f, roll);
        }
    }

    private void FixedUpdate()
    {
        float fwd = SpaceGameManager.Instance != null ? SpaceGameManager.Instance.CurrentSpeed : 14f;

        Vector3 desired = transform.forward * fwd
                        + transform.right   * (_move.x * strafeSpeed)
                        + Vector3.up        * (_move.y * elevationSpeed);

        _rb.velocity = Vector3.Lerp(_rb.velocity, desired, 8f * Time.fixedDeltaTime);
    }
}
