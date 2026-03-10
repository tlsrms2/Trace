using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class ShootEnemyBullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 5f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector2 savedVelocity;
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
            rb.linearVelocity = direction * speed; 
        }

        StartCoroutine(DestroyAfterTime());
    }

    private void Update()
    {
        if (rb == null) return;
        
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            if (rb.linearVelocity != Vector2.zero)
            {
                savedVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            if (rb.linearVelocity == Vector2.zero)
            {
                rb.linearVelocity = savedVelocity;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Attack"))
        {
            rb.linearVelocity = -rb.linearVelocity * 1.5f;
            direction = rb.linearVelocity.normalized;
            AttackData attackData = gameObject.GetComponent<AttackData>();
            attackData.Damage = 5;
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        float timer = 0f;
        while (timer < lifetime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
