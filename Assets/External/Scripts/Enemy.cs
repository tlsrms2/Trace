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
        target = GameObject.FindGameObjectWithTag("Player").transform;
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

    protected virtual void Attack()
    {
        
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Attack();
        }   
    }
}