using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AsteroidController : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _lifetime = 20f;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        float scale = Random.Range(0.75f, 1.65f);
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(scale * Random.Range(0.75f, 1.2f), scale, scale * Random.Range(0.75f, 1.2f)));
        transform.rotation = Random.rotation;
    }

    public void Initialize(Vector3 direction, float speed)
    {
        _direction = direction.normalized;
        _speed = speed;
    }

    private void Update()
    {
        transform.Rotate(Vector3.one * 15f * Time.deltaTime, Space.Self);

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        _rb.MovePosition(_rb.position + _direction * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            GameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    public void HitByShot()
    {
        Destroy(gameObject);
    }
}
