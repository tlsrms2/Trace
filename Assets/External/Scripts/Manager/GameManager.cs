using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public enum GamePhase { Paused, Replay, RealTime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action OnGameOver;
    public event Action OnGameClear;
    public event Action OnTraceStarted;
    public event Action OnTraceEnded;
    
    // 조타수 개입(Shift) 유지 실패 시 발생하는 이벤트
    public event Action OnSteeringExhausted; 

    public bool IsPaused { get; private set; }
    
    // 시작 시 계획 페이즈(Paused)로 돌입하도록 변경
    public GamePhase CurrentPhase = GamePhase.Paused; 
    public bool isPaused => CurrentPhase == GamePhase.Paused;

    [Header("운명 일치율 (Fate Sync Rate)")]
    [Tooltip("절대적인 최대 운명 일치율")]
    [SerializeField] private float absoluteMaxSyncRate = 100f;
    [Tooltip("단절 상태일 때 초당 감소하는 일치율")]
    [SerializeField] private float syncDepletionRate = 10f; 
    [Tooltip("단절 상태일 때 초당 깎여나가는 '최대치(영구적 저주)'")]
    [SerializeField] private float maxSyncPenaltyRate = 1f; 
    [Tooltip("궤도 결합(Synced) 상태일 때 회복되는 일치율 속도")]
    [SerializeField] private float syncRecoveryRate = 5f;

    public float CurrentMaxSyncRate { get; private set; }
    public float CurrentFateSyncRate { get; private set; }
    private bool isShipLost = false;

    [Header("UI Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject clearText;
    [SerializeField] private GameObject timerText;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject firstClearPanel;
    [SerializeField] private GameObject secondClearPanel;
    [SerializeField] private GameObject thirdClearPanel;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TMP_InputField nameInputField;

    [Header("UI Keyboard Focus Settings")]
    [SerializeField] private GameObject firstTitleButton;
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstGameOverButton;
    [SerializeField] private GameObject firstGameClearInputButton;
    [SerializeField] private GameObject secondClearButton;
    [SerializeField] private GameObject thirdSelectButton;

    [Header("Leaderboard UI")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Gauge(Focus) Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge = 100f;
    [SerializeField] private float ConsumptionRate = 20f;
    [SerializeField] private float RecoveryRate = 10f;
    [SerializeField] private float RecoveryStartTime = 1f;

    private Coroutine chargeGaugeCor;
    private bool canCharge;
    private bool secondPanelReady = false; 
    private string playerName;
    
    // 외부(ShipController)에서 참조할 조타수 모드 상태
    public bool IsSteeringMode { get; private set; } = false;
    private ShipController shipController; // 참조할 ShipController 컴포넌트

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        Cursor.visible = true; // 계획 페이즈를 위해 마우스 커서 활성화
        Cursor.lockState = CursorLockMode.None;
        OnTraceEnded += StartChargeWait;

        //초기 설정
        CurrentMaxSyncRate = absoluteMaxSyncRate;
        CurrentFateSyncRate = absoluteMaxSyncRate;
    }

    private void Start()
    {
        shipController = FindFirstObjectByType<ShipController>();
        if (shipController == null)
        {
            shipController.OnLostStateChanged += HandleLostState;
        }

        TitleUIFocus();
        UpdateLeaderboardUI();
        
        // 게임 시작 시 무조건 계획 페이즈(시간 정지) 돌입
        ChangePhase(GamePhase.Paused);
    }

    private void OnDestroy()
    {
        if (shipController != null)
        {
            shipController.OnLostStateChanged -= HandleLostState;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // [Space] 키 : 계획 페이즈(해도를 다 그림) -> 실행 페이즈(출항) 전환
        if (Input.GetKeyDown(KeyCode.Space) && CurrentPhase == GamePhase.Paused && !IsPaused)
        {
            ChangePhase(GamePhase.RealTime);
        }

        if (Input.GetKeyDown(KeyCode.R) && !firstClearPanel.activeSelf)
        {
            RestartGame();
        }

        HandleSteeringGauge();
        HandleFateSyncRate();

        if (secondPanelReady && secondClearPanel != null && secondClearPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            secondClearPanel.SetActive(false);
            thirdClearPanel.SetActive(true);
            clearText?.SetActive(true);
            timerText?.SetActive(true);
            SetUIFocus(thirdSelectButton); 
            secondPanelReady = false; 
        }
    }

    private void HandleLostState(bool lost)
    {
        isShipLost = lost;
    }

    private void HandleFateSyncRate()
    {
        if (CurrentPhase != GamePhase.RealTime) return;

        if (isShipLost)
        {
            // 1. 현재 일치율 급속 감소
            CurrentFateSyncRate -= syncDepletionRate * Time.deltaTime;
            // 2. 일치율 최대치(Max) 영구 감소 (어뷰징 방지용 흉터)
            CurrentMaxSyncRate -= maxSyncPenaltyRate * Time.deltaTime;
            
            CurrentMaxSyncRate = Mathf.Max(1f, CurrentMaxSyncRate); // 최소 1%의 최대치는 보장
            CurrentFateSyncRate = Mathf.Max(0f, CurrentFateSyncRate);

            if (CurrentFateSyncRate <= 0)
            {
                GameOver(); // 저주 침식으로 인한 게임오버
            }
        }
        else
        {
            // 결합 상태(Synced)일 때 서서히 회복하되, 훼손된 'CurrentMaxSyncRate'까지만 회복됨
            if (shipController != null && shipController.IsSynchronized)
            {
                if (CurrentFateSyncRate < CurrentMaxSyncRate)
                {
                    CurrentFateSyncRate += syncRecoveryRate * Time.deltaTime;
                    CurrentFateSyncRate = Mathf.Min(CurrentFateSyncRate, CurrentMaxSyncRate);
                }
            }
        }
    }

    #region Game Flow & Phase Control
    public void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase) return;

        switch (CurrentPhase)
        {
            case GamePhase.Paused: OnTraceEnded?.Invoke(); break;
            case GamePhase.RealTime: OnTraceStarted?.Invoke(); break;
        }

        CurrentPhase = nextPhase;

        switch (nextPhase)
        {
            case GamePhase.Paused: AudioManager.Instance.SetSlowBgm(); break;
            case GamePhase.Replay:
            case GamePhase.RealTime: AudioManager.Instance.SetNormalBgm(); break;
        }
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void TitleUIFocus()
    {
        if (firstTitleButton != null)
            SetUIFocus(firstTitleButton);
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
            SetUIFocus(firstPauseButton);
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void GameOver()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;
        ChangePhase(GamePhase.Paused); 
        StartCoroutine(ShowGameOverPanelRoutine());
        OnGameOver?.Invoke();
    }

    private IEnumerator ShowGameOverPanelRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            SetUIFocus(firstGameOverButton);
        }
    }

    public void GameClear()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
            firstClearPanel.SetActive(true);
            SetUIFocus(firstGameClearInputButton);
        }
        OnGameClear?.Invoke();
    }

    public void OnFirstPanelSubmit()
    {
        playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName))
            playerName = "Unnamed";

        LeaderboardManager.Instance.AddScore(playerName, GameTimer.Instance.currentTime);

        firstClearPanel.SetActive(false);
        secondClearPanel.SetActive(true);

        clearText.SetActive(false);
        timerText.SetActive(false);

        UpdateSecondPanelLeaderboard();

        EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(EnableSecondPanelInputNextFrame());
    }

    private IEnumerator EnableSecondPanelInputNextFrame()
    {
        yield return null;
        secondPanelReady = true;
    }

    private void UpdateSecondPanelLeaderboard()
    {
        if (LeaderboardManager.Instance == null) return;
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard();
        int count = Mathf.Min(leaderboard.Count, 10); 
        rankText.text = ""; nameText.text = ""; timeText.text = "";
        for (int i = 0; i < count; i++)
        {
            var entry = leaderboard[i];
            rankText.text += entry.rank + "\n";
            nameText.text += entry.playerName + "\n";
            timeText.text += entry.clearTime.ToString("F2") + "\n";
        }
    }

    private void SetUIFocus(GameObject firstSelected)
    {
        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
    #endregion

    #region Gauge(Focus) Logic
    public float GetCurrentGauge() => CurrentGauge;
    public float GetGaugePercentage() => CurrentGauge / MaxGauge;

    // UI에서 현재 일치율 비율을 가져갈 때 사용 (0.0 ~ 1.0)
    public float GetFateSyncPercentage() => CurrentFateSyncRate / absoluteMaxSyncRate;
    // UI에서 최대치 훼손율을 가져갈 때 사용 (Vignette 이펙트 등에 활용)
    public float GetMaxSyncPenaltyPercentage() => CurrentMaxSyncRate / absoluteMaxSyncRate;
    // 기존의 HandleFocusGauge를 토글 방식의 HandleSteeringGauge로 변경
    private void HandleSteeringGauge()
    {
        // 계획 페이즈에서는 게이지 변동 없음
        if (CurrentPhase != GamePhase.RealTime)
        {
            if (IsSteeringMode) 
            {
                DisableSteeringMode();
            }
            canCharge = false;
            return;
        }

        bool isShipDetached = shipController != null && !shipController.IsSynchronized;
        // Shift 키가 눌려있는지 여부 체크
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 조타수 모드 진입 조건: Shift 키를 누르고 있고, 현재 단절 상태이며, 게이지가 남아있어야 함
        if (isShipDetached && isHoldingShift && CurrentGauge > 0)
        {
            IsSteeringMode = true;
            canCharge = false;
            // 게이지 감소 처리 (초당 ConsumptionRate만큼 감소)
            if (chargeGaugeCor != null) 
            { 
                StopCoroutine(chargeGaugeCor); 
                chargeGaugeCor = null; 
            }

            CurrentGauge -= ConsumptionRate * Time.deltaTime;
            
            // 게이지가 0 이하로 떨어지면 조타수 모드 강제 해제
            if (CurrentGauge <= 0)
            {
                CurrentGauge = 0;
                DisableSteeringMode();
                OnSteeringExhausted?.Invoke();
            }
        }
        else
        {
            // 조타수 모드가 아닐 때는 게이지 회복 처리
            if (IsSteeringMode) 
            {
                DisableSteeringMode();
            }
            // 게이지 회복 처리 (초당 RecoveryRate만큼 회복)
            if (canCharge) 
            {
                CurrentGauge = Mathf.Min(MaxGauge, CurrentGauge + RecoveryRate * Time.deltaTime);
            }
        }
    }

    // 모드 해제 시 공통으로 처리해야 할 로직(회복 유예 코루틴 시작 등)을 묶음
    private void DisableSteeringMode()
    {
        IsSteeringMode = false;
        StartChargeWait();
    }

    private void StartChargeWait()
    {
        if (chargeGaugeCor != null) StopCoroutine(chargeGaugeCor);
        chargeGaugeCor = StartCoroutine(WaitChargeGauge());
    }

    private IEnumerator WaitChargeGauge()
    {
        yield return new WaitForSeconds(RecoveryStartTime);
        canCharge = true;
    }
    #endregion

    #region Scene & UI Interaction
    public void UpdateLeaderboardUI()
    {
        if (LeaderboardManager.Instance == null || rankText == null) return;
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard();
        rankText.text = ""; nameText.text = ""; timeText.text = "";
        foreach (var entry in leaderboard)
        {
            rankText.text += entry.rank + "\n";
            nameText.text += entry.playerName + "\n";
            timeText.text += entry.clearTime.ToString("F2") + "\n";
        }
    }
    public void RestartGame() 
    { 
        Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
    public void GoToMainMenu() 
    { 
        Time.timeScale = 1f; SceneManager.LoadScene("TitleScene"); 
    }
    public void StartGame() 
    { 
        Time.timeScale = 1f; SceneManager.LoadScene("GameScene"); 
    }
    public void QuitGame() 
    { 
        Application.Quit(); 
    }
    #endregion
}