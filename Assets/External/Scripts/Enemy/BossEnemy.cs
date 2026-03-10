using UnityEngine;
using System.Collections;

public class BossEnemy : Enemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float introDownDistance = 10f;
    [SerializeField] private float introDuration = 2f;

    private float _attackTimer;
    private Vector2 _originalPosition;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update() {}

    private void OnEnable()
    {
        _originalPosition = transform.position;
        StartCoroutine(BossIntroSequence());
    }

    
    #region 보스 인트로 
    private IEnumerator BossIntroSequence()
    {
        // 시작 위치 위로 순간이동
        Vector2 startPosition = _originalPosition + (Vector2.up * introDownDistance);
        transform.position = startPosition;

        // 인트로 시간 동안 부드럽게 하강
        float timer = 0f;
        while (timer < introDuration)
        {
            timer += Time.deltaTime;
            float t = timer / introDuration;
            
            t = t * t * (3f - 2f * t);

            transform.position = Vector2.Lerp(startPosition, _originalPosition, t);
            yield return null;
        }

        // 위치 보정
        transform.position = _originalPosition;

        StartCoroutine(ShowWarningEffect(1f));

        yield return new WaitForSeconds(1f);
        
        StartCoroutine(PatternLoop());
    }

    /// <summary>
    /// 지정된 시간 동안 보스가 진동합니다.
    /// </summary>
    private IEnumerator ShowWarningEffect(float duration)
    {
        float timer = 0f;
        float shakeMagnitude = 0.05f; 

        while (timer < duration)
        {
            float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
            float yOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
            
            transform.position = _originalPosition + new Vector2(xOffset, yOffset);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = _originalPosition;
    }
    #endregion

    // 보스 공격 패턴 루프
    private IEnumerator PatternLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            Dash();
            yield return new WaitForSeconds(1f);
            Dash();
            yield return new WaitForSeconds(1f);
            StartCoroutine(BackToOriginalPosition());
            yield return new WaitForSeconds(2f);
            Shoot();
        }
    }

    #region 패턴 1: 대쉬 공격
    private void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;
        StartCoroutine(DashAttackRoutine(dir));
    }

    IEnumerator DashAttackRoutine(Vector2 dir)
    {
        spriteRenderer.color = Color.red; 
        yield return new WaitForSeconds(0.25f);
        
        spriteRenderer.color = Color.green;
        float dashDuration = 0.25f; 
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            transform.position += (Vector3)dir * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator BackToOriginalPosition()
    {
        float returnDuration = 0.5f; 
        float elapsedTime = 0f;

        Vector2 startPosition = transform.position;

        while (elapsedTime < returnDuration)
        {
            transform.position = Vector2.Lerp(startPosition, _originalPosition, elapsedTime / returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = _originalPosition;
    }
    #endregion

    #region 패턴 2: 총알 발사
    void Shoot()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * 6f;
    }
    #endregion
}