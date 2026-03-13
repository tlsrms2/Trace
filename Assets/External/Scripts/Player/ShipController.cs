using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(LineRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("항해 및 조작 속도 (프로토타입 수치 기반)")]
    [SerializeField] private float playerAccel = 15f;      // 가속도
    [SerializeField] private float autoAccelMult = 0.8f;   // 자동 항해 시 가속도 배율
    [SerializeField] private float turnSpeed = 5f;         // 선회 속도
    [SerializeField] private float waterFriction = 1.5f;   // 물의 저항 (마찰력)

    [Header("경로 탐색 설정")]
    [SerializeField] private float minDrawDistance = 1f;   // 점을 찍는 최소 간격
    [SerializeField] private float waypointPassDist = 1.5f;// 웨이포인트 통과 기본 거리
    [SerializeField] private float waypointMissDist = 4.5f;// 지나쳤다고 판단할 최대 반경

    [Header("사격 컴포넌트")]
    [Tooltip("함포 사격을 담당하는 컴포넌트")]
    [SerializeField] private CharacterShoot cannonShooter;

    [Header("함포 설정")]
    [SerializeField] private float maxCannonDeviation = 40f; // 포신 최대 회전 각도 (도)
    [SerializeField] private GameObject cannonSprite;        // 회전할 포신 오브젝트

    [Header("시각 효과")]
    [SerializeField] private Material routeMaterial;
    [SerializeField] private float lineWidth = 0.2f;

    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    
    private List<Vector3> tracePoints = new List<Vector3>();
    private int routeIndex = 0;
    
    private bool isDrawing = false;
    private bool isReturning = false;
    private bool wasManual = false;

    private float currentAngle = -90f; 
    public float CannonAngle { get; private set; }

    // 연산 최적화를 위한 거리 제곱값 캐싱
    private float sqrWaypointPassDist;
    private float sqrWaypointMissDist;
    private float sqrMinDrawDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = routeMaterial ? routeMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = true;

        // 최적화: 기준 거리들의 제곱값을 미리 계산
        sqrWaypointPassDist = waypointPassDist * waypointPassDist;
        sqrWaypointMissDist = waypointMissDist * waypointMissDist;
        sqrMinDrawDistance = minDrawDistance * minDrawDistance;

        // 인스펙터에서 누락된 경우 자동 탐색
        if (cannonShooter == null)
        {
            cannonShooter = GetComponentInChildren<CharacterShoot>();
        }
    }

    private void Start()
    {
        GameManager.Instance.OnSteeringExhausted += ReturnToOptimalPath;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSteeringExhausted -= ReturnToOptimalPath;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            HandlePlanningPhase();
        }
        else if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            // 1. 포신 각도 계산
            UpdateCannonAngle();

            // 2. 마우스 좌클릭 시 사격 시도 (쿨타임 체크는 CharacterShoot에서 처리)
            if (Input.GetMouseButtonDown(0) && cannonShooter != null)
            {
                cannonShooter.TryShoot(CannonAngle);
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dt = Time.fixedDeltaTime;

        // 물의 저항 적용
        Vector2 velocity = rb.linearVelocity;
        velocity -= velocity * waterFriction * dt;
        rb.linearVelocity = velocity;

        bool isManual = GameManager.Instance.IsSteeringMode;

        if (!isManual && isReturning == false && wasManual == true)
        {
            ReturnToOptimalPath();
        }

        if (isManual)
        {
            ExecuteManualMovement(dt);
        }
        else
        {
            ExecuteAutoNavigation(dt);
        }

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        wasManual = isManual;
    }

    #region Phase 1: Planning (계획)
    private void HandlePlanningPhase()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            // 제곱 거리 비교 적용
            float sqrDistToPlayer = (mousePos - transform.position).sqrMagnitude;
            if (sqrDistToPlayer < 9f || tracePoints.Count == 0) // 3f * 3f
            {
                isDrawing = true;
                tracePoints.Clear();
                tracePoints.Add(transform.position);
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            if (tracePoints.Count > 0)
            {
                // 제곱 거리 비교 적용
                float sqrDist = (tracePoints[tracePoints.Count - 1] - mousePos).sqrMagnitude;
                if (sqrDist > sqrMinDrawDistance)
                {
                    tracePoints.Add(mousePos);
                    UpdateLineRenderer();
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }
    }
    #endregion

    #region Phase 2: Execution (항해 로직)
    private void ExecuteManualMovement(float dt)
    {
        float ax = Input.GetAxisRaw("Horizontal");
        float ay = Input.GetAxisRaw("Vertical");

        if (ax != 0 || ay != 0)
        {
            float targetAngle = Mathf.Atan2(ay, ax) * Mathf.Rad2Deg;
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * 100f * dt);

            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector2 forwardVec = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            
            rb.linearVelocity += forwardVec * playerAccel * dt;
        }
    }

    private void ExecuteAutoNavigation(float dt)
    {
        bool advanced = false;

        while (routeIndex < tracePoints.Count)
        {
            Vector3 target = tracePoints[routeIndex];
            float dx = target.x - transform.position.x;
            float dy = target.y - transform.position.y;
            
            // 제곱 거리 산출
            float sqrDist = (dx * dx) + (dy * dy);

            float angleRad = currentAngle * Mathf.Deg2Rad;
            float dot = dx * Mathf.Cos(angleRad) + dy * Mathf.Sin(angleRad);

            // 거리 제곱값 및 내적 비교 (루트 연산 제거)
            if (sqrDist < sqrWaypointPassDist || (sqrDist < sqrWaypointMissDist && dot < 0))
            {
                routeIndex++;
                advanced = true;
            }
            else
            {
                break;
            }
        }

        if (advanced && isReturning)
        {
            isReturning = false;
        }

        if (routeIndex < tracePoints.Count)
        {
            Vector3 target = tracePoints[routeIndex];
            float dx = target.x - transform.position.x;
            float dy = target.y - transform.position.y;

            float targetAngle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * 100f * dt);

            float currentRad = currentAngle * Mathf.Deg2Rad;
            Vector2 forwardVec = new Vector2(Mathf.Cos(currentRad), Mathf.Sin(currentRad));
            
            rb.linearVelocity += forwardVec * (playerAccel * autoAccelMult) * dt;
        }
        else
        {
            // TODO: 웨이브 클리어 조건
        }
    }

    private void ReturnToOptimalPath()
    {
        isReturning = true;
        float minSqrDist = float.MaxValue;
        int bestIdx = routeIndex;

        for (int i = routeIndex; i < tracePoints.Count; i++)
        {
            // 최단 거리 탐색에도 제곱 거리 활용
            float sqrDist = (tracePoints[i] - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                bestIdx = i;
            }
        }
        routeIndex = bestIdx;
    }
    #endregion

    #region 함포 조준 로직
    private void UpdateCannonAngle()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        float mouseAngle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg;
        
        // 진행 방향(currentAngle) 기준, 함선 좌/우측 기준 각도
        float leftSide = currentAngle + 90f;
        float rightSide = currentAngle - 90f;

        // 마우스 각도가 좌/우측 중 어디에 가까운지 판단하여 최대 회전 범위 내에서 포신 각도 결정
        float diffLeft = Mathf.Abs(Mathf.DeltaAngle(leftSide, mouseAngle));
        float diffRight = Mathf.Abs(Mathf.DeltaAngle(rightSide, mouseAngle));

        float targetCannonAngle = mouseAngle;

        // 더 가까운 측면을 선택하고, 최대 허용 각도를 초과하는 경우 Clamp
        if (diffLeft < diffRight)
        {
            if (diffLeft > maxCannonDeviation)
            {
                float sign = Mathf.Sign(Mathf.DeltaAngle(leftSide, mouseAngle));
                targetCannonAngle = leftSide + (sign * maxCannonDeviation);
            }
        }
        else
        {
            if (diffRight > maxCannonDeviation)
            {
                float sign = Mathf.Sign(Mathf.DeltaAngle(rightSide, mouseAngle));
                targetCannonAngle = rightSide + (sign * maxCannonDeviation);
            }
        }

        CannonAngle = targetCannonAngle;

        if (cannonSprite != null)
        {
            cannonSprite.transform.rotation = Quaternion.Euler(0f, 0f, CannonAngle);
        }
    }
    #endregion

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());
    }
}