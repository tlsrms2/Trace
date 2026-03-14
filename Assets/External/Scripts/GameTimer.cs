using UnityEngine;
using TMPro;
using System.Collections;
using UnityEditor.Rendering;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI timerToDayText;
    public TextMeshProUGUI timerToWeekText;
    public TextMeshProUGUI timerToMonthText;
    public TextMeshProUGUI gameOverTimerText;
    public TextMeshProUGUI gameClearTimerText;

    [Header("Timer Settings")]
    public float currentTime = 0;
    public bool isRunning = true;
    public int months = 1;
    public int weeks = 1;
    public int days = 1;
    public int hours;
    public int minutes;
    public int seconds;
    public int milliseconds;

    private int previousTotalDays = -1;
    private int currentYear = 1;
    private readonly int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    [Header("Distance Fading Settings")]
    public Transform player; // 플레이어의 Transform
    public Transform timerWorldPosition; // 거리를 잴 타이머의 월드 위치 (비워두면 이 스크립트가 붙은 오브젝트 기준)
    
    public float fadeStartDistance = 10f; // 이 거리보다 가까워지면 투명해지기 시작함
    public float fadeEndDistance = 3f;    // 이 거리보다 가까워지면 최소 투명도 유지
    
    [Range(0f, 1f)] public float maxAlpha = 1f;   // 멀리 있을 때의 투명도 (1 = 완전 불투명)
    [Range(0f, 1f)] public float minAlpha = 0f; // 가까이 있을 때의 투명도 (0.2 = 많이 투명함)

    [Header("Time Reduce Settings")]
    public TextMeshProUGUI reduceText;
    public float accumulatedAmount = 0f;
    public float effectDuration = 1f;
    public Coroutine effectCoroutine;

    void Start()
    {
        // 기준점 위치를 따로 할당하지 않았다면, 이 스크립트가 붙은 오브젝트를 기준으로 삼습니다.
        if (timerWorldPosition == null)
        {
            timerWorldPosition = this.transform;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StopGameOverTimer;
            GameManager.Instance.OnGameClear += StopGameClearTimer;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= StopGameOverTimer;
            GameManager.Instance.OnGameClear -= StopGameClearTimer;
        }
    }

    void Update()
    {
        // 1. 타이머 시간 계산
        if (isRunning && GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();

            // 총 며칠(24초당 1일)이 지났는지 계산
            int currentTotalDays = Mathf.FloorToInt(currentTime / 24f);
            if (currentTotalDays != previousTotalDays)
            {
                UpdateCalendar(currentTotalDays);
                previousTotalDays = currentTotalDays;
            }
        }
    }

    void StopGameOverTimer()
    {
        isRunning = false;
        if (gameOverTimerText != null)
        {
            UpdateGameOverDisplay();
        }
    }

    void StopGameClearTimer()
    {
        isRunning = false;
        if (gameClearTimerText != null)
        {
            UpdateGameClearDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        hours = Mathf.FloorToInt(currentTime / 3600f);
        minutes = Mathf.FloorToInt((currentTime % 3600f) / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    void UpdateCalendar(int totalDays)
    {
        // 월(Month) 및 일(Day) 계산
        int tempDays = totalDays;
        int tempMonth = 1;
        int tempYear = 1;

        while (true)
        {
            int maxDays = daysInMonth[tempMonth - 1];
            
            // 윤년 계산 (4년에 한 번 2월은 29일)
            if (tempMonth == 2 && (tempYear % 4 == 0 && (tempYear % 100 != 0 || tempYear % 400 == 0)))
            {
                maxDays = 29;
            }

            // 남은 일수가 이번 달의 최대 일수보다 크거나 같다면 다음 달로 넘어감
            if (tempDays >= maxDays)
            {
                tempDays -= maxDays;
                tempMonth++;
                if (tempMonth > 12)
                {
                    tempMonth = 1;
                    tempYear++;
                }
            }
            else
            {
                break;
            }
        }

        months = tempMonth;
        days = 1 + tempDays;
        currentYear = tempYear;

        // 주(Week) 계산: 현재 달의 몇 주차인지 계산 (1~5주차)
        weeks = Mathf.FloorToInt((days - 1) / 7f) + 1;

        UpdateDayTimerDisplay();
        UpdateWeekTimerDisplay();
        UpdateMonthTimerDisplay();
    }

    void UpdateDayTimerDisplay()
    {
        if (timerToDayText != null)
            timerToDayText.text = string.Format("{0:0}", days);
    }

    void UpdateWeekTimerDisplay()
    {
        if (timerToWeekText != null && weeks == 1)
            timerToWeekText.text = string.Format("{0:0}st Week", weeks);
        else if (timerToWeekText != null && weeks == 2)
            timerToWeekText.text = string.Format("{0:0}nd Week", weeks);
        else if (timerToWeekText != null && weeks == 3)
            timerToWeekText.text = string.Format("{0:0}rd Week", weeks);
        else if (timerToWeekText != null && weeks > 3)
            timerToWeekText.text = string.Format("{0:0}th Week", weeks);
    }

    void UpdateMonthTimerDisplay()
    {
        if (timerToMonthText != null)
            timerToMonthText.text = string.Format("{0:0}", months);
    }

    void UpdateGameOverDisplay()
    {
        hours = Mathf.FloorToInt(currentTime / 3600f);
        minutes = Mathf.FloorToInt((currentTime % 3600f) / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);
        
        gameOverTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    void UpdateGameClearDisplay()
    {
        hours = Mathf.FloorToInt(currentTime / 3600f);
        minutes = Mathf.FloorToInt((currentTime % 3600f) / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);
        
        gameClearTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    public void ReduceTime(int amount)
    {
        currentTime = Mathf.Max(0, currentTime - amount);

        accumulatedAmount += amount;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = StartCoroutine(ShowReduceEffectRoutine());
    }

    private IEnumerator ShowReduceEffectRoutine()
    {
        yield return new WaitForEndOfFrame();

        reduceText.gameObject.SetActive(true);
        reduceText.text = $"- {accumulatedAmount:F0}:00";

        float timer = 0f;
        Color c = reduceText.color;
        while (timer < effectDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / effectDuration;

            c.a = Mathf.Lerp(1f, 0f, ratio);
            reduceText.color = c;

            yield return null;
        }
        
        reduceText.gameObject.SetActive(false);
        accumulatedAmount = 0f;
        effectCoroutine = null;
    }
}