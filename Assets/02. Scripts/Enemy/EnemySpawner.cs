using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace LuckyDefense
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Enemy Settings")] 
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] waypointPath1;
        [SerializeField] private Transform[] waypointPath2;
        [SerializeField] private Transform enemyParent;
        [SerializeField] private int poolSize = 100;
        
        [Header("Boss & MiniBoss Settings")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject miniBossPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private float pathDelay = 0.1f;

        [Header("Wave Settings")] 
        [SerializeField] private ArenaType currentArena = ArenaType.Normal;

        [Header("Damage Text")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private Transform damageTextParent;
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = Color.red;
        [SerializeField] private float criticalSizeMultiplier = 1.5f;

        private List<GameObject> enemyPool = new List<GameObject>();
        private List<GameObject> bossPool = new List<GameObject>();
        private List<GameObject> miniBossPool = new List<GameObject>();
        private List<Enemy> activeEnemies = new List<Enemy>();
        private List<GameObject> damageTextPool = new List<GameObject>();
        private CSVLoadManager csvManager;
        private Coroutine currentWaveCoroutine;
        private Vector3[] path1Positions;
        private Vector3[] path2Positions;
        private int damageTextPoolIndex = 0;
        private int miniBossSpawnCount = 0;
        private int bossSpawnCount = 0;

        private int currentWaveIndex = 1;
        private bool isWaveActive = false;
        private WaveData currentWaveData;

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
                return;
            }
            
            csvManager = CSVLoadManager.Instance;
        }

        private void Start()
        {
            InitializeEnemyPool();
            InitializeDamageTextPool();
            CacheWaypointPositions();
        }

        private void InitializeEnemyPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject enemy = Instantiate(enemyPrefab);
                enemy.transform.SetParent(enemyParent);
                enemy.name = $"Enemy_{i}";
                enemy.SetActive(false);
                enemyPool.Add(enemy);
            }

            if (bossPrefab != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject boss = Instantiate(bossPrefab);
                    boss.transform.SetParent(enemyParent);
                    boss.name = $"Boss_{i}";
                    boss.SetActive(false);
                    bossPool.Add(boss);
                }
            }

            if (miniBossPrefab != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject miniBoss = Instantiate(miniBossPrefab);
                    miniBoss.transform.SetParent(enemyParent);
                    miniBoss.name = $"MiniBoss_{i}";
                    miniBoss.SetActive(false);
                    miniBossPool.Add(miniBoss);
                }
            }
        }

        private void InitializeDamageTextPool()
        {
            if (damageTextPrefab == null || damageTextParent == null) 
            {
                Debug.LogWarning("DamageText 프리팹 또는 부모 오브젝트가 설정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject damageText = Instantiate(damageTextPrefab);
                damageText.transform.SetParent(damageTextParent);
                damageText.name = $"DamageText_{i}";
                damageText.SetActive(false);
                damageTextPool.Add(damageText);
            }

            Debug.Log($"데미지 텍스트 풀 {poolSize}개 초기화 완료");
        }

        private void CacheWaypointPositions()
        {
            if (waypointPath1 != null && waypointPath1.Length > 0)
            {
                path1Positions = new Vector3[waypointPath1.Length];
                for (int i = 0; i < waypointPath1.Length; i++)
                {
                    path1Positions[i] = waypointPath1[i].position;
                }
            }

            if (waypointPath2 != null && waypointPath2.Length > 0)
            {
                path2Positions = new Vector3[waypointPath2.Length];
                for (int i = 0; i < waypointPath2.Length; i++)
                {
                    path2Positions[i] = waypointPath2[i].position;
                }
            }
        }

        public void StartWave()
        {
            if (isWaveActive) return;

            Debug.Log($"StartWave 호출됨 - Arena: {currentArena}, WaveIndex: {currentWaveIndex}");
            
            currentWaveData = csvManager.GetWaveDataByWaveIndex(currentArena, currentWaveIndex);
    
            if (currentWaveData == null)
            {
                Debug.Log($"웨이브 데이터가 없습니다. Arena: {currentArena}, WaveIndex: {currentWaveIndex}");
                return;
            }

            Debug.Log($"웨이브 {currentWaveIndex} 시작 - 몬스터 ID: {currentWaveData.Monster_ID}, 보스: {currentWaveData.IsBoss}");

            isWaveActive = true;
            currentWaveCoroutine = StartCoroutine(ProcessWave());
        }

        public void StopWave()
        {
            if (currentWaveCoroutine != null)
            {
                StopCoroutine(currentWaveCoroutine);
                currentWaveCoroutine = null;
            }

            isWaveActive = false;
        }

        private IEnumerator ProcessWave()
        {
            if (currentWaveData.IsBoss)
            {
                Vector3 spawnPosition1 = waypointPath1[0].position;
                Vector3 spawnPosition2 = waypointPath2[0].position;

                yield return new WaitForSeconds(pathDelay);
                SpawnBoss(spawnPosition1, path1Positions);
                SpawnBoss(spawnPosition2, path2Positions);
        
                isWaveActive = false;
                
                Debug.Log("보스 소환");
                yield break;
            }

            float waveDuration = currentWaveData.Wave_Time;
            float endTime = Time.time + waveDuration;
            bool spawnPath1 = true;
            float lastSpawnTime = Time.time;

            while (Time.time < endTime)
            {
                if (Time.time >= lastSpawnTime + spawnInterval)
                {
                    Vector3[] targetPath = spawnPath1 ? path1Positions : path2Positions;
                    Vector3 spawnPosition = spawnPath1 ? waypointPath1[0].position : waypointPath2[0].position;

                    SpawnEnemy(currentWaveData.Monster_ID, spawnPosition, targetPath);

                    lastSpawnTime = Time.time;
                    spawnPath1 = !spawnPath1;

                    yield return new WaitForSeconds(pathDelay);
                }
                else
                {
                    yield return null;
                }
            }

            isWaveActive = false;
        }

        private void SpawnEnemy(int monsterId, Vector3 spawnPosition, Vector3[] waypoints)
        {
            GameObject enemyObj = GetPooledEnemy();
            if (enemyObj == null) return;

            enemyObj.transform.position = spawnPosition;
            enemyObj.SetActive(true);

            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(waypoints, this);
                SetupEnemyStats(enemy, monsterId, false);
                activeEnemies.Add(enemy);
            }
        }

        private void SpawnBoss(Vector3 spawnPosition, Vector3[] waypoints)
        {
            GameObject bossObj = GetPooledBoss();
            if (bossObj == null)
            {
                bossObj = GetPooledEnemy();
            }
            if (bossObj == null) return;

            bossObj.transform.position = spawnPosition;
            bossObj.SetActive(true);

            Enemy boss = bossObj.GetComponent<Enemy>();
            if (boss != null)
            {
                boss.Initialize(waypoints, this);
                SetupEnemyStats(boss, currentWaveData.Monster_ID, true);
                activeEnemies.Add(boss);
            }
        }

        private GameObject GetPooledEnemy()
        {
            foreach (GameObject enemy in enemyPool)
            {
                if (!enemy.activeInHierarchy)
                {
                    return enemy;
                }
            }
            return null;
        }

        private GameObject GetPooledBoss()
        {
            foreach (GameObject boss in bossPool)
            {
                if (!boss.activeInHierarchy)
                {
                    return boss;
                }
            }
            return null;
        }

        private GameObject GetPooledDamageText()
        {
            if (damageTextPool.Count == 0) return null;

            if (damageTextPoolIndex >= damageTextPool.Count)
                damageTextPoolIndex = 0;

            GameObject damageText = damageTextPool[damageTextPoolIndex];
            damageTextPoolIndex++;
            
            return damageText;
        }

        private void SetupEnemyStats(Enemy enemy, int monsterId, bool isBoss)
        {
            if (isBoss)
            {
                BossData bossData = csvManager.GetBossData(currentWaveData.Boss_ID);
                if (bossData != null)
                {
                    enemy.SetStats(
                        bossData.HP,
                        bossData.Move_Speed,
                        bossData.ATK,
                        bossData.Gold
                    );
                }
                else
                {
                    int waveMultiplier = currentWaveIndex;
                    enemy.SetStats(
                        500 * waveMultiplier,
                        1.5f,
                        50 * waveMultiplier,
                        100 * waveMultiplier
                    );
                }
            }
            else
            {
                MonsterData monsterData = csvManager.GetMonsterData(monsterId);
                if (monsterData != null)
                {
                    int waveMultiplier = currentWaveIndex;
                    enemy.SetStats(
                        monsterData.HP * waveMultiplier,
                        monsterData.Move_Speed,
                        monsterData.ATK * waveMultiplier,
                        monsterData.Gold * waveMultiplier
                    );
                }
                else
                {
                    int waveMultiplier = currentWaveIndex;
                    enemy.SetStats(
                        100 * waveMultiplier,
                        2f,
                        10 * waveMultiplier,
                        10 * waveMultiplier
                    );
                }
            }
        }

        public void ShowDamageText(Vector3 enemyPosition, int damage, bool isCriticalHit = false)
        {
            GameObject damageTextObj = GetPooledDamageText();
            if (damageTextObj == null) return;

            Vector3 worldPosition = enemyPosition + Vector3.up * 1f;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            
            damageTextObj.SetActive(true);

            EnemyDamageText damageText = damageTextObj.GetComponent<EnemyDamageText>();
            if (damageText != null)
            {
                damageText.SetDamageColor(isCriticalHit ? criticalDamageColor : normalDamageColor);
                damageText.SetFontSize(isCriticalHit ? 35 * criticalSizeMultiplier : 35);
                damageText.Setup(damage, screenPosition);
            }
        }

        public void DestroyEnemy(Enemy enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);
            enemy.gameObject.SetActive(false);
        }

        public void SetArenaType(ArenaType arenaType)
        {
            currentArena = arenaType;
        }

        public void SetWaveIndex(int waveIndex)
        {
            currentWaveIndex = waveIndex;
        }

        public void SetWaveData(WaveData waveData)
        {
            currentWaveData = waveData;
        }

        public int GetActiveEnemyCount()
        {
            return activeEnemies.Count;
        }

        public void ClearAllEnemies()
        {
            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            activeEnemies.Clear();
        }
        
        private void OnDestroy()
        {
            StopWave();
        }
        
        private GameObject GetPooledMiniBoss()
        {
            foreach (GameObject miniBoss in miniBossPool)
            {
                if (!miniBoss.activeInHierarchy)
                {
                    return miniBoss;
                }
            }
            return null;
        }
        
        public void SpawnMiniBoss()
        {
            Vector3 spawnPosition1 = waypointPath1[0].position;
            Vector3 spawnPosition2 = waypointPath2[0].position;

            GameObject miniBoss1 = GetPooledMiniBoss();
            if (miniBoss1 != null)
            {
                miniBoss1.transform.position = spawnPosition1;
                miniBoss1.SetActive(true);

                Enemy enemy1 = miniBoss1.GetComponent<Enemy>();
                if (enemy1 != null)
                {
                    enemy1.Initialize(path1Positions, this);
                    SetupMiniBossStats(enemy1);
                    activeEnemies.Add(enemy1);
                }
            }

            GameObject miniBoss2 = GetPooledMiniBoss();
            if (miniBoss2 != null)
            {
                miniBoss2.transform.position = spawnPosition2;
                miniBoss2.SetActive(true);

                Enemy enemy2 = miniBoss2.GetComponent<Enemy>();
                if (enemy2 != null)
                {
                    enemy2.Initialize(path2Positions, this);
                    SetupMiniBossStats(enemy2);
                    activeEnemies.Add(enemy2);
                }
            }

            miniBossSpawnCount++;
        }

        private void SetupMiniBossStats(Enemy enemy)
        {
            BossData miniBossData = csvManager.GetBossData(1);
            if (miniBossData != null)
            {
                float hpMultiplier = 1f + (miniBossSpawnCount * 0.5f);
        
                enemy.SetStats(
                    Mathf.RoundToInt(miniBossData.HP * hpMultiplier),
                    miniBossData.Move_Speed,
                    miniBossData.ATK,
                    miniBossData.Gold
                );
            }
            else
            {
                float hpMultiplier = 1f + (miniBossSpawnCount * 0.5f);
                int waveMultiplier = currentWaveIndex;
        
                enemy.SetStats(
                    Mathf.RoundToInt(300 * waveMultiplier * hpMultiplier),
                    1.5f,
                    30 * waveMultiplier,
                    80 * waveMultiplier
                );
            }
        }

        public void ResetMiniBossSpawnCount()
        {
            miniBossSpawnCount = 0;
        }

        public void ResetBossSpawnCount()
        {
            bossSpawnCount = 0;
        }

        public void ResetAllSpawnCounts()
        {
            miniBossSpawnCount = 0;
            bossSpawnCount = 0;
        }
    }
}