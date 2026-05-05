using UnityEngine;

public class SpaceHazard : MonoBehaviour
{
    [SerializeField] private bool destroyOnShot = true;
    [SerializeField] private bool damagePlayerOnContact = true;

    private void OnTriggerEnter(Collider other)
    {
        if (damagePlayerOnContact && other.CompareTag("Player"))
        {
            GameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (damagePlayerOnContact && collision.collider.CompareTag("Player"))
        {
            GameManager.Instance?.TakeDamage();
            Destroy(gameObject);
        }
    }

    public void HitByShot()
    {
        if (destroyOnShot)
            Destroy(gameObject);
    }
}
