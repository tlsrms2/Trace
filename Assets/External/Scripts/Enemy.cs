using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float speed;

    protected Transform target;

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
}