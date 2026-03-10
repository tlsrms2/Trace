using UnityEngine;
using System.Collections;

public class ShootEnemy : Enemy
{
    [SerializeField] private GameObject bulletPrefab;

    [SerializeField] private float moveDuration = 1f;   // 1초 이동
    [SerializeField] private float stopDuration = 0.5f; // 발사 후 정지
    [SerializeField] private float bulletSpeed = 5f;

    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 1f;   // Dash 시간

    protected override void Start()
    {
        base.Start(); // 부모 Start 호출 -> target 설정

        // 코루틴 시작
        StartCoroutine(PatternCoroutine());
    }

    private IEnumerator PatternCoroutine()
    {
        while (true)
        {
            // 1️⃣ 이동
            float timer = 0f;
            while (timer < moveDuration)
            {
                MoveTowardsPlayer(speed);
                timer += Time.deltaTime;
                yield return null;
            }

            // 2️⃣ 정지
            yield return new WaitForSeconds(stopDuration);

            // 3️⃣ 발사
            Shoot();

            // 4️⃣ 발사 후 정지
            yield return new WaitForSeconds(stopDuration);

            // 5️⃣ Dash 이동
            timer = 0f;
            while (timer < dashDuration)
            {
                MoveTowardsPlayer(dashSpeed);
                timer += Time.deltaTime;
                yield return null;
            }

            // 6️⃣ Dash 후 잠깐 대기 (1초)
            yield return new WaitForSeconds(1f);
        }
    }
    // speed를 인자로 받아서 이동 지속 적용
    private void MoveTowardsPlayer(float currentSpeed)
    {
        if (target == null) return;

        Vector2 dir = (target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * currentSpeed * Time.deltaTime);
    }

    // ...existing code...

    private void Shoot()
    {
        if (target == null) return;

        Vector2 dir = (target.position - transform.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;  // Changed from velocity to linearVelocity
    }

}