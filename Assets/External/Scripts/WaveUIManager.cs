using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUIManager : MonoBehaviour
{
    [Header("Wave Info UI")]
    public TextMeshProUGUI waveText;        // 예: "WAVE 1 / 3"
    public TextMeshProUGUI enemyCountText;  // 예: "15 / 20"
    
    [Header("Visual Effect Strategy")]
    [Tooltip("원하는 연출 스크립트(WidthScale 또는 RandomSpawn)를 여기에 끌어다 놓으세요.")]
    public WaveVisualEffect activeVisualEffect; 

    /* (구)게이지 변수
    public Slider progressGauge;            // 남은 적 표시 슬라이더
    public Image gaugeFillImage;            // 게이지 이미지
    public Color normalGauge;               // 일반 게이지 색상
    public Color bossGauge;                 // 보스 게이지 색상
    
    [Header("Gauge Ticks (눈금)")]
    public Transform tickContainer;         // 눈금 이미지들을 묶어둘 부모 Transform (Slider 크기와 동일해야 함)
    public GameObject tickPrefab;           // 눈금 역할을 할 얇은 막대 이미지 프리팹
    public float tickWidth = 5.0f;          // 눈금 두께
    */

    [Header("Game Clear UI")]
    public GameObject gameClearPanel;       // 클리어 시 보여줄 패널

    private bool isBossMode = false;

    private void Start()
    {
        // 1. WaveManager의 이벤트 구독
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveUpdated += UpdateWaveInfo;
            WaveManager.Instance.OnEnemyProgressUpdated += UpdateEnemyGauge;
            
            // 보스 이벤트 구독
            WaveManager.Instance.OnWaveModeChanged += SetMode;
            WaveManager.Instance.OnBossHpUpdated += UpdateBossGauge;

            WaveManager.Instance.OnAllWavesCleared += ShowClearScreen;
        }

        gameClearPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 2. 메모리 누수 방지를 위한 구독 해제
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveUpdated -= UpdateWaveInfo;
            WaveManager.Instance.OnEnemyProgressUpdated += UpdateEnemyGauge;
            WaveManager.Instance.OnBossHpUpdated += UpdateBossGauge;
            WaveManager.Instance.OnAllWavesCleared -= ShowClearScreen;
        }
    }

    private void SetMode(bool isBoss)
    {
        isBossMode = isBoss;

        // 보스 웨이브 시작 시 이펙트 초기화 (필요시)
        if (activeVisualEffect != null) activeVisualEffect.SetProgress(0f);
        
        /* (구)게이지 로직
        if (isBossMode)
        {
            // 보스전 게이지 색상을 지정한 색으로 변경
            if (gaugeFillImage != null) gaugeFillImage.color = bossGauge;
        }
        else
        {
            // 일반 모드 게이지 색상을 지정한 색으로 변경
            if (gaugeFillImage != null) gaugeFillImage.color = normalGauge;
        }
        */
    }

    private void UpdateWaveInfo(int currentWave, int maxWaves)
    {
        // 보스 웨이브일 경우 텍스트를 "BOSS WAVE" 등으로 변경 가능
        waveText.text = isBossMode ? $"BOSS" : $"WAVE {currentWave} / {maxWaves}";
    }

    // 일반 모드 진행도 갱신
    private void UpdateEnemyGauge(int remaining, int total)
    {
        if (isBossMode) return; // 보스전이면 무시

        enemyCountText.text = $"{remaining} / {total}";

        /* (구)게이지 로직
        progressGauge.value = (float)currentGauge / total;

        if (remaining == total) DrawTicks(total);
        */ 
        
        // 진행도 계산: 0 (시작) -> 1 (모두 처치)
        float progress = 1f - ((float)remaining / total);

        // 연출 클래스에 진행도 전달 (나머지 애니메이션 처리는 저쪽에서 알아서 함)
        if (activeVisualEffect != null)
        {
            activeVisualEffect.SetProgress(progress);
        }

    }

    // 보스 HP 모드 진행도 갱신
    private void UpdateBossGauge(float currentHp, float maxHp)
    {
        if (!isBossMode) return;

        // 체력이 음수나 소수로 표기 되는 것을 방지 (0 / Max)
        float displayHp = Mathf.Max(0, currentHp);
        
        // 텍스트를 체력으로 표시 (예: 1500 / 2000)
        enemyCountText.text = $"{displayHp:F0} / {maxHp:F0}";
        
        /* (구)게이지 슬라이더 연동
        progressGauge.value = displayHp / bossHp;

        if (displayHp == bossHp) DrawTicks(bossHp);
        */ 
        
        // 진행도 계산: 0 (체력 꽉참) -> 1 (체력 0)
        float progress = 1f - (displayHp / maxHp);

        if (activeVisualEffect != null)
        {
            activeVisualEffect.SetProgress(progress);
        }
    }

    /* (구) 눈금 생성 로직
    private void DrawTicks(int totalTicks)
    {
        // 기존 눈금 삭제
        foreach (Transform child in tickContainer)
        {
            Destroy(child.gameObject);
        }

        // 적이 1마리보다 많으면, 총 적의 수에 맞게 등분하여 눈금(선) 생성
        if (totalTicks > 1 || isBossMode)
        {
            for (int i = 1; i < totalTicks; i++)
            {
                GameObject tick = Instantiate(tickPrefab, tickContainer);
                RectTransform rect = tick.GetComponent<RectTransform>();

                // 0~1 사이의 비율 위치 계산
                float ratio = (float)i / totalTicks;

                // 비율에 따라 부모 기준의 앵커(위치) 설정
                rect.anchorMin = new Vector2(ratio, 0);
                rect.anchorMax = new Vector2(ratio, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);
                
                // 여백 0으로 초기화 및 눈금 두께(tickWidth) 고정
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.sizeDelta = new Vector2(tickWidth, rect.sizeDelta.y);
            }
        }
        else return;
    }
    */
    
    private void ShowClearScreen()
    {
        gameClearPanel.SetActive(true);
    }
}