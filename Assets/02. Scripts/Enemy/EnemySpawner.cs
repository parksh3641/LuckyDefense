using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuckyDefense
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Enemy Settings")] [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField] private Transform[] waypointPath1;
        [SerializeField] private Transform[] waypointPath2;
        [SerializeField] private Transform enemyParent;
        [SerializeField] private int poolSize = 100;

        [Header("Wave Settings")] [SerializeField]
        private WaveManager waveManager;

        [SerializeField] private ArenaType currentArena = ArenaType.Normal;

        private List<GameObject> enemyPool = new List<GameObject>();
        private List<Enemy> activeEnemies = new List<Enemy>();
        private CSVLoadManager csvManager;
        private Coroutine currentWaveCoroutine;
        private Vector3[] path1Positions;
        private Vector3[] path2Positions;

        private int currentWaveIndex = 1;
        private bool isWaveActive = false;

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
        }

        private void Start()
        {
            csvManager = CSVLoadManager.Instance;
            InitializeEnemyPool();
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
    
            var waveData = csvManager.GetWaveDataByWaveIndex(currentArena, currentWaveIndex);
            Debug.Log($"가져온 웨이브 데이터 개수: {waveData.Count}");
    
            if (waveData.Count == 0)
            {
                Debug.Log($"웨이브 데이터가 없습니다. Arena: {currentArena}, WaveIndex: {currentWaveIndex}");
                return;
            }

            // 유효한 몬스터 데이터 확인
            var validMonsters = waveData.Where(w => w.IsValidMonster).ToList();
            Debug.Log($"유효한 몬스터 데이터 개수: {validMonsters.Count}");

            isWaveActive = true;
            currentWaveCoroutine = StartCoroutine(ProcessWave(waveData));
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

        private IEnumerator ProcessWave(List<WaveData> waveDataList)
        {
            foreach (var waveData in waveDataList)
            {
                //if (!waveData.IsValidMonster) continue;

                float startDelay = waveData.Start_Time / 1000f;
                float spawnDuration = (waveData.End_Time - waveData.Start_Time) / 1000f;
                float spawnDelay = waveData.Delay_Time / 1000f;

                if (startDelay > 0)
                {
                    yield return new WaitForSeconds(startDelay);
                }

                yield return StartCoroutine(SpawnMonsters(
                    waveData.Monster_ID,
                    spawnDuration,
                    spawnDelay,
                    waveData.HP_Value,
                    waveData.Money_Value
                ));
            }

            isWaveActive = false;
            currentWaveIndex++;
        }

        private IEnumerator SpawnMonsters(int monsterId, float duration, float spawnDelay, float hpValue, float moneyValue)
        {
            float endTime = Time.time + duration;
            bool spawnPath1 = true;
            float lastSpawnTime = Time.time;

            while (Time.time < endTime)
            {
                if (Time.time >= lastSpawnTime + 0.5f)
                {
                    Vector3[] targetPath = spawnPath1 ? path1Positions : path2Positions;
                    Vector3 spawnPosition = spawnPath1 ? waypointPath1[0].position : waypointPath2[0].position;

                    SpawnEnemy(monsterId, spawnPosition, targetPath, hpValue, moneyValue);

                    lastSpawnTime = Time.time;
                    spawnPath1 = !spawnPath1;

                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void SpawnEnemy(int monsterId, Vector3 spawnPosition, Vector3[] waypoints, float hpValue,
            float moneyValue)
        {
            GameObject enemyObj = GetPooledEnemy();
            if (enemyObj == null) return;

            enemyObj.transform.position = spawnPosition;
            enemyObj.SetActive(true);

            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize(waypoints, this);
                SetupEnemyStats(enemy, monsterId, hpValue, moneyValue);
                activeEnemies.Add(enemy);
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

        private void SetupEnemyStats(Enemy enemy, int monsterId, float hpValue, float moneyValue)
        {
            MonsterData monsterData = null;
    
            if (monsterId > 0)
            {
                monsterData = csvManager.GetMonsterData(monsterId);
            }
    
            if (monsterData != null)
            {
                float hpMultiplier = 1 + (hpValue / 100f);
                float goldMultiplier = 1 + (moneyValue / 100f);

                int hp = Mathf.RoundToInt(monsterData.HP * hpMultiplier);
                int gold = Mathf.RoundToInt(monsterData.Gold * goldMultiplier);

                enemy.SetStats(
                    hp,
                    monsterData.Move_Speed,
                    monsterData.ATK,
                    gold
                );
            }
            else
            {
                float hpMultiplier = 1 + (hpValue / 100f);
                float goldMultiplier = 1 + (moneyValue / 100f);

                int hp = Mathf.RoundToInt(100 * hpMultiplier);
                int gold = Mathf.RoundToInt(10 * goldMultiplier);

                enemy.SetStats(hp, 2f, 10, gold);
            }
        }

        public void DestroyEnemy(Enemy enemy, bool reachedEnd = false)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);

            if (reachedEnd)
            {
                GameManager.Instance.MyGold -= 5;
            }
            else
            {
                GameManager.Instance.MyGold += enemy.GoldReward;
            }

            enemy.gameObject.SetActive(false);
        }

        public void NextWave()
        {
            if (!isWaveActive)
            {
                StartWave();
            }
        }

        public void SetArenaType(ArenaType arenaType)
        {
            currentArena = arenaType;
            currentWaveIndex = 1;
        }

        public void SetWaveIndex(int waveIndex)
        {
            currentWaveIndex = waveIndex;
        }

        public List<Enemy> GetActiveEnemies()
        {
            return new List<Enemy>(activeEnemies);
        }

        public int GetActiveEnemyCount()
        {
            return activeEnemies.Count;
        }

        public Enemy GetNearestEnemy(Vector3 position)
        {
            Enemy nearest = null;
            float minDistance = float.MaxValue;

            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    float distance = Vector3.Distance(position, enemy.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        public List<Enemy> GetEnemiesInRange(Vector3 position, float range)
        {
            List<Enemy> enemiesInRange = new List<Enemy>();

            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    float distance = Vector3.Distance(position, enemy.transform.position);
                    if (distance <= range)
                    {
                        enemiesInRange.Add(enemy);
                    }
                }
            }

            return enemiesInRange;
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

        public bool IsWaveActive()
        {
            return isWaveActive;
        }

        public int GetCurrentWaveIndex()
        {
            return currentWaveIndex;
        }

        private void OnDestroy()
        {
            StopWave();
        }
    }
}