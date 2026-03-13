using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(LineRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("항해 및 조작 속도")]
    [SerializeField] private float playerAccel = 15f;      
    [SerializeField] private float autoAccelMult = 0.8f;   
    [SerializeField] private float turnSpeed = 5f;         
    [SerializeField] private float waterFriction = 1.5f;   

    [Header("운명 동기화(Sync) 설정")]
    [SerializeField] private float detachThreshold = 3.0f; 
    [SerializeField] private float attachThreshold = 1.0f;
    public float maxFateDistance = 15f; 
    [SerializeField] private float syncPullForce = 20f;

    [Header("슬립스트림(궤적 추적) 설정")]
    [Tooltip("궤적 위에 있을 때의 가속 배율")]
    [SerializeField] private float slipstreamMultiplier = 2.0f;
    [Tooltip("슬립스트림 반경")]
    [SerializeField] private float slipstreamRadius = 2.5f;
    [Tooltip("코너 이탈 방지용: 궤적 중심으로 당겨주는 자력(그립력)")]
    [SerializeField] private float slipstreamGripForce = 10f;

    [Header("사격 컴포넌트")]
    [SerializeField] private CharacterShoot cannonShooter;
    [SerializeField] private float maxCannonDeviation = 40f; 
    [SerializeField] private GameObject cannonSprite;        

    [Header("시각 효과")]
    [SerializeField] private Material routeMaterial;
    [SerializeField] private float lineWidth = 0.2f;

    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    
    private List<Vector3> tracePoints = new List<Vector3>();
    private int routeIndex = 0;
    private bool isDrawing = false;
    
    private Vector2 ghostShipPos;
    
    // 상태 관리
    public bool IsSynchronized { get; private set; } = true;
    public bool IsLost { get; private set; } = false; 
    public float CurrentFateDeviation { get; private set; }

    public event Action<bool> OnSyncStateChanged; 
    public event Action<bool> OnLostStateChanged; 

    private float currentAngle = -90f; 
    public float CannonAngle { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = waterFriction; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = routeMaterial ? routeMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = true;

        if (cannonShooter == null) cannonShooter = GetComponentInChildren<CharacterShoot>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) HandlePlanningPhase();
        else if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            UpdateCannonAngle();
            if (Input.GetMouseButtonDown(0) && cannonShooter != null) cannonShooter.TryShoot(CannonAngle);
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

        UpdateGhostShipPosition(dt);
        UpdateSyncState();

        if (IsSynchronized) ExecuteSynchronizedMovement(dt);
        else if (GameManager.Instance.IsSteeringMode) ExecuteManualMovement(dt);

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    #region Planning Phase
    private void HandlePlanningPhase()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            if (Vector3.Distance(mousePos, transform.position) < 3f || tracePoints.Count == 0) 
            {
                isDrawing = true;
                tracePoints.Clear();
                tracePoints.Add(transform.position);
                ghostShipPos = transform.position; 
                routeIndex = 0;
                SetSyncState(true);
                SetLostState(false);
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            if (tracePoints.Count > 0 && Vector3.Distance(tracePoints[tracePoints.Count - 1], mousePos) > 1f)
            {
                tracePoints.Add(mousePos);
                UpdateLineRenderer();
            }
        }
        else if (Input.GetMouseButtonUp(0)) isDrawing = false;
    }
    #endregion

    #region Execution Phase
    private void UpdateGhostShipPosition(float dt)
    {
        if (routeIndex >= tracePoints.Count) return;

        Vector2 target = tracePoints[routeIndex];
        float dist = Vector2.Distance(ghostShipPos, target);

        if (dist < 1.0f)
        {
            routeIndex++;
            if (routeIndex >= tracePoints.Count) return; 
            target = tracePoints[routeIndex];
        }

        Vector2 dir = (target - ghostShipPos).normalized;
        // 단절 상태 시 유령선 속도 50% 감속 (추격의 여지 제공)
        float currentSpeed = (playerAccel * autoAccelMult) * (IsLost ? 0.5f : 1.0f);
        ghostShipPos += dir * currentSpeed * dt;
    }

    private void UpdateSyncState()
    {
        CurrentFateDeviation = Vector2.Distance(transform.position, ghostShipPos);

        if (IsSynchronized && CurrentFateDeviation > detachThreshold) SetSyncState(false);
        else if (!IsSynchronized && CurrentFateDeviation <= attachThreshold) SetSyncState(true);

        if (!IsLost && CurrentFateDeviation >= maxFateDistance) SetLostState(true);
        else if (IsLost && CurrentFateDeviation < maxFateDistance * 0.8f) SetLostState(false);
    }

    private void SetSyncState(bool state)
    {
        if (IsSynchronized == state) return;
        IsSynchronized = state;
        OnSyncStateChanged?.Invoke(IsSynchronized);
    }

    private void SetLostState(bool state)
    {
        if (IsLost == state) return;
        IsLost = state;
        OnLostStateChanged?.Invoke(IsLost); 
    }

    private void ExecuteSynchronizedMovement(float dt)
    {
        Vector2 dirToGhost = (ghostShipPos - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(dirToGhost.y, dirToGhost.x) * Mathf.Rad2Deg;
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * 100f * dt);

        Vector2 followVelocity = dirToGhost * (playerAccel * autoAccelMult);
        Vector2 pullForce = dirToGhost * (CurrentFateDeviation * syncPullForce);

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, followVelocity, dt * 10f);
        rb.AddForce(pullForce, ForceMode2D.Force); 
    }

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
            
            float currentAccel = playerAccel * 2.5f;
            Vector2 gripForce = Vector2.zero;

            // 슬립스트림 자력 레일(Magnetic Rail) 연산
            if (TryGetSlipstreamData(out Vector2 nearestPoint))
            {
                currentAccel *= slipstreamMultiplier; 
                
                // 함선을 궤적의 중심(nearestPoint)으로 끌어당기는 구심력 계산
                Vector2 dirToRail = (nearestPoint - (Vector2)transform.position).normalized;
                float distToRail = Vector2.Distance(transform.position, nearestPoint);
                gripForce = dirToRail * (distToRail * slipstreamGripForce);
            }
            
            rb.AddForce((forwardVec * currentAccel) + gripForce, ForceMode2D.Force); 
        }
    }

    // 슬립스트림 판정 및 궤적 위 가장 가까운 좌표 반환
    private bool TryGetSlipstreamData(out Vector2 nearestPoint)
    {
        nearestPoint = Vector2.zero;
        if (tracePoints.Count < 2) return false;
        
        Vector2 currentPos = transform.position;
        float minDistSqr = float.MaxValue;
        bool found = false;

        for (int i = 0; i < tracePoints.Count - 1; i++)
        {
            Vector2 a = tracePoints[i];
            Vector2 b = tracePoints[i + 1];
            
            Vector2 ab = b - a;
            float t = Vector2.Dot(currentPos - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + t * ab;
            
            float distSqr = (currentPos - projection).sqrMagnitude;
            if (distSqr < slipstreamRadius * slipstreamRadius && distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearestPoint = projection;
                found = true;
            }
        }
        return found;
    }
    #endregion

    private void UpdateCannonAngle()
    {
        if (cannonSprite == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        float mouseAngle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg;
        CannonAngle = Mathf.Clamp(mouseAngle, currentAngle - maxCannonDeviation, currentAngle + maxCannonDeviation);
        cannonSprite.transform.rotation = Quaternion.Euler(0f, 0f, CannonAngle);
    }

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = tracePoints.Count;
        lineRenderer.SetPositions(tracePoints.ToArray());
    }
}