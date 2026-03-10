using System;
using UnityEngine;

[Serializable]
public struct enemyData
{
    public GameObject enemyPrefab;
    public float spawnInterval;
}

[CreateAssetMenu(fileName = "New Wave Data", menuName = "ScriptableObjects/WaveData", order = 1)]
public class WaveData : ScriptableObject
{
    public enemyData[] enemies;
    public int enemyCount;
    public bool isBossWave;
}
