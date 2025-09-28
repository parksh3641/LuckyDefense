using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuckyDefense
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave Settings")]
        [SerializeField] private ArenaType currentArenaType = ArenaType.Normal;
        [SerializeField] private bool autoStartNextWave = true;
        
        private CSVLoadManager csvManager;
        private EnemySpawner enemySpawner;
        private int currentWaveIndex = 0;
        private bool isWaveActive = false;
        private float waveTimer = 0f;
        private bool gameStarted = false;
        private WaveData currentWaveData;

        public int CurrentWaveIndex => currentWaveIndex;
        public bool IsWaveActive => isWaveActive;
        public float WaveTimer => waveTimer;
        public bool GameStarted => gameStarted;
        public bool IsCurrentWaveBoss => currentWaveData != null && currentWaveData.IsBoss;
        public int CurrentWaveTime => currentWaveData != null ? currentWaveData.Wave_Time : 30;
        public int CurrentMonsterID => currentWaveData != null ? currentWaveData.Monster_ID : 1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            csvManager = CSVLoadManager.Instance;
            enemySpawner = EnemySpawner.Instance;
        }

        private void Update()
        {
            if (!gameStarted || !isWaveActive) return;

            waveTimer -= Time.deltaTime;

            if (waveTimer <= 0f)
            {
                if (enemySpawner != null && enemySpawner.GetActiveEnemyCount() == 0)
                {
                    if (autoStartNextWave)
                    {
                        StartNextWave();
                    }
                    else
                    {
                        isWaveActive = false;
                    }
                }
                else
                {
                    waveTimer = 5f;
                }
            }
        }

        public void StartGame()
        {
            gameStarted = true;
            currentWaveIndex = 0;
            StartNextWave();
        }

        public void StartNextWave()
        {
            currentWaveIndex++;
            
            currentWaveData = csvManager.GetWaveDataByWaveIndex(currentArenaType, currentWaveIndex);
            
            if (currentWaveData == null)
            {
                Debug.Log($"웨이브 {currentWaveIndex} 데이터가 없습니다. 게임 종료.");
                EndGame();
                return;
            }

            isWaveActive = true;
            waveTimer = currentWaveData.Wave_Time;

            Debug.Log($"웨이브 {currentWaveIndex} 시작 - 시간: {currentWaveData.Wave_Time}초, " +
                     $"몬스터: {currentWaveData.Monster_ID}, 보스: {(currentWaveData.IsBoss ? "예" : "아니오")}");

            if (enemySpawner != null)
            {
                enemySpawner.SetArenaType(currentArenaType);
                enemySpawner.SetWaveIndex(currentWaveIndex);
                enemySpawner.SetWaveData(currentWaveData);
                enemySpawner.StartWave();
            }
        }

        public void ForceNextWave()
        {
            if (!gameStarted) return;

            if (enemySpawner != null)
            {
                enemySpawner.ClearAllEnemies();
            }
            
            StartNextWave();
        }

        public void StopWave()
        {
            isWaveActive = false;
            waveTimer = 0f;
            
            if (enemySpawner != null)
            {
                enemySpawner.StopWave();
            }
        }

        public void EndGame()
        {
            gameStarted = false;
            isWaveActive = false;
            waveTimer = 0f;
            
            if (enemySpawner != null)
            {
                enemySpawner.StopWave();
            }
            
            Debug.Log("게임 종료");
        }

        public void SetArenaType(ArenaType arenaType)
        {
            currentArenaType = arenaType;
        }

        public void SetAutoStart(bool autoStart)
        {
            autoStartNextWave = autoStart;
        }

        public string GetWaveTimeString()
        {
            if (!isWaveActive) return "00:00";
            
            int minutes = Mathf.FloorToInt(waveTimer / 60f);
            int seconds = Mathf.FloorToInt(waveTimer % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        public string GetWaveString()
        {
            if (!gameStarted) return "Wave -";
            string bossText = IsCurrentWaveBoss ? " (BOSS)" : "";
            return $"Wave {currentWaveIndex}{bossText}";
        }

        public int GetMaxWaveCount()
        {
            return csvManager.GetMaxWaveIndex(currentArenaType);
        }

        public bool IsGameComplete()
        {
            return currentWaveIndex >= GetMaxWaveCount();
        }

        public WaveData GetCurrentWaveData()
        {
            return currentWaveData;
        }

        public void RestartGame()
        {
            currentWaveIndex = 0;
            currentWaveData = null;
            isWaveActive = false;
            waveTimer = 0f;
            gameStarted = false;
            
            if (enemySpawner != null)
            {
                enemySpawner.ClearAllEnemies();
            }
        }
    }
}