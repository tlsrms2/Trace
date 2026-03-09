using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Tooltip("이동 속도")][SerializeField] private float moveSpeed = 5f;
    [Tooltip("포인트 간 최소 거리")][SerializeField] private float minDistance = 0.1f;
    [Tooltip("선 굵기")][SerializeField] private float lineWidth = 0.2f;
    [Tooltip("도형 완성 거리 보정값")]
    [SerializeField] private float closeThreshold = 0.2f;

    private LineRenderer lineRenderer;
    private bool isTracing = false;
    private List<Vector3> tracePoints = new List<Vector3>(); // 포인트 위치 저장

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(x, y).normalized;

        transform.Translate(inputDir * moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartTrace();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            StartCoroutine(EraseFromStart());
        }

        // 다음 선 그리기
        if (isTracing)
        {
            RecordPosition();
        }
    }

    // 시간 정지 및 그리기 시작
    private void StartTrace()
    {
        // 시간 정지 미구현
        isTracing = true;
        tracePoints.Clear();
        lineRenderer.positionCount = 0;
        tracePoints.Add(transform.position);
    }

    // 포인트 기록
    private void RecordPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 lastPos = tracePoints[tracePoints.Count - 1];
        if (Vector3.Distance(currentPos, lastPos) < minDistance) return;

        // 인접한 선분은 제외(마지막 2개)하고 교차 확인
        for (int i = 0; i < tracePoints.Count - 2; i++)
        {
            Vector2 intersection;
            if (SegmentsIntersect(
                tracePoints[i], tracePoints[i + 1],
                lastPos, currentPos,
                out intersection))
            {
                // 교차점 발견했으면 발견 포인트 번호 이후 포인트들이 도형
                OnSelfIntersect(i + 1, intersection);
                return;
            }
        }
        // 교차 없으면 일반 포인트 추가
        tracePoints.Add(currentPos);
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());
    }

    // 포인트 제거
    private IEnumerator EraseFromStart(float interval = 0.01f)
    {
        isTracing = false;
        while (tracePoints.Count > 1)
        {
            SpawnAttackCollider(tracePoints[0], tracePoints[1], interval);

            // 가장 오래된 포인트 제거
            tracePoints.RemoveAt(0);
            lineRenderer.positionCount = tracePoints.Count;
            lineRenderer.SetPositions(tracePoints.ToArray());
            yield return new WaitForSeconds(interval);
        }
        lineRenderer.positionCount = 0;
    }

    // 선분 삭제 시 공격 판정 생성
    private void SpawnAttackCollider(Vector3 from, Vector3 to, float lifetime)
    {
        // 선분 중점에 오브젝트 생성
        Vector3 mid = (from + to) * 0.5f;
        GameObject attackObj = new GameObject("EraseAttack");
        attackObj.tag = "Attack";
        attackObj.transform.position = mid;

        // 선분 길이에 맞춰 BoxCollider2D 크기 설정
        BoxCollider2D col = attackObj.AddComponent<BoxCollider2D>();
        float segLen = Vector3.Distance(from, to);
        col.size = new Vector2(segLen + lineWidth, lineWidth);
        col.isTrigger = true;

        // 선분 방향으로 회전
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        attackObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(attackObj, lifetime);
    }

    private void OnSelfIntersect(int loopStartIndex, Vector2 intersection)
    {
        isTracing = false;

        // 교차점부터 현재까지가 도형의 꼭짓점들
        List<Vector3> shapePoints = new List<Vector3>();
        shapePoints.Add(new Vector3(intersection.x, intersection.y, 0f)); // 교차점

        for (int i = loopStartIndex; i < tracePoints.Count; i++)
            shapePoints.Add(tracePoints[i]);

        shapePoints.Add(new Vector3(intersection.x, intersection.y, 0f)); // 닫기

        Debug.Log($"자기교차 도형 완성! 꼭짓점 수: {shapePoints.Count}");
        CreateShape(shapePoints);
    }

    // 도형 만들기
    private void CreateShape(List<Vector3> points)
    {
        GameObject shapeObj = new GameObject("FilledShape");

        // PolygonCollider2D로 외곽선 설정
        PolygonCollider2D col = shapeObj.AddComponent<PolygonCollider2D>();
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in points)
            points2D.Add(new Vector2(p.x, p.y));
        col.SetPath(0, points2D);

        // 삼각분할
        Mesh mesh = col.CreateMesh(false, false);
        MeshFilter mf = shapeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = shapeObj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = new Color(1f, 0.5f, 0.5f, 0.4f);
    }

    // 선분 AB와 선분 CD가 교차하는지 판별 (중간에 도형이 생기는지 판별하기 위함)
    private bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = b - a;
        Vector2 s = d - c;
        float denom = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(denom) < 0.0001f) return false;
        Vector2 diff = c - a;
        float t = (diff.x * s.y - diff.y * s.x) / denom;
        float u = (diff.x * r.y - diff.y * r.x) / denom;
        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersection = a + t * r;
            return true;
        }
        return false;
    }
}
