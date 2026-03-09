using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool IsPaused { get; private set; }

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
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

    public void ConsumeGauge()
    {
        CurrentGauge -= ConsumptionRate * Time.deltaTime;
        if (CurrentGauge < 0f)
        {
            CurrentGauge = 0f;
        }
    }

    public void RecoverGauge()
    {
        CurrentGauge += RecoveryRate * Time.deltaTime;
        if (CurrentGauge > MaxGauge)
        {
            CurrentGauge = MaxGauge;
        }
    }
}