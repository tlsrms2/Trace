using UnityEngine;
using System; // Action 사용을 위해 추가

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected float increaseSpeed;
    [SerializeField] protected float Hp;
    [SerializeField] private GameObject destroyParticle;
    
    // --- 추가된 부분 ---
    public float MaxHp { get; private set; } 
    public event Action<float, float> OnHpChanged; // <현재 체력, 최대 체력>
    // -------------------

    protected Transform target;
    protected Collider2D col;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        
        // 스폰될 때 초기 체력을 최대 체력으로 기억
        MaxHp = Hp; 
    }

    protected virtual void Start()
    {
        increaseSpeed = 0.1f;
    }

    protected virtual void Update()
    {
        if (target && GameManager.Instance.CurrentPhase != GamePhase.Paused && Vector2.Distance(transform.position, target.position) > 0.1f)    
        {
            Move();
            speed += increaseSpeed * Time.deltaTime;
        }
    }

    protected virtual void Move()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AttackData attack;
        float beforeHp = Hp;
        if (collision.TryGetComponent(out attack))
        {
            Hp -= attack.Damage;

            // --- 추가된 부분: 체력이 깎일 때마다 이벤트 발생 ---
            OnHpChanged?.Invoke(Hp, MaxHp);
            // ---------------------------------------------------

            if (Hp <= 0)
            {
                WaveManager.Instance.OnEnemyKilled();
                var particle = Instantiate(destroyParticle, transform.position, Quaternion.identity);
                ParticleSystem ps = particle.GetComponent<ParticleSystem>();
                var main = ps.main;
                main.startColor = spriteRenderer.color;
                Destroy(gameObject);
                if (collision.gameObject.name == "FilledShape")
                {
                    // GameTimer.Instance.ReduceTime(2);
                }
            }
            else if (beforeHp > Hp)
            {
                ParticleSystem hitEffect = Instantiate(destroyParticle, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
                hitEffect.Play();
            }
        }
    }
}