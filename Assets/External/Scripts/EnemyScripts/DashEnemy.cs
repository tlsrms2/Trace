using UnityEngine;
<<<<<<< Updated upstream
using System.Collections;
=======
>>>>>>> Stashed changes

public class DashEnemy : Enemy
{
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashCooldown = 3f;

    private float dashTimer;
<<<<<<< Updated upstream
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        dashTimer = 0f;
    }
=======

>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
    private void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;
        StartCoroutine(DashAttackRoutine(dir));
    }

    IEnumerator DashAttackRoutine(Vector2 dir)
    {
        spriteRenderer.color = Color.red; 
        yield return new WaitForSeconds(0.25f);
        
        spriteRenderer.color = Color.blue;
        float dashDuration = 0.25f; 
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            transform.position += (Vector3)dir * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
=======

    private void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        transform.position += (Vector3)dir * dashSpeed;
>>>>>>> Stashed changes
    }
}