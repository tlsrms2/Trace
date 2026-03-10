using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected float Hp;

    protected Transform target;
    protected Collider2D col;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        // Player 찾기
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
            Debug.LogError("Player 태그가 있는 오브젝트를 찾을 수 없습니다!");
    }

    protected virtual void Update()
    {
        Move();
    }

    protected virtual void Move()
    {
        if (Vector2.Distance(transform.position, target.position) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어 데미지 처리 로직
        }   
    }
}