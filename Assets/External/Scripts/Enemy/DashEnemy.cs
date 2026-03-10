using UnityEngine;
using System.Collections;

public class DashEnemy : Enemy
{
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashCooldown = 3f;

    private float dashTimer;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        dashTimer = 0f;
    }
    protected override void Update()
    {
        base.Update();

        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
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
        StartCoroutine(DashAttackRoutine(dir));
    }

    IEnumerator DashAttackRoutine(Vector2 dir)
    {
        spriteRenderer.color = Color.red;
        float elapsedTime = 0f;
        float waitCooldown = 0.25f;
        while (elapsedTime < waitCooldown)
        {
            elapsedTime += Time.deltaTime;
            yield return GameManager.Instance.CurrentPhase != GamePhase.Paused;
        }
        
        spriteRenderer.color = Color.blue;
        float dashDuration = 0.25f; 
        elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            transform.position += (Vector3)dir * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return GameManager.Instance.CurrentPhase != GamePhase.Paused;
        }
    }
}