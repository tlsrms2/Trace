using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CrossEnemy : Enemy 
{
    [Header("Cross Enemy Settings")]
    [SerializeField] private float alertTime = 2.0f;
    [SerializeField] private Color pathColor = new Color(1, 0, 0, 0.5f);

    private LineRenderer lineRenderer;
    private float timer;
    private float initialWidth;
    private bool isAlerting = true;
    private bool isActivating = true;
    
    private Vector3 direction; // 돌진 방향
    private Camera mainCam;

    protected override void Awake()
    {
        base.Awake(); 
        lineRenderer = GetComponent<LineRenderer>();
        mainCam = Camera.main;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void OnEnable()
    {
        isAlerting = true;
        lineRenderer.enabled = true;

        ReadySpecialAttack();
    }

    private void ReadySpecialAttack()
    {
        // 1. 경고 페이즈 동안 모습과 충돌체 숨기기
        spriteRenderer.enabled = false;
        col.enabled = false;
        timer = alertTime;
        isActivating = true;
        lineRenderer.enabled = true;

        // --- 2. 동적 화면 크기 계산 및 내 위치 재조정 (95% 룰 적용) ---
        if (mainCam != null)
        {
            float camHalfHeight = mainCam.orthographicSize;
            float camHalfWidth = camHalfHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;

            // 화면의 95% 안전 구역 설정
            float safeY = camHalfHeight * 0.95f;
            float safeX = camHalfWidth * 0.95f;

            // 스포너가 대충 던져준 위치(transform.position)를 바탕으로 어느 쪽 벽에 가까운지 판단
            float distX = transform.position.x - camPos.x;
            float distY = transform.position.y - camPos.y;

            // 화면 밖 등장/퇴장 기준선 (카메라 크기 + 여유값 2f)
            float outX = camHalfWidth + 2f;
            float outY = camHalfHeight + 2f;

            Vector3 startPos = Vector3.zero;

            int randomWall = Random.Range(0, 4);

            switch (randomWall)
            {
                case 0: // 우측
                    startPos = new Vector3(camPos.x + outX, camPos.y + Random.Range(-safeY, safeY), 0);
                    direction = Vector3.left;
                    break;
                case 1: // 좌측
                    startPos = new Vector3(camPos.x - outX, camPos.y + Random.Range(-safeY, safeY), 0);
                    direction = Vector3.right;
                    break;
                case 2: // 상단
                    startPos = new Vector3(camPos.x + Random.Range(-safeX, safeX), camPos.y + outY, 0);
                    direction = Vector3.down;
                    break;
                case 3: // 하단
                    startPos = new Vector3(camPos.x + Random.Range(-safeX, safeX), camPos.y - outY, 0);
                    direction = Vector3.up;
                    break;
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
        }
    }

    protected override void Update()
    {
        if (isAlerting &&  isActivating && GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            // --- 경고 페이즈 ---
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

            if (timer <= 0f)
            {
                isAlerting = false;
                lineRenderer.enabled = false; 
                spriteRenderer.enabled = true; 
                col.enabled = true; 
            }
        }
        else if(isAlerting == false)
        {
            // --- 돌진 페이즈 ---
            base.Update(); // 오버라이드한 Move() 실행
            CheckOutOfBounds(); 
        }
    }

    protected override void Move()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
        // 플레이어를 쫓아가지 않고 계산된 일직선 방향으로만 돌진
        transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void CheckOutOfBounds()
    {
        if (mainCam == null) return;

        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        float distX = Mathf.Abs(transform.position.x - camPos.x);
        float distY = Mathf.Abs(transform.position.y - camPos.y);

        if (distX > camHalfWidth + 3.0f || distY > camHalfHeight + 3.0f)
        {
            StartCoroutine(RespawnAfterDelay(0.8f)); 
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        isAlerting = true;
        spriteRenderer.enabled = false;
        col.enabled = false;
        isActivating = false;

        yield return new WaitForSeconds(delay);

        ReadySpecialAttack();
    }
}