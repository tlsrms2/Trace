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
    private Vector3 lastRecordedPos;

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
            if (!isTracing)
                StartTrace();
            else
                StartCoroutine(EraseFromStart());
        }

        if (isTracing)
        {
            // 다음 선 그리기
            if (Vector3.Distance(transform.position, lastRecordedPos) >= minDistance)
            {
                tracePoints.Add(transform.position);
                lineRenderer.positionCount = tracePoints.Count;
                lineRenderer.SetPositions(tracePoints.ToArray());
                lastRecordedPos = transform.position;
            }

            // 포인트가 10개 이상이고 시작점 근처로 오면 도형 완성
            if (tracePoints.Count > 10 && Vector3.Distance(transform.position, tracePoints[0]) < closeThreshold)
            {
                OnPathClosed();
            }
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
        lastRecordedPos = transform.position;
    }

    // 포인트 제거
    private IEnumerator EraseFromStart(float interval = 0.01f)
    {
        isTracing = false;
        while (tracePoints.Count > 1)
        {
            // 가장 오래된 포인트 제거
            tracePoints.RemoveAt(0);
            lineRenderer.positionCount = tracePoints.Count;
            lineRenderer.SetPositions(tracePoints.ToArray());
            yield return new WaitForSeconds(interval);
        }
        lineRenderer.positionCount = 0;
    }

    private void OnPathClosed()
    {
        isTracing = false;
        // 시작점으로 선 연결
        tracePoints.Add(tracePoints[0]);
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());

        FillShape();
    }

    // 도형 만들기
    private void FillShape()
    {
        GameObject shapeObj = new GameObject("FilledShape");

        // PolygonCollider2D로 외곽선 설정
        PolygonCollider2D col = shapeObj.AddComponent<PolygonCollider2D>();
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in tracePoints)
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
}
