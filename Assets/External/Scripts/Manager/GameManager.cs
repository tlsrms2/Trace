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

    private string playerName;
    private bool secondPanelReady = false; 

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
    
    // 외부(ShipController)에서 참조할 조타수 모드 상태
    public bool IsSteeringMode { get; private set; } = false;

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
    }

    private void Start()
    {
        TitleUIFocus();
        UpdateLeaderboardUI();
        
        // 게임 시작 시 무조건 계획 페이즈(시간 정지) 돌입
        ChangePhase(GamePhase.Paused);
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

    // 기존의 HandleFocusGauge를 토글 방식의 HandleSteeringGauge로 변경
    private void HandleSteeringGauge()
    {
        // 1. 실행(항해) 페이즈일 때만 게이지 소모/회복 처리
        if (CurrentPhase == GamePhase.RealTime)
        {
            // Shift 키 토글 감지 (GetKeyDown 적용)
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                if (IsSteeringMode)
                {
                    // 이미 켜져 있다면 수동으로 끄기
                    DisableSteeringMode();
                }
                else if (CurrentGauge > 0)
                {
                    // 꺼져 있고 게이지가 남아있다면 켜기
                    IsSteeringMode = true;
                    CurrentGauge = Mathf.Max(0, CurrentGauge - (MaxGauge * 0.05f)); // 착수금 5% 차감
                    canCharge = false;
                    
                    // 혹시 대기 중이던 충전 코루틴이 있다면 취소하여 중복 방지
                    if (chargeGaugeCor != null) 
                        StopCoroutine(chargeGaugeCor); 
                }
            }

            // 게이지 소모 진행
            if (IsSteeringMode)
            {
                canCharge = false;
                CurrentGauge -= ConsumptionRate * Time.deltaTime;

                // 게이지 고갈 시 강제 해제 이벤트 발송
                if (CurrentGauge <= 0)
                {
                    CurrentGauge = 0;
                    DisableSteeringMode();
                    OnSteeringExhausted?.Invoke();
                }
            }
        }
        else // 계획 페이즈 (Paused)
        {
            if (IsSteeringMode)
            {
                IsSteeringMode = false;
            }
            canCharge = false; // 계획 중에는 게이지 변동 없음
        }

        if (canCharge)
        {
            CurrentGauge = Mathf.Min(MaxGauge, CurrentGauge + RecoveryRate * Time.deltaTime);
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
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("TitleScene"); }
    public void StartGame() { Time.timeScale = 1f; SceneManager.LoadScene("GameScene"); }
    public void QuitGame() { Application.Quit(); }
    #endregion
}