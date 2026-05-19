using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpaceHazardItem : MonoBehaviour
{
    private Vector3   _dir;
    private float     _speed;
    private float     _lifetime = 18f;
    private Rigidbody _rb;

    public void Init(Vector3 direction, float speed)
    {
        _dir   = direction.normalized;
        _speed = speed;
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity  = false;
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        float s = Random.Range(0.7f, 1.8f);
        transform.localScale = Vector3.Scale(
            transform.localScale,
            new Vector3(s * Random.Range(0.8f, 1.2f), s, s * Random.Range(0.8f, 1.2f))
        );
        transform.rotation = Random.rotation;
    }

    private void Update()
    {
        transform.Rotate(Vector3.one * 14f * Time.deltaTime, Space.Self);
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f) Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (_rb != null)
            _rb.MovePosition(_rb.position + _dir * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SpaceGameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Player"))
        {
            SpaceGameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    public void HitByShot() => Destroy(gameObject);
}
