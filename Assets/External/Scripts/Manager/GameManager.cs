using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // 키보드 UI 제어를 위해 추가
using TMPro;

public enum GamePhase { Paused, Replay, RealTime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action OnGameOver;
    public event Action OnGameClear;
    public event Action OnTraceStarted;
    public event Action OnTraceEnded;

    public bool IsPaused { get; private set; }
    public GamePhase CurrentPhase = GamePhase.RealTime;
    public bool isPaused => CurrentPhase == GamePhase.Paused;
    
    [Header("UI Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;       // 클리어 시 보여줄 패널
    [SerializeField] private GameObject firstClearPanel;     // 클리어 시 보여줄 첫 번째 입력 패널
    [SerializeField] private GameObject secondClearPanel;    // 첫 번째 패널 입력 완료 후 표시할 두 번째 클리어 패널
    [SerializeField] private GameObject pauseMenu;

    [Header("UI Keyboard Focus Settings")]
    [Tooltip("타이틀 창이 뜰 때 처음 선택될 버튼 (예: Start Button)")]
    [SerializeField] private GameObject firstTitleButton;
    [Tooltip("일시정지 창이 뜰 때 처음 선택될 버튼 (예: Resume Button)")]
    [SerializeField] private GameObject firstPauseButton;
    [Tooltip("게임오버 창이 뜰 때 처음 선택될 버튼 (예: Restart Button)")]
    [SerializeField] private GameObject firstGameOverButton;
    [Tooltip("게임 클리어 창이 뜰 때 처음 선택될 버튼 (예: Next Button)")]
    [SerializeField] private GameObject firstGameClearInputButton;
    [Tooltip("첫 번째 클리어 패널에서 입력 완료 후 두 번째 패널이 뜰 때 선택될 버튼")]
    [SerializeField] private GameObject secondClearButton;

    [Header("Leaderboard UI")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Gauge Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge;
    [SerializeField] private float ConsumptionRate = 20f;
    [SerializeField] private float RecoveryRate = 10f;
    [Tooltip("게이지 회복 시작 대기 시간")]
    [SerializeField] private float RecoveryStartTime = 1f;

    private Coroutine chargeGaugeCor;
    private bool canCharge;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        OnTraceEnded += StartChargeWait;
    }

    void Start()
    {
        TitleUIFocus();
        UpdateLeaderboardUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Space) && CurrentPhase == GamePhase.RealTime && !IsPaused)
        {
            ChangePhase(GamePhase.Paused);
        }

        if (Input.GetKeyUp(KeyCode.Space) && CurrentPhase == GamePhase.Paused)
        {
            ChangePhase(GamePhase.Replay);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        HandleGauge();
    }

    #region Game Flow & Phase Control
    public void ChangePhase(GamePhase nextPhase)
{
    if (CurrentPhase == nextPhase) return;

    switch (CurrentPhase)
    {
        case GamePhase.Paused:
            OnTraceEnded?.Invoke();
            break;

        case GamePhase.RealTime:
            OnTraceStarted?.Invoke();
            break;
    }

    CurrentPhase = nextPhase;

    // 🔊 BGM Pitch 변경
    switch (nextPhase)
    {
        case GamePhase.Paused:
            AudioManager.Instance.SetSlowBgm();   // 시간 정지 느낌
            break;

        case GamePhase.Replay:
        case GamePhase.RealTime:
            AudioManager.Instance.SetNormalBgm(); // 원래 속도
            break;
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
        {
            SetUIFocus(firstTitleButton); // 타이틀 창이 뜰 때 첫 버튼 자동 선택
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null) 
        {
            pauseMenu.SetActive(true);
            SetUIFocus(firstPauseButton); // ⬅️ 추가: 창이 열릴 때 첫 버튼 자동 선택
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null) 
        {
            pauseMenu.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null); // ⬅️ 추가: 포커스 해제
        }
    }

    public void GameOver()
    {
        StartCoroutine(ShowGameOverPanelRoutine());
        OnGameOver?.Invoke();
    }

    private IEnumerator ShowGameOverPanelRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(true);
            SetUIFocus(firstGameOverButton); // ⬅️ 추가: 게임오버 창 첫 버튼 자동 선택
        }
    }

    public void GameClear()
    {
        if (gameClearPanel != null) 
        {
            gameClearPanel.SetActive(true);
            SetUIFocus(firstGameClearInputButton);
        }
        OnGameClear?.Invoke();
    }

    public void ClearPanelChange()
    {
        firstClearPanel.SetActive(false);
        secondClearPanel.SetActive(true);
        SetUIFocus(secondClearButton);
    }

    // 💡 UI 포커스를 설정해주는 헬퍼 함수
    private void SetUIFocus(GameObject firstSelected)
    {
        if (firstSelected != null && EventSystem.current != null)
        {
            // 기존 포커스를 지우고 새로 설정해야 확실하게 적용됩니다.
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
    #endregion

    #region Gauge Logic
    public float GetCurrentGauge() { return CurrentGauge; }
    public float GetGaugePercentage() { return CurrentGauge / MaxGauge; }

    private void HandleGauge()
    {
        if (CurrentPhase == GamePhase.Paused)
        {
            canCharge = false;
            CurrentGauge -= ConsumptionRate * Time.unscaledDeltaTime;
            if (CurrentGauge <= 0)
            {
                CurrentGauge = 0;
                ChangePhase(GamePhase.Replay);
            }
        }

        if (canCharge)
        {
            CurrentGauge = Mathf.Min(MaxGauge, CurrentGauge + RecoveryRate * Time.deltaTime);
        }
    }

    private void StartChargeWait()
    {
        if (chargeGaugeCor != null) StopCoroutine(chargeGaugeCor);
        chargeGaugeCor = StartCoroutine(WaitChargeGauge());
    }

    private IEnumerator WaitChargeGauge()
    {
        yield return new WaitForSeconds(RecoveryStartTime);
        StartCharge();
    }

    private void StartCharge() { canCharge = true; }
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
    #endregion
    
}