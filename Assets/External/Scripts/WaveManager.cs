using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private static WaveManager instance = null;

    void Awake()
    {
        if (null == instance)
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        enemySpawner = GetComponent<EnemySpawner>();
    }

    public static WaveManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    [SerializeField] private WaveData[] waves;
    private int currentWaveIndex = 0;
    private int enemiesRemainingToSpawn;
    private int enemiesRemainingAlive;
    private EnemySpawner enemySpawner;

    void Start()
    {
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Length)
        {
            WaveData currentWave = waves[currentWaveIndex];

            enemiesRemainingToSpawn = currentWave.enemyCount;

            foreach (var enemy in currentWave.enemies)
            {
                StartCoroutine(SpawnEnemy(enemy));
            }

            while (enemiesRemainingToSpawn > 0 || enemiesRemainingAlive > 0)
            {
                Debug.Log($"Waiting for wave {currentWaveIndex} to finish. Remaining to spawn: {enemiesRemainingToSpawn}, Remaining alive: {enemiesRemainingAlive}");
                yield return null;
            }
            
            currentWaveIndex++;
        }
    }

    private IEnumerator SpawnEnemy(enemyData enemy)
    {
        while (enemiesRemainingToSpawn > 0)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
            {
                enemySpawner.SpawnEnemy(enemy.enemyPrefab);
                enemiesRemainingToSpawn--;
                enemiesRemainingAlive++;
            }

            yield return new WaitForSeconds(enemy.spawnInterval);
        }
    }

    public void OnEnemyKilled()
    {
        enemiesRemainingAlive--;
    }
}