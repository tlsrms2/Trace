using UnityEngine;

public class BossEnemy : Enemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float dashSpeed = 8f;

    private float attackTimer;

    protected override void Update()
    {
        base.Update();

        attackTimer += Time.deltaTime;

        if (attackTimer > 2f)
        {
            Dash();
        }

        if (attackTimer > 4f)
        {
            Shoot();
            attackTimer = 0;
        }
    }

    void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        transform.position += (Vector3)dir * dashSpeed;
    }

    void Shoot()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * 6f;
    }
}