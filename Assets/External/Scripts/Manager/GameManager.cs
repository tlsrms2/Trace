using System;
using System.Collections;
using UnityEngine;

public enum GamePhase { Paused, Replay, RealTime, GameOver }
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action OnTraceStarted;
    public event Action OnTraceEnded;

    public bool IsPaused { get; private set; }
    public GamePhase CurrentPhase = GamePhase.RealTime;
    
    [Header("Gauge Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge;
    [SerializeField] private float ConsumptionRate = 20f;
    [SerializeField] private float RecoveryRate = 10f;
    [Tooltip("리플레이 후 게이지 충전 시작 시간")][SerializeField] private float RecoveryStartTime = 1f;

    private Coroutine chargeGaugeCor;
    private bool canCharge;

    public bool isPaused => CurrentPhase == GamePhase.Paused;

    void Awake()
    {
        Instance = this;
        OnTraceEnded += StartChargeWait;
    }

    void Update()
    {
        if (CurrentPhase == GamePhase.GameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Space) && CurrentPhase == GamePhase.RealTime)
        {
            ChangePhase(GamePhase.Paused);
        }

        if (Input.GetKeyUp(KeyCode.Space) && CurrentPhase == GamePhase.Paused)
        {
            ChangePhase(GamePhase.Replay);
        }

        HandleGauge();
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        UIManager.Instance.ShowPause();
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        UIManager.Instance.HidePause();
    }

    public float GetCurrentGauge()
    {
        return CurrentGauge;
    }
    
    public float GetGaugePercentage()
    {
        return CurrentGauge / MaxGauge;
    }

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
        if (chargeGaugeCor != null)
            StopCoroutine(chargeGaugeCor);
        chargeGaugeCor = StartCoroutine(WaitChargeGauge());
    }

    private IEnumerator WaitChargeGauge()
    {
        yield return new WaitForSeconds(RecoveryStartTime);
        StartCharge();
    }

    private void StartCharge()
    {
        canCharge = true;
    }

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

            default:
                break;
        }

        CurrentPhase = nextPhase;

        switch (nextPhase)
        {
            case GamePhase.Paused:
                break;

            case GamePhase.Replay:
                break;

            case GamePhase.RealTime:
                break;

            case GamePhase.GameOver:
                break;
        }
    }
}