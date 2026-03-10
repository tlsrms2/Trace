using UnityEngine;
using System.Collections;

public struct specialAttackInfo
{
    public Vector3 startPos;
    public Vector3 endPos;
    public Vector3 direction;
}

public class BossEnemy : Enemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float dashSpeed;

    [Header("인트로 설정")]
    [SerializeField] private float introDownDistance = 10f;
    [SerializeField] private float introDuration = 2f;

    [Header("패턴3")]
    [SerializeField] private float alertTime = 2f;
    [SerializeField] private Color pathColor = new Color(1, 0, 0, 0.5f);
    private specialAttackInfo[] specialInfo = new specialAttackInfo[4];
    private Camera mainCam;
    private Vector3 direction;
    private float initialWidth;


    private float timer;

    private LineRenderer lineRenderer;
    private Vector2 _originalPosition;

    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        mainCam = Camera.main;
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
            // yield return new WaitForSeconds(2f);
            // Dash();
            // yield return new WaitForSeconds(1f);
            // Dash();
            // yield return new WaitForSeconds(1f);
            // StartCoroutine(BackToOriginalPosition());
            // yield return new WaitForSeconds(2f);
            // Shoot();
            // yield return new WaitForSeconds(2f);
            StartCoroutine(SpecialAttack());
            yield return new WaitForSeconds(4f);
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

    #region 패턴 3: 특수 공격
    private specialAttackInfo ReadySpecialAttack()
    {
        timer = alertTime;
        lineRenderer.enabled = true;

        specialAttackInfo info = new specialAttackInfo();

        if (mainCam != null)
        {
            float camHalfHeight = mainCam.orthographicSize;
            float camHalfWidth = camHalfHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;

            // 화면의 95% 안전 구역 설정
            float safeY = camHalfHeight * 0.95f;
            float safeX = camHalfWidth * 0.95f;

            // 화면 밖 등장/퇴장 기준선 (카메라 크기 + 여유값 2f)
            float outX = camHalfWidth + 2f;
            float outY = camHalfHeight + 2f;

            Vector3 startPos = Vector3.zero;
            
            if (Random.Range(0, 2) == 0)
            {
                // Y축을 95% 안전 구역 내에서 랜덤 재설정
                float randomY = Random.Range(camPos.y - safeY, camPos.y + safeY);
                if (Random.Range(0, 2) == 0) // 우측
                {
                    startPos = new Vector3(camPos.x + outX, randomY, 0);
                    direction = Vector3.left;
                }
                else // 좌측
                {
                    startPos = new Vector3(camPos.x - outX, randomY, 0);
                    direction = Vector3.right;
                }
            }
            else
            {
                // X축을 95% 안전 구역 내에서 랜덤 재설정
                float randomX = Random.Range(camPos.x - safeX, camPos.x + safeX);
                if (Random.Range(0, 2) == 0) // 상단
                {
                    startPos = new Vector3(randomX, camPos.y + outY, 0);
                    direction = Vector3.down;
                }
                else // 하단
                {
                    startPos = new Vector3(randomX, camPos.y - outY, 0);
                    direction = Vector3.up;
                }
            }

            // 스포너가 정해준 위치를 무시하고, 계산된 완벽한 95% 위치로 내 위치 덮어쓰기!
            transform.position = startPos;

            // 도착 지점 계산 (내 위치에서 반대편 화면 밖까지)
            Vector3 endPos = startPos + direction * (Mathf.Abs(direction.x) > 0 ? outX * 2 : outY * 2);

            // 3. LineRenderer (경로 표시) 설정
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            initialWidth = transform.localScale.x;
            lineRenderer.startWidth = initialWidth;
            lineRenderer.endWidth = initialWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            info.startPos = startPos;
            info.endPos = endPos;
            info.direction = direction;
        }

        return info;
    }

    private IEnumerator SpecialAttack()
    {
        for (int i = 0; i < 4; i++)
        {
            specialInfo[i] = ReadySpecialAttack();
            Debug.Log($"Special Attack {i + 1}: StartPos={specialInfo[i].startPos}, EndPos={specialInfo[i].endPos}, Direction={specialInfo[i].direction}");
        }

        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(SpecialAttackRoutine(specialInfo[i]));
            yield return new WaitForSeconds(2.5f);
        }
    }

    private IEnumerator SpecialAttackRoutine(specialAttackInfo info)
    {
        float timer = alertTime;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / alertTime);

            lineRenderer.startWidth = initialWidth * ratio;
            lineRenderer.endWidth = initialWidth * ratio;

            float blinkSpeed = Mathf.Lerp(30f, 5f, ratio);
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color c = pathColor;
            c.a = alpha;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            yield return null;
        }
        
        lineRenderer.enabled = false;

        float dashTimer = 0f;
        float dashDuration = 0.5f;

        while (dashTimer < dashDuration)
        {
            dashTimer += Time.deltaTime;
            transform.position += info.direction * speed * Time.deltaTime;

            yield return null;
        }
    }
    #endregion
}