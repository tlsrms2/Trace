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
    public float waveDuration;
    public float waveStartTime;
    public enemyData[] enemies;
}
