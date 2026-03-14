using System; // Action ?�용???�해 추�?
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // --- ?��???---
    private static WaveManager instance = null;
    public static WaveManager Instance
    {
        get
        {
            if (null == instance) return null;
            return instance;
        }
    }

    void Awake()
    {
        if (null == instance)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        enemySpawner = GetComponent<EnemySpawner>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += StopSpawning;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= StopSpawning;
    }

    // --- UI?� ?�신???�벤???�언 ---
    // (?�재 ?�이�?번호, ?�체 ?�이�?개수)
    public event Action<int, int> OnWaveUpdated; 
    
    // (?��? ??개수, ?�재 ?�이브의 �???개수)
    public event Action<int, int> OnEnemyProgressUpdated; 

    // --- 보스???�벤??추�? ---
    public event Action<bool> OnWaveModeChanged;       // true�?보스 모드, false�??�반 모드
    public event Action<float, float> OnBossHpUpdated; // 보스 ?�재 체력, 최�? 체력
    // -------------------------

    public event Action<Action> OnWaveTransitionStarted; // ?�출 ?�료 콜백??받을 ?�벤??

    // --- 변??---
    [SerializeField] private WaveData[] waves; // ?�스?�터?�서 ScriptableObject ?�이???�당
    private int currentWaveIndex = 0;
    private int enemiesRemainingToSpawn = 0;
    private int enemiesRemainingAlive = 0;
    private int totalEnemiesInCurrentWave = 0; // ?�재 ?�이�?�?????기록??
    private EnemySpawner enemySpawner;
    private bool isSpawningStopped = false;

    void Start()
    {
        StartCoroutine(SpawnWaves());
        AudioManager.Instance.PlayIngameBgm();
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Length)
        {
            WaveData currentWave = waves[currentWaveIndex];
            
            // 1. UI 갱신: ?�이브�? ?�작?????�출 (?�덱?�는 0부?�이므�?+1)
            OnWaveUpdated?.Invoke(currentWaveIndex + 1, waves.Length);

            if (currentWave.isBossWave)
            {
                // UI�?보스 모드�??�환
                OnWaveModeChanged?.Invoke(true); 
                
                totalEnemiesInCurrentWave = 1;
                enemiesRemainingToSpawn = 0;
                enemiesRemainingAlive = 1;

                // 보스 ?�환
                GameObject bossObj = Instantiate(currentWave.enemies[0].enemyPrefab, Vector3.zero, Quaternion.identity);
                Enemy bossEnemy = bossObj.GetComponent<Enemy>();

                if (bossEnemy != null)
                {
                    // 보스가 ?��?지�??�을 ?�마??UI�??�달?�도�??�결
                    bossEnemy.OnHpChanged += (currentHp, maxHp) => 
                    {
                        OnBossHpUpdated?.Invoke(currentHp, maxHp);
                    };

                    // ?�폰 직후 �?�?체력??UI???�송
                    OnBossHpUpdated?.Invoke(bossEnemy.MaxHp, bossEnemy.MaxHp);
                }
            }
            else 
            {
                totalEnemiesInCurrentWave = 0;
                foreach (var enemy in currentWave.enemies)
                {
                    totalEnemiesInCurrentWave += enemy.enemyCount;
                }
                enemiesRemainingToSpawn = totalEnemiesInCurrentWave;
                enemiesRemainingAlive = 0;
                
                // ?�이�??�작 ??가??�?게이지 갱신
                OnEnemyProgressUpdated?.Invoke(totalEnemiesInCurrentWave, totalEnemiesInCurrentWave);

                foreach (var enemy in currentWave.enemies)
                {
                    StartCoroutine(SpawnEnemy(enemy));
                }
            }

            // 2. ?�재 ?�이브의 ?�이 모두 죽을 ?�까지 ?��?
            while (enemiesRemainingToSpawn > 0 || enemiesRemainingAlive > 0)
            {
                yield return null;
            }
            // 3. ?�이�??�리???�출 ?�작???�림 (진행 ?��?�?체크??변???�성)
            bool isTransitionFinished = false;

            // UI 쪽에 ?�출??지?�하�? ?�출???�나�?isTransitionFinished�?true�?바꾸?�는 콜백 ?�수�??�겨�?
            OnWaveTransitionStarted?.Invoke(() => isTransitionFinished = true);

            // UI ?�출???�전???�날 ?�까지 ?�음 ?�이브로 ?�어가지 ?�고 ?��?
            while (!isTransitionFinished)
            {
                yield return null;
            }

            currentWaveIndex++;
        }

        // 4. 모든 ?�이�?종료 ??게임 ?�리??처리
        GameManager.Instance.GameClear(); // 게임 ?�태 변�?(?�?�머 ?��? ??
    }

    private IEnumerator SpawnEnemy(enemyData enemy)
    {
        float timer = 0f;
        while(timer < enemy.spawnStartTime)
        {
            if (isSpawningStopped)
            {
                yield return null;
                continue;
            }

            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
        
        int leftSpawnCnt = enemy.enemyCount;
        while (leftSpawnCnt > 0)
        {
            if (isSpawningStopped)
            {
                yield return null;
                continue;
            }

            if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
            {
                enemySpawner.SpawnEnemy(enemy.enemyPrefab);
                enemiesRemainingToSpawn--;
                enemiesRemainingAlive++;
                leftSpawnCnt--;
            }

            float wait = 0f;
            while (wait < enemy.spawnInterval)
            {
                if (!isSpawningStopped)
                    wait += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ?�이 죽었?????�출?�는 ?�수 (???�크립트?�서 ???�수�??�출?�야 ??
    public void OnEnemyKilled()
    {
        enemiesRemainingAlive--;
        
        // ?�전 ?�치: ?�수 방�? (중복 ?�출 ?��?
        if (enemiesRemainingAlive < 0)
        {
            Debug.LogWarning("[WaveManager] enemiesRemainingAlive went negative! Clamping to 0.");
            enemiesRemainingAlive = 0;
        }

        // ?��? ?�체 ??= ?�직 ?�폰 ??????+ ?�재 맵에 ?�아?�는 ??
        int totalRemaining = enemiesRemainingToSpawn + enemiesRemainingAlive;
        
        // 4. UI 갱신: ?�이 죽을 ?�마??게이지 ?�데?�트
        OnEnemyProgressUpdated?.Invoke(totalRemaining, totalEnemiesInCurrentWave);
    }

    public void StopSpawning()
    {
        isSpawningStopped = true;
    }

    public void ResumeSpawning()
    {
        isSpawningStopped = false;
    }
}
