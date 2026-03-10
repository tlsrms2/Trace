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

    [Header("Wave Settings")]
    [SerializeField] private WaveData[] waves;
    private int currentWaveIndex = 0;
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
            float waveEndTime = Time.time + currentWave.waveDuration;

            foreach (var enemy in currentWave.enemies)
            {
                StartCoroutine(SpawnEnemy(enemy));
            }

            while (Time.time < waveEndTime)
            {
                yield return null;
            }

            currentWaveIndex++;
        }
    }

    private IEnumerator SpawnEnemy(enemyData enemy)
    {
        while (true)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
            {
                enemySpawner.SpawnEnemy(enemy.enemyPrefab);
            }
            yield return new WaitForSeconds(enemy.spawnInterval);
        }
    }
}