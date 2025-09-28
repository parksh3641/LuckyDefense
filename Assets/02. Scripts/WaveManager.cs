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
        [SerializeField] private float waveInterval = 30f;
        [SerializeField] private bool autoStartNextWave = true;
        
        private CSVLoadManager csvManager;
        private EnemySpawner enemySpawner;
        private int currentWaveIndex = 0;
        private bool isWaveActive = false;
        private float waveTimer = 0f;
        private bool gameStarted = false;

        public int CurrentWaveIndex => currentWaveIndex;
        public bool IsWaveActive => isWaveActive;
        public float WaveTimer => waveTimer;
        public bool GameStarted => gameStarted;

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
                if (enemySpawner.GetActiveEnemyCount() == 0)
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
            
            var waveDataList = csvManager.GetWaveDataByWaveIndex(currentArenaType, currentWaveIndex);
            
            if (waveDataList.Count == 0)
            {
                Debug.Log($"CSV에 웨이브 {currentWaveIndex} 데이터가 없어 기본 데이터로 생성합니다.");
            }

            isWaveActive = true;
            waveTimer = waveInterval;

            if (enemySpawner != null)
            {
                enemySpawner.SetArenaType(currentArenaType);
                enemySpawner.SetWaveIndex(currentWaveIndex);
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

        public void SetArenaType(ArenaType arenaType)
        {
            currentArenaType = arenaType;
        }

        public void SetWaveInterval(float interval)
        {
            waveInterval = Mathf.Max(5f, interval);
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
            return $"Wave {currentWaveIndex}";
        }
    }
}