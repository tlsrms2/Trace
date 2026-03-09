using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private List<GameObject> shapes = new List<GameObject>(); // 도형 리스트 (공격 후 초기화)

    private Coroutine eraseCoroutine;

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
            if (shapes.Count > 0)
                ActivateShapes();
            else
                eraseCoroutine = StartCoroutine(EraseFromStart());
            isTracing = false;
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
        if (eraseCoroutine != null)
        {
            StopCoroutine(eraseCoroutine);
            eraseCoroutine = null;
        }

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

    // 선분 제거
    private IEnumerator EraseFromStart(float interval = 0.01f)
    {
        // 선분 삭제 시 공격 판정 생성 후 뒤에서부터 삭제
        GameObject colliderObj = new GameObject("AttackCollider");
        EdgeCollider2D edgeCol = colliderObj.AddComponent<EdgeCollider2D>();
        Vector2[] points2D = tracePoints.Select(p => new Vector2(p.x, p.y)).Reverse().ToArray();
        edgeCol.points = points2D;
        edgeCol.isTrigger = true;
        edgeCol.tag = "Attack";

        while (tracePoints.Count > 1)
        {
            // 가장 오래된 포인트 제거
            tracePoints.RemoveAt(0);
            lineRenderer.positionCount = tracePoints.Count;
            lineRenderer.SetPositions(tracePoints.ToArray());

            if (edgeCol != null && edgeCol.pointCount > 2)
            {
                Vector2[] pts = edgeCol.points;
                System.Array.Resize(ref pts, pts.Length - 1);
                edgeCol.points = pts;
            }

            yield return new WaitForSeconds(interval);
        }
        lineRenderer.positionCount = 0;
        eraseCoroutine = null;
        Destroy(colliderObj);
    }

    private void OnSelfIntersect(int loopStartIndex, Vector2 intersection)
    {
        // 교차점부터 현재까지가 도형의 꼭짓점들
        List<Vector3> shapePoints = new List<Vector3>();
        shapePoints.Add(new Vector3(intersection.x, intersection.y, 0f)); // 교차점

        for (int i = loopStartIndex; i < tracePoints.Count; i++)
            shapePoints.Add(tracePoints[i]);

        shapePoints.Add(new Vector3(intersection.x, intersection.y, 0f)); // 닫기

        CreateShape(shapePoints);

        Vector3 intersectionV3 = new Vector3(intersection.x, intersection.y, 0f);
        tracePoints.RemoveRange(loopStartIndex, tracePoints.Count - loopStartIndex);
        tracePoints.Add(intersectionV3);
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());
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

        shapes.Add(shapeObj);
        shapeObj.SetActive(false);
    }

    private void ActivateShapes()
    {
        foreach (GameObject shape in shapes)
        {
            shape.SetActive(true);
            // TODO: 도형 내부 적 감지, 데미지 등 공격 판정
        }
        shapes.Clear();
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
