using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("불변 객체")]
    [SerializeField] private GameObject Illusion;
    [SerializeField] private LineRenderer dotLineRenderer;

    [Header("이동 및 드로우")]
    [Tooltip("이동 속도")][SerializeField] private float moveSpeed = 5f;
    [Tooltip("시간 정지 시 이동 속도")][SerializeField] private float drawMoveSpeed = 3f;
    [Tooltip("포인트 간 최소 거리")][SerializeField] private float minDistance = 0.1f;
    [Tooltip("선 굵기")][SerializeField] private float lineWidth = 0.2f;
    [Tooltip("도형 완성 거리 보정값")][SerializeField] private float closeThreshold = 1f;

    [Header("데미지")]
    [Tooltip("선 데미지")][SerializeField] private int lineDamage = 5;
    [Tooltip("도형 데미지")][SerializeField] private int shapeDamage = 10;

    private LineRenderer lineRenderer;
    private bool isTracing = false;
    private List<Vector3> tracePoints = new List<Vector3>();
    private List<GameObject> shapes = new List<GameObject>();

    private bool isReplaying = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D playerRigidbody;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isReplaying)
        {
            StartTrace();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            isTracing = false;
            EvaluateShape();
        }

        if (isTracing)
        {
            RecordPosition();
        }

        if (isReplaying)
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

        if (!isReplaying)
            playerRigidbody.linearVelocity = inputDir * moveSpeed;
        else
            playerRigidbody.linearVelocity = Vector2.zero;
    }

    private void StartTrace()
    {
        isTracing = true;
        tracePoints.Clear();
        lineRenderer.positionCount = 0;
        tracePoints.Add(transform.position);
    }

    /// <summary>
    /// dotLineRenderer
    /// </summary>
    private void RecordPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 lastPos = tracePoints[tracePoints.Count - 1];
        if (Vector3.Distance(currentPos, lastPos) < minDistance) return;

        tracePoints.Add(currentPos);
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());
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
        isReplaying = true;
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

        while (tracePoints.Count > 1)
        {
            Illusion.transform.position = tracePoints[0];
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

        tracePoints.Clear();
        lineRenderer.positionCount = 0;

        isReplaying = false;
        Destroy(colliderObj);

        ActivateShapes();
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
    }
}
