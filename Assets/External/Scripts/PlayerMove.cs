using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("불변 객체")]
    [SerializeField] private GameObject Illusion;
    [SerializeField] private LineRenderer dotLineRenderer;
    [SerializeField] private Material dotLineMaterial;

    [Header("이동 및 드로우")]
    [Tooltip("평상 시 이동 속도")][SerializeField] private float normalMoveSpeed = 5f;
    [Tooltip("시간 정지 시 이동 속도")][SerializeField] private float traceMoveSpeed = 3f;
    [Tooltip("포인트 간 최소 거리")][SerializeField] private float minDistance = 0.1f;
    [Tooltip("선 굵기")][SerializeField] private float lineWidth = 0.2f;
    [Tooltip("도형 완성 거리 보정값")][SerializeField] private float closeThreshold = 1f;

    [Header("데미지")]
    [Tooltip("선 데미지")][SerializeField] private int lineDamage = 5;
    [Tooltip("도형 데미지")][SerializeField] private int shapeDamage = 10;

    private float moveSpeed;

    private LineRenderer lineRenderer;
    private List<Vector3> tracePoints = new List<Vector3>();
    private List<GameObject> shapes = new List<GameObject>();

    private bool IsTracing => GameManager.Instance.CurrentPhase == GamePhase.Paused;
    private bool IsReplaying => GameManager.Instance.CurrentPhase == GamePhase.Replay;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D playerRigidbody;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // 점선 설정
        dotLineRenderer.textureMode = LineTextureMode.Tile;
        dotLineRenderer.material = dotLineMaterial;
        dotLineRenderer.startWidth = lineWidth;
        dotLineRenderer.endWidth = lineWidth;
        dotLineRenderer.useWorldSpace = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        GameManager.Instance.OnTraceStarted += StartTrace;
        GameManager.Instance.OnTraceEnded += EndTrace;
    }
    private void OnDestroy()
    {
        GameManager.Instance.OnTraceStarted -= StartTrace;
        GameManager.Instance.OnTraceEnded -= EndTrace;
    }

    private void Update()
    {
        if (IsTracing)
        {
            RecordPosition();
            moveSpeed = traceMoveSpeed;
        }
        else
        {
            moveSpeed = normalMoveSpeed;
        }

        if (IsReplaying)
        {
            spriteRenderer.color = new Color(0, 0, 0, 0);
            Illusion.SetActive(true);
        }
        else
        {
            spriteRenderer.color = originalColor;
            Illusion.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(x, y).normalized;

        if (!IsReplaying)
        {
            playerRigidbody.linearVelocity = inputDir * moveSpeed;
        }
        else
            playerRigidbody.linearVelocity = Vector2.zero;
    }

    private void StartTrace()
    {
        tracePoints.Clear();
        lineRenderer.positionCount = 0;
        tracePoints.Add(transform.position);
    }

    private void EndTrace()
    {
        EvaluateShape();
    }

    /// <summary>
    /// dotLineRenderer를 이용한 점선 그리기
    /// </summary>
    private void RecordPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 lastPos = tracePoints[tracePoints.Count - 1];
        if (Vector3.Distance(currentPos, lastPos) < minDistance) return;

        tracePoints.Add(currentPos);
        dotLineRenderer.positionCount = tracePoints.Count;
        dotLineRenderer.SetPositions(tracePoints.ToArray());
    }

    // Space를 뗐을 때 도형 여부 판단
    private void EvaluateShape()
    {
        bool isClosed = tracePoints.Count >= 5 &&
                        Vector3.Distance(tracePoints[tracePoints.Count - 1], tracePoints[0]) < closeThreshold;

        if (isClosed)
        {
            tracePoints.Add(tracePoints[0]);
            CreateShape(tracePoints);
        }
        StartCoroutine(EraseFromStart());
    }

    /// <summary>
    /// tracePoints를 따라 lineRenderer를 재생성
    /// </summary>
    /// <param name="interval">프레임당 삭제 간격</param>
    /// <returns></returns>
    private IEnumerator EraseFromStart(float interval = 0.01f)
    {
        GameObject colliderObj = new GameObject("AttackCollider");
        EdgeCollider2D edgeCol = colliderObj.AddComponent<EdgeCollider2D>();
        Vector2[] points2D = tracePoints.Select(p => new Vector2(p.x, p.y)).Reverse().ToArray();
        edgeCol.points = points2D;
        edgeCol.isTrigger = true;
        edgeCol.tag = "Attack";
        AttackData data = colliderObj.AddComponent<AttackData>();
        data.Damage = lineDamage;
        var rig = colliderObj.AddComponent<Rigidbody2D>();
        rig.gravityScale = 0;

        // 공격 선 드로우 시 사용하는 포인트 위치
        List<Vector3> attackTracePoints = new List<Vector3>();

        while (tracePoints.Count > 1)
        {
            Illusion.transform.position = tracePoints[0];
            attackTracePoints.Add(tracePoints[0]);
            tracePoints.RemoveAt(0);

            lineRenderer.positionCount = attackTracePoints.Count;
            lineRenderer.SetPositions(attackTracePoints.ToArray());
            dotLineRenderer.positionCount = tracePoints.Count;
            dotLineRenderer.SetPositions(tracePoints.ToArray());

            if (edgeCol != null && edgeCol.pointCount > 2)
            {
                Vector2[] pts = edgeCol.points;
                System.Array.Resize(ref pts, pts.Length - 1);
                edgeCol.points = pts;
            }

            yield return new WaitForSeconds(interval);
        }

        tracePoints.Clear();
        dotLineRenderer.positionCount = 0;

        Destroy(colliderObj);
        ActivateShapes();
        GameManager.Instance.ChangePhase(GamePhase.RealTime);
    }

    private void CreateShape(List<Vector3> points)
    {
        GameObject shapeObj = new GameObject("FilledShape");

        PolygonCollider2D col = shapeObj.AddComponent<PolygonCollider2D>();
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in points)
            points2D.Add(new Vector2(p.x, p.y));
        col.SetPath(0, points2D);
        col.isTrigger = true;

        Mesh mesh = col.CreateMesh(false, false);
        MeshFilter mf = shapeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = shapeObj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = new Color(1f, 0.5f, 0.5f, 0.4f);

        AttackData attack = shapeObj.AddComponent<AttackData>();
        attack.Damage = shapeDamage;
        shapeObj.tag = "Attack";
        var rig = shapeObj.AddComponent<Rigidbody2D>();
        rig.gravityScale = 0;

        shapes.Add(shapeObj);
        shapeObj.SetActive(false);
    }

    private void ActivateShapes()
    {
        foreach (GameObject shape in shapes)
        {
            shape.SetActive(true);
            Destroy(shape, 2f);
        }
        shapes.Clear();
        lineRenderer.positionCount = 0;
    }
}
