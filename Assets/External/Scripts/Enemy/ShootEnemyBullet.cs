using UnityEngine;

public class ShootEnemyBullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 5f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector2 direction;
    private Rigidbody2D rb;

    public void Initialize(Vector2 dir, float bulletSpeed, int bulletDamage)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed; // Unity6 기준
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (rb == null)
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }
}
