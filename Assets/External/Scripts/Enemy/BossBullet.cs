using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int maxBounce = 2;

    private int bounceCount = 0;
    private Vector2 moveDir;

    void Start()
    {
        // 처음 발사 방향 (총구 방향 기준)
        moveDir = transform.up.normalized;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            return;
        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            bounceCount++;

            if (bounceCount > maxBounce)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 normal = collision.contacts[0].normal;
            moveDir = Vector2.Reflect(moveDir, normal).normalized;
        }
    }
}