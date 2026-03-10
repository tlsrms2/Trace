using UnityEngine;

public class DashEnemy : Enemy
{
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashCooldown = 3f;

    private float dashTimer;

    protected override void Update()
    {
        base.Update();

        dashTimer += Time.deltaTime;

        if (dashTimer >= dashCooldown)
        {
            Dash();
            dashTimer = 0;
        }
    }

    private void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        transform.position += (Vector3)dir * dashSpeed;
    }
}