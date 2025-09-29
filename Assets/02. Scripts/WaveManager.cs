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
        
        [Header("Scaling Settings")]
        [SerializeField] private float healthIncreasePerWave = 0.5f;
        
        [Header("Enemy Limit Settings")]
        [SerializeField] private float enemyCheckInterval = 0.5f;
        
        private CSVLoadManager csvManager;
        private EnemySpawner enemySpawner;
        private int currentWaveIndex = 0;
        private bool isWaveActive = false;
        private float waveTimer = 0f;
        private bool gameStarted = false;
        private WaveData currentWaveData = new WaveData();
        private float enemyCheckTimer = 0f;

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
            
            csvManager = CSVLoadManager.Instance;
            enemySpawner = EnemySpawner.Instance;
        }

        private void Update()
        {
            if (!gameStarted || !isWaveActive) return;

            waveTimer -= Time.deltaTime;
            enemyCheckTimer += Time.deltaTime;

            if (enemyCheckTimer >= enemyCheckInterval)
            {
                enemyCheckTimer = 0f;
                CheckEnemyLimit();
            }

            if (waveTimer <= 0f)
            {
                if (IsCurrentWaveBoss && IsBossStillAlive())
                {
                    TriggerGameOver();
                    return;
                }

                if (autoStartNextWave)
                {
                    StartNextWave();
                }
                else
                {
                    isWaveActive = false;
                }
            }
        }

        private void CheckEnemyLimit()
        {
            if (UIManager.Instance != null && UIManager.Instance.IsEnemyCountAtMax())
            {
                Debug.Log("적 수가 최대치를 초과했습니다. 게임 오버!");
                TriggerGameOver();
            }
        }

        private bool IsBossStillAlive()
        {
            GameObject[] bosses = GameObject.FindGameObjectsWithTag("Boss");
            return bosses != null && bosses.Length > 0;
        }

        private void TriggerGameOver()
        {
            isWaveActive = false;
            gameStarted = false;
    
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.StopWave();
            }
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverUI();
            }
        }

        public void StartGame()
        {
            gameStarted = true;
            currentWaveIndex = 0;
            enemyCheckTimer = 0f;
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
                     $"몬스터: {currentWaveData.Monster_ID}, 보스: {(currentWaveData.IsBoss ? "예" : "아니오")}, " +
                     $"체력 배율: {GetHealthMultiplier():F2}x");

            if (UIManager.Instance != null)
                UIManager.Instance.ShowWaveUI(currentWaveIndex);

            if (enemySpawner != null)
            {
                enemySpawner.SetArenaType(currentArenaType);
                enemySpawner.SetWaveIndex(currentWaveIndex);
                enemySpawner.SetWaveData(currentWaveData);
                enemySpawner.StartWave();
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

        public float GetHealthMultiplier()
        {
            return 1.0f + (healthIncreasePerWave * (currentWaveIndex - 1));
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
        
        public bool IsLastWave()
        {
            int maxWave = csvManager.GetMaxWaveIndex(currentArenaType);
            return currentWaveIndex >= maxWave;
        }
    }
}