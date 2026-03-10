using UnityEngine;

public enum GamePhase { Paused, Replay, RealTime, GameOver }
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool IsPaused { get; private set; }
    public GamePhase CurrentPhase = GamePhase.RealTime;
    
    [Header("Gauge Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge;
    [SerializeField] private float ConsumptionRate = 20f;
    [SerializeField] private float RecoveryRate = 10f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (CurrentPhase == GamePhase.GameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            switch (CurrentPhase)
            {
                case GamePhase.Paused:
                    ChangePhase(GamePhase.Replay); 
                    break;
                case GamePhase.RealTime:
                    ChangePhase(GamePhase.Paused);
                    break;
            }
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
            CurrentGauge -= ConsumptionRate * Time.unscaledDeltaTime;
            if (CurrentGauge <= 0)
            {
                CurrentGauge = 0;
                ChangePhase(GamePhase.Replay); 
            }
        }
        else if (CurrentPhase == GamePhase.RealTime)
        {
            CurrentGauge = Mathf.Min(MaxGauge, CurrentGauge + RecoveryRate * Time.deltaTime);
        }
    }

    public void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase) return;

        CurrentPhase = nextPhase;

        switch (nextPhase)
        {
            case GamePhase.Paused:
                Time.timeScale = 0f; 
                break;

            case GamePhase.Replay:
                Time.timeScale = 1f;
                break;

            case GamePhase.RealTime:
                Time.timeScale = 1f;
                break;

            case GamePhase.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }
}