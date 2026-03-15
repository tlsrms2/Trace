using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;

[RequireComponent(typeof(Rigidbody2D), typeof(LineRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("항해 및 조작 속도 (선박형)")]
    [Tooltip("선박의 기본 이동 속도")]
    [SerializeField] private float baseSpeed = 0f;
    [Tooltip("선박의 기본 가속 레벨")]
    [SerializeField] private float accelLevel = 1.0f;
    [Tooltip("유령선의 기본 가속 레벨")]
    [SerializeField] private float ghostAccelLevel = 1.0f;
    [Tooltip("선박의 최대 전진 속도")]
    [SerializeField] private float maxForwardSpeed = 4.0f;
    [Tooltip("선박의 최대 후진 속도")]
    [SerializeField] private float maxBackwardSpeed = -2.0f;
    [Tooltip("전진(W) 가속도 증감치")]
    [SerializeField] private float forwardAccel = 2.0f;     
    [Tooltip("후진(S) 가속도 증감치 (절대값, 전진보다 느림)")]
    [SerializeField] private float backwardAccel = 1.0f;    
    [Tooltip("자동 항해 시 가속도 배율")]
    [SerializeField] private float autoAccelMult = 1.0f;   
    [Tooltip("좌/우(A/D) 선회 속도")]
    [SerializeField] private float turnSpeed = 0.5f;         
    [Tooltip("물의 저항 (높을수록 조작 멈춤 시 빠르게 정지함)")]
    [SerializeField] private float waterFriction = 1.5f;  

    [Header("항해 및 선박 조작 UI/UX")]
    [Tooltip("선박의 현재 가속 레벨을 표시하는 UI TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI speedDisplay;

    [Header("운명 궤도 시각 효과와 UI")]
    [Tooltip("결합 가능 범위를 시각적으로 표시하는 Image (예: 원형 이미지)")]
    [SerializeField] private GameObject attachRangeIndicator;
    [Tooltip("운명 이탈 상태를 시각적으로 표시하는 Image (예: 화면 가장자리부터 검게 변하는 효과)")]
    [SerializeField] private GameObject fateDeviationIndicator;
    [Tooltip("운명 궤도에서 이탈했을 때 나타나는 경고 UI (예: 'Fate Lost' 텍스트)")]
    [SerializeField] private GameObject fateLostUI;
    [Tooltip("운명 이탈 후 감소하는 운명 이탈율을 실시간으로 표시하는 UI slider")]
    [SerializeField] private Slider fateDeviationSlider;

    [Header("유령선과 슬립스트림 시각 효과")]
    [Tooltip("유령선의 시각적 표현으로 사용할 프리팹")]
    [SerializeField] private GameObject ghostShipPrefab;
    [Tooltip("슬립스트림(유령선이 지나간 선)을 시각적으로 표시하는 슬립스트림 Meterial")]
    [SerializeField] private Material slipstreamLineMaterial;
    [Tooltip("슬립스트림 궤적의 폭")]
    [SerializeField] private float slipstreamLineWidth = 0.3f;
    private GameObject ghostShipInstance;

    [Header("운명 동기화(Sync) 설정")]
    [Tooltip("플레이어와 유령선 간의 거리가 이 값보다 멀어지면 분리(Detach)")]
    [SerializeField] private float detachThreshold = 3.0f; 
    [Tooltip("플레이어와 유령선 간의 거리가 이 값보다 가까우면 결합(Attach)")]
    [SerializeField] private float attachThreshold = 2.0f;
    public float maxFateDistance = 15f; 
    [Tooltip("유령선과의 거리 오차를 즉각적으로 좁히는 스프링 계수")]
    [SerializeField] private float syncSpringForce = 5f;

    [Header("슬립스트림(궤적 추적) 설정")]
    [Tooltip("궤적 위에 있을 때의 최대 가속 배율")]
    [SerializeField] private float maxSlipstreamMultiplier = 1.2f;
    [Tooltip("슬립스트림 반경")]
    [SerializeField] private float slipstreamRadius = 2.0f;
    [Tooltip("코너 이탈 방지용: 궤적 중심으로 당겨주는 자력(그립력)")]
    [SerializeField] private float slipstreamGripForce = 3.0f;

    [Header("항해 경로(Planning) 설정")]
    [Tooltip("계획 페이즈에서 플레이어가 그릴 수 있는 최대 궤적 포인트 수 (길이 제한)")]
    [SerializeField] private int maxTracePoints = 200;

    // 조작감 향상을 위한 슬립스트림 보간 변수
    private float currentSlipstreamMultiplier = 1.0f;

    [Header("사격 컴포넌트")]
    [SerializeField] private CharacterShoot cannonShooter;
    [SerializeField] private float maxCannonDeviation = 40f; 
    [SerializeField] private GameObject cannonSprite;        

    [Header("시각 효과")]
    [SerializeField] private Material routeMaterial;
    [SerializeField] private float lineWidth = 0.3f;

    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private LineRenderer slipstreamLineRenderer;
    
    private List<Vector3> tracePoints = new List<Vector3>();

    // 렌더링 최적화를 위한 캐싱 버퍼
    private Vector3[] slipstreamBuffer = new Vector3[200];
    private Vector3[] futureBuffer = new Vector3[200];

    private int ghostTargetIndex = 0; // 유령선이 향하는 목표 인덱스
    private bool isDrawing = false;
    
    // 논리적 유령선 위치와 속도 (물리 프레임에서 계산)
    private Vector2 ghostShipPos;
    private Vector2 ghostShipVelocity;
    
    // 상태 관리
    public bool IsSynchronized { get; private set; } = true;
    public bool IsLost { get; private set; } = false; 
    // 운명 궤도에서의 이탈 정도 (플레이어와 유령선 간의 현재 거리)
    public float CurrentFateDeviation { get; private set; }
    
    // UI에 제공할 궤적 사용률 (0.0 ~ 1.0)
    public float GetTraceProgress() => maxTracePoints > 0 ? Mathf.Clamp01((float)tracePoints.Count / maxTracePoints) : 0f;

    // 자발적 분리 상태 (Shift 키로 강제 분리)
    private bool isVoluntarilyDetached = false;

    public event Action<bool> OnSyncStateChanged; 
    public event Action<bool> OnLostStateChanged; 

    // 미리 배치한 각도 그대로 시작
    private float currentAngle = 0f; 
    public float CannonAngle { get; private set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = waterFriction; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        // 물리 업데이트의 보간을 활성화하여 시각적 끊김 방지
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (cannonShooter == null) cannonShooter = GetComponentInChildren<CharacterShoot>();

        //순수 시각용 유령선 생성 및 부모 분리
        if (ghostShipPrefab != null)
        {
            ghostShipInstance = Instantiate(ghostShipPrefab, transform.position, Quaternion.identity);
            ghostShipInstance.SetActive(false);
        }
    }

    private void Start()
    {
        // 항해 경로용 라인 렌더러
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = routeMaterial;
        lineRenderer.useWorldSpace = true;

        // 슬립스트림 표시용 라인 렌더러
        slipstreamLineRenderer = ghostShipInstance.GetComponent<LineRenderer>();
        slipstreamLineRenderer.startWidth = lineWidth;
        slipstreamLineRenderer.endWidth = lineWidth;
        slipstreamLineRenderer.material = slipstreamLineMaterial;
        slipstreamLineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused) 
        {
            HandlePlanningPhase();
        }
        else if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            float dt = Time.deltaTime;

            // 1. 순수 수학/논리 연산 (랜더 프레임에 맞춰 부드럽게 실행)
            UpdateGhostShipPosition(dt);
            ConsumeTracePoints();

            // 2. 시각적 연산
            UpdateGhostShipVisuals();
            UpdateLineRenderer();

            // 3. 결합 여부 연산 (시각적 연산이 완료된 이후 진행하여 유령선의 위치 동기화 과정 중계를 방지)  
            UpdateSyncState();

            // 4. 입력 연산
            UpdateCannonAngle();
            if (Input.GetMouseButtonDown(0) && cannonShooter != null) cannonShooter.TryShoot(CannonAngle);
        }

        speedDisplay.text = accelLevel.ToString();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float fixedDt = Time.fixedDeltaTime;

        // 물리 연산 (힘, 속도 제어)은 FixedUpdate에서 수행
        if (IsSynchronized) 
        {
            ExecuteSynchronizedMovement(fixedDt);
        }
        else
        {
            ExecuteManualMovement(fixedDt);  // 결합이 풀려있을 때 수동 조작
        }

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
                ghostTargetIndex = 1;       // 1번 인덱스를 향해 출발 준비

                SetSyncState(true);
                SetLostState(false);
                isVoluntarilyDetached = false;

                if (ghostShipInstance != null)
                {
                    ghostShipInstance.transform.position = ghostShipPos;
                }
                UpdateLineRenderer();
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            if (tracePoints.Count > 0 && Vector3.Distance(tracePoints[tracePoints.Count - 1], mousePos) > 1f)
            {
                if (tracePoints.Count < maxTracePoints)
                {
                    tracePoints.Add(mousePos);
                    UpdateLineRenderer();
                }
            }
        }
        else if (Input.GetMouseButtonUp(0)) isDrawing = false;
    }
    #endregion

    #region Execution Phase: Logic & Visuals (Update)
    // 논리적 유령선 위치 이동(FixedUpdate에서 호출)
    private void UpdateGhostShipPosition(float dt)
    {
        if (tracePoints.Count == 0 || ghostTargetIndex >= tracePoints.Count) 
        {
            ghostShipVelocity = Vector2.zero;
            return;
        }

        Vector2 target = tracePoints[ghostTargetIndex];
        float dist = Vector2.Distance(ghostShipPos, target);

        if (dist < 0.5f)
        {
            ghostTargetIndex++;
            if (ghostTargetIndex >= tracePoints.Count) 
            {
                ghostShipVelocity = Vector2.zero;
                return;
            } 
            target = tracePoints[ghostTargetIndex];
        }

        // 유령선이 궤적을 따라 부드럽게 이동하도록 보간
        Vector2 dir = (target - ghostShipPos).normalized;
        float currentGhostSpeed = ((baseSpeed + ghostAccelLevel * forwardAccel) * autoAccelMult);
        
        Vector2 nextPos = ghostShipPos + dir * currentGhostSpeed * dt;
        
        ghostShipVelocity = (nextPos - ghostShipPos) / dt;
        ghostShipPos = nextPos;
    }

    // 시각적 유령선 이동 (Update에서 호출, Lerp 보간)
    private void UpdateGhostShipVisuals()
    {
        if (ghostShipInstance == null) return;

        ghostShipInstance.transform.position = Vector3.Lerp(ghostShipInstance.transform.position, ghostShipPos, Time.deltaTime * 15f);

        // 진행 방향으로 회전 보간
        if (ghostTargetIndex < tracePoints.Count)
        {
            Vector2 dir = ((Vector2)tracePoints[ghostTargetIndex] - (Vector2)ghostShipInstance.transform.position).normalized;
            if (dir != Vector2.zero)
            {
                float ghostAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion targetRot = Quaternion.Euler(0, 0, ghostAngle);
                ghostShipInstance.transform.rotation = Quaternion.Slerp(ghostShipInstance.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
    }

    private void UpdateSyncState()
    {
        CurrentFateDeviation = Vector2.Distance(transform.position, ghostShipPos);
        bool isHoldingShift = GameManager.Instance.IsSteeringMode;

        // 결합되어 있으며, Shift 키가 눌려있는 경우 강제 분리 상태로 전환 (조타수 모드 진입)
        if (IsSynchronized && isHoldingShift) 
        {
            SetSyncState(false);
            isVoluntarilyDetached = true;
        }

        // 강제 분리 상태이며, 슬립스트림 반경보다 멀어지면, 강제 분리 해제 (자동으로 다시 결합 가능하도록)
        if (isVoluntarilyDetached && CurrentFateDeviation > slipstreamRadius)
        {
            isVoluntarilyDetached = false;
        }

        // 강제 분리 상태가 아니라면, 현재 유령선과 플레이어 간의 거리에 따라 자동으로 결합/분리 상태 전환
        if (!isVoluntarilyDetached)
        {
            if (IsSynchronized && CurrentFateDeviation > detachThreshold) 
            {
                SetSyncState(false);
            }
            else if (!IsSynchronized && CurrentFateDeviation <= attachThreshold) 
            {
                SetSyncState(true);
            }
        }

        // 운명 이탈 상태 전환 로직(분리 상태에서 일정 거리 이상 멀어지면 운명 이탈, 가까워지면 복귀)
        if (!IsLost && CurrentFateDeviation >= maxFateDistance) 
        {
            SetLostState(true);
        }
        else if (IsLost && CurrentFateDeviation < maxFateDistance * 0.8f)
        {
            SetLostState(false);
        }
    }

    private void SetSyncState(bool state)
    {
        if (IsSynchronized == state) return;
        IsSynchronized = state;
        // 결합 상태로 변경될 때 가속 레벨을 유령선과 일치시켜 속도 정렬 및 부드러운 전환 유도
        accelLevel = ghostAccelLevel; 
        // 유령선 시각적 피드백: 분리되면 나타나고, 결합하면 숨겨짐
        if (ghostShipInstance != null)
        {
            ghostShipInstance.SetActive(!IsSynchronized);
        }
        OnSyncStateChanged?.Invoke(IsSynchronized);
    }

    private void SetLostState(bool state)
    {
        if (IsLost == state) return;
        IsLost = state;
        OnLostStateChanged?.Invoke(IsLost); 
    }

    // 유령선이 지나간 궤적만을 추적하여 데이터 반환
    private bool TryGetSlipstreamData(out Vector2 nearestPoint, out int segmentStartIndex)
    {
        nearestPoint = Vector2.zero;
        segmentStartIndex = -1;
        if (tracePoints.Count < 2 || ghostTargetIndex < 1) return false;

        Vector2 currentPos = transform.position;
        float minDistSqr = float.MaxValue;
        bool found = false;

        int activeSegments = ghostTargetIndex;

        for (int i = 0; i < tracePoints.Count - 1; i++)
        {
            Vector2 a = tracePoints[i];
            Vector2 b = (i == activeSegments - 1) ? ghostShipPos : (Vector2)tracePoints[i + 1];

            if ((b - a).sqrMagnitude < 0.001f) continue; // 너무 짧은 세그먼트는 무시하여 계산 안정성 확보

            Vector2 ab = b - a;
            float t = Vector2.Dot(currentPos - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + t * ab;

            float distSqr = (currentPos - projection).sqrMagnitude;
            if (distSqr < slipstreamRadius * slipstreamRadius && distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearestPoint = projection;
                segmentStartIndex = i;
                found = true;
            }
        }
        return found;
    }

    // 플레이어가 지나친 궤적(segments) 소거
    private void ConsumeTracePoints()
    {
        // 항로가 교차할 때 대량으로 궤적이 소거되는 현상을 방지하기 위해, 
        // 가장 오래된 궤적(Index 0) 하나만을 대상으로 순차적으로 검사하여 제거
        // 유령선 또한 해당 궤적(Index 1 이상)을 지나간 것이 보장된 상태여야 함
        if (tracePoints.Count < 2 || ghostTargetIndex < 2) return;

        Vector2 a = tracePoints[0];
        Vector2 b = tracePoints[1];
        Vector2 currentPos = transform.position;

        // 플레이어가 a -> b 선분을 실제로 순차적으로 지나갔는지 수학적으로 검증
        Vector2 ab = b - a;
        Vector2 ap = currentPos - a;

        // 궤적(ab)에 플레이어 위치를 투영하여 진행도(t) 계산
        float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;

        // 교차로 인한 엉뚱한 삭제 방지를 위해, 플레이어가 해당 궤적 직선 반경 내에 물리적으로 존재하는지 확인
        Vector2 projection = a + t * ab;
        float distSqr = (currentPos - projection).sqrMagnitude;
        bool isInsideRadius = distSqr <= (slipstreamRadius * slipstreamRadius);

        // 조건 1: 플레이어가 a -> b 궤적을 80% 이상 지나쳤다 (t >= 0.8f)
        // 조건 2: 플레이어가 물리적으로 해당 궤적 반경 안에 있다
        if (t >= 0.8f && isInsideRadius)
        {
            tracePoints.RemoveAt(0);
            ghostTargetIndex--; // 0번 인덱스가 지워졌으므로 유령선 타겟 인덱스도 1 감소
        }
    }
    
    private void EnsureBufferSize(ref Vector3[] buffer, int requiredSize)
    {
        if (buffer == null || buffer.Length < requiredSize)
        {
            int newSize = buffer == null ? 200 : buffer.Length * 2;
            while (newSize < requiredSize) newSize *= 2;
            buffer = new Vector3[newSize];
        }
    }

    private void UpdateLineRenderer()
    {
        if (tracePoints.Count == 0)
        {
            lineRenderer.positionCount = 0;
            slipstreamLineRenderer.positionCount = 0;
            return;
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            lineRenderer.positionCount = tracePoints.Count;
            slipstreamLineRenderer.positionCount = tracePoints.Count;
            EnsureBufferSize(ref futureBuffer, tracePoints.Count);
            tracePoints.CopyTo(futureBuffer);
            lineRenderer.SetPositions(futureBuffer);
            slipstreamLineRenderer.SetPositions(futureBuffer);
            return;
        }

        // 실행 페이즈일 경우: 유령선을 기준으로 지나온 길과 앞으로 갈 길을 나눔
        if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
        {
            if (ghostTargetIndex > 0 && ghostTargetIndex < tracePoints.Count)
            {
                // 1. 유령선이 지나간 경로 (플레이어 꽁무니(0) ~ 유령선 위치)
                if (slipstreamLineRenderer != null)
                {
                    int slipCount = ghostTargetIndex + 1;
                    EnsureBufferSize(ref slipstreamBuffer, slipCount);
                    
                    for (int i = 0; i < ghostTargetIndex; i++)
                    {
                        slipstreamBuffer[i] = tracePoints[i];
                    }
                    slipstreamBuffer[ghostTargetIndex] = ghostShipPos;
                    
                    slipstreamLineRenderer.positionCount = slipCount;
                    slipstreamLineRenderer.SetPositions(slipstreamBuffer);
                }

                // 2. 유령선이 앞으로 갈 경로 (유령선 위치 ~ 목표 끝점)
                int futurePointCount = tracePoints.Count - ghostTargetIndex + 1;
                EnsureBufferSize(ref futureBuffer, futurePointCount);
                
                futureBuffer[0] = ghostShipPos;
                for (int i = ghostTargetIndex; i < tracePoints.Count; i++)
                {
                    futureBuffer[i - ghostTargetIndex + 1] = tracePoints[i];
                }
                
                lineRenderer.positionCount = futurePointCount;
                lineRenderer.SetPositions(futureBuffer);
            }
            else if (ghostTargetIndex >= tracePoints.Count)
            {
                // 유령선이 끝까지 도달했을 때 (전부 지나간 경로)
                if (slipstreamLineRenderer != null)
                {
                    slipstreamLineRenderer.positionCount = tracePoints.Count;
                    EnsureBufferSize(ref slipstreamBuffer, tracePoints.Count);
                    tracePoints.CopyTo(slipstreamBuffer);
                    slipstreamLineRenderer.SetPositions(slipstreamBuffer);
                }
                lineRenderer.positionCount = 0;
            }
            else
            {
                lineRenderer.positionCount = tracePoints.Count;
                EnsureBufferSize(ref futureBuffer, tracePoints.Count);
                tracePoints.CopyTo(futureBuffer);
                lineRenderer.SetPositions(futureBuffer);
                
                if (slipstreamLineRenderer != null) slipstreamLineRenderer.positionCount = 0;
            }
        }
    }

    #endregion

    #region Execution Phase: Physics (FixedUpdate)
    // 마스터 - 슬레이브 추종 모델
    private void ExecuteSynchronizedMovement(float dt)
    {
        // 1. 궤도 연산을 끄고 오직 마스터(유령선)의 위치와 속도만 바라봅니다.
        Vector2 toGhost = ghostShipPos - (Vector2)transform.position;
        
        // 2. 유령선의 현재 속도에, 오차를 줄이기 위한 강제 스프링 보정값을 더합니다.
        // 물의 마찰력(damping)을 완전히 씹어먹을 수 있도록 속도를 강제로 일치시킵니다.
        Vector2 catchUpVelocity = toGhost * syncSpringForce;
        rb.linearVelocity = ghostShipVelocity + catchUpVelocity;

        // 3. 회전 처리 (유령선이 이동 중일 때만 회전)
        if (ghostShipVelocity.sqrMagnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(ghostShipVelocity.y, ghostShipVelocity.x) * Mathf.Rad2Deg;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, dt * turnSpeed * 2f);
        }

        currentSlipstreamMultiplier = 1.0f;
    }

    private void ExecuteManualMovement(float dt)
    {
        // 선박형 조작 연산
        float turnInput = Input.GetAxisRaw("Horizontal");
        currentAngle -= turnInput * turnSpeed * 100f * dt;

        bool accelCounted = false;
        float throttleInput = 0f;

        if (!accelCounted)
        {
            throttleInput = Input.GetAxisRaw("Vertical");
        }
        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector2 forwardVec = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // 슬립스트림 배율(기본값)과 그립력 초기화
        float targetSlipstreamMultiplier = 1.0f;
        Vector2 gripForce = Vector2.zero;

        // 강제 분리 상태가 아니고, 슬립스트림 반경 내에 있으며, 결합 상태도 아니라면, 슬립스트림 효과 적용
        if (!isVoluntarilyDetached && TryGetSlipstreamData(out Vector2 nearestPoint, out int segmentIndex) && !IsSynchronized)
        {
            targetSlipstreamMultiplier = maxSlipstreamMultiplier;

            Vector2 dirToRail = (nearestPoint - (Vector2)transform.position).normalized;
            float distToRail = Vector2.Distance(transform.position, nearestPoint);
            // 수동 조작 상태에서 슬립스트림에 들어가면 그립력을 적용하여 궤적에서 이탈하지 않도록 보정
            gripForce = dirToRail * (distToRail * slipstreamGripForce * (rb.linearVelocity.magnitude * 0.5f));
        }

        currentSlipstreamMultiplier = Mathf.Lerp(currentSlipstreamMultiplier, targetSlipstreamMultiplier, dt * 5f);

        // 플레이어 입력 횟수를 감지하여 가속 레벨 변화 (0: 정지, 1: 전진, -1: 후진, 2: 최대 전진, -2: 최대 후진)
        if (throttleInput >= 0 && (accelLevel * forwardAccel) < maxForwardSpeed)
        {
            // 전진 입력이 있을 때 가속 레벨 1 증가 (최대 전진 속도까지)
            accelLevel += throttleInput;
            accelCounted = true;
        }
        else if (throttleInput <= 0 && (accelLevel * backwardAccel) > maxBackwardSpeed)
        {
            // 후진 입력이 있을 때 가속 레벨 1 감소 (최대 후진 속도까지)
            accelLevel += throttleInput;
            accelCounted = true;
        }
        else if (throttleInput == 0)
        {
            accelCounted = false;
        }
        
        // 가속 레벨이 최대 범위에 도달했거나, 입력이 없는 경우, 현재 가속 레벨에 따라 목표 속력 계산
        float targetSpeed = (accelLevel >= 0) ? baseSpeed + (accelLevel * forwardAccel) : baseSpeed + (accelLevel * backwardAccel);
        // 결합 상태가 아니라면, 목표 속력에 슬립스트림 배율을 적용하여 최종 추진력 계산
        if(!IsSynchronized)
        {
            float finalThrust = targetSpeed * currentSlipstreamMultiplier;
            rb.AddForce((forwardVec * finalThrust) + gripForce, ForceMode2D.Force);
        }
        else
        {
            rb.AddForce((forwardVec * targetSpeed) + gripForce, ForceMode2D.Force);
        }
    }
    #endregion

    private void UpdateCannonAngle()
    {
        if (cannonSprite == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        float mouseAngle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg;

        // 선박의 좌현/우현 각도 도출
        float leftSide = currentAngle + 90f;
        float rightSide = currentAngle - 90f;

        // 마우스와 좌/우현 간의 최단 각도 차이 연산
        float diffLeft = Mathf.Abs(Mathf.DeltaAngle(leftSide, mouseAngle));
        float diffRight = Mathf.Abs(Mathf.DeltaAngle(rightSide, mouseAngle));

        // 마우스와 더 가까운 현(side)을 기준각으로 설정
        float baseAngle = (diffLeft < diffRight) ? leftSide : rightSide;

        // 기준각으로부터 마우스까지의 각도 차이를 구하고, 최대 회전 범위를 Clamp
        float delta = Mathf.DeltaAngle(baseAngle, mouseAngle);
        delta = Mathf.Clamp(delta, -maxCannonDeviation, maxCannonDeviation);

        CannonAngle = baseAngle + delta;
        cannonSprite.transform.rotation = Quaternion.Euler(0f, 0f, CannonAngle);
    }
}