using UnityEngine;

public class ShootEnemy : Enemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float bulletSpeed = 5f;

    private float shootTimer;

    protected override void Update()
    {
        base.Update();

        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            shootTimer += Time.deltaTime;

        if (shootTimer >= shootCooldown)
        {
            Shoot();
            shootTimer = 0;
        }
    }

    void Shoot()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<ShootEnemyBullet>().Shoot(dir * bulletSpeed);
    }
}