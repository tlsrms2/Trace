using System.Collections;
using FullscreenEditor.Linux;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CrossEnemy : Enemy 
{
    [Header("Cloud Settings")]
    private LineRenderer lineRenderer;

    float outX = 5f;
    float outY = 5f;
    float safeX = 50f;
    float safeY = 50f;
    private bool isActivating = true;
    
    private Vector3 direction; 
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
        lineRenderer.enabled = true;

        ReadySpecialAttack();
    }

    private void ReadySpecialAttack()
    {
        isActivating = true;
        lineRenderer.enabled = true;
        base.Start(); // target 초기화

        // 첫 번째 공격 개시
        ResetAttack(); 
    }

    // ★ 핵심: 공격 상태를 초기화하고 새로운 위치를 잡는 함수
    private void ResetAttack()
    {        
        // 임의의 방향을 결정 로직
        int randomSide = Random.Range(0, 4); 

        Vector3 startPos = Vector3.zero;
        

        switch (randomSide)
        {
            case 0: // 좌측 벽에서 우측으로
                startPos = new Vector3(startPos.x - outX, Random.Range(startPos.y - safeY, startPos.y + safeY), 0);
                direction = Vector3.right;
                break;
            case 1: // 우측 벽에서 좌측으로
                startPos = new Vector3(startPos.x + outX, Random.Range(startPos.y - safeY, startPos.y + safeY), 0);
                direction = Vector3.left;
                break;
            // case 2: // 하단 벽에서 상단으로
            //     startPos = new Vector3(Random.Range(camPos.x - safeX, camPos.x + safeX), camPos.y - outY, 0);
            //     direction = Vector3.up;
            //     break;
            // case 3: // 상단 벽에서 하단으로
            //     startPos = new Vector3(Random.Range(camPos.x - safeX, camPos.x + safeX), camPos.y + outY, 0);
            //     direction = Vector3.down;
            //     break;
            

            // 계산된 새로운 위치로 순간이동
            transform.position = startPos;

            // 도착 지점 계산
            Vector3 endPos = startPos + direction * (Mathf.Abs(direction.x) > 0 ? outX * 2 : outY * 2);

            // 3. LineRenderer (경로 표시) 재설정
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }
    }

    protected override void Update()
    {
        // --- 돌진 페이즈 ---
        base.Update(); 
        CheckOutOfBounds(); 
    }

    protected override void Move()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void CheckOutOfBounds()
    {

        // 월드맵(50f) 밖으로 완전히 벗어나면 다시 생성
        if (Mathf.Abs(transform.position.x) > safeX || Mathf.Abs(transform.position.y) > safeY)
        {
            ResetAttack();
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        spriteRenderer.enabled = false;
        col.enabled = false;
        isActivating = false;

        yield return new WaitForSeconds(delay);

        ReadySpecialAttack();
    }
}