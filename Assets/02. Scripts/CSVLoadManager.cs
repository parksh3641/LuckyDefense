using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LuckyDefense
{
    [System.Serializable]
    public class WaveData
    {
        public int ID;
        public int Wave_Index;
        public int Arena;
        public int Wave_Time;
        public int Monster_ID;
        public int Boss_ID;
        
        public bool IsValidMonster => Monster_ID > 0;
        public bool IsBoss => Boss_ID > 0;
    }

    [System.Serializable]
    public class MonsterData
    {
        public int ID;
        public int Asset_ID;
        public int Type;
        public float Move_Speed;
        public int HP;
        public int Gold;
        public int ATK;
        public float Value_1;
        public float Value_2;
        public float Value_3;
        public float Value_4;
        public float Value_5;
        
        public bool IsValid => ID > 0;
    }

    [System.Serializable]
    public class BossData
    {
        public int ID;
        public int Asset_ID;
        public int Type;
        public int HP;
        public float Move_Speed;
        public int ATK;
        public int Gold;
        public int Value_1;
        public int Value_2;
        public int Value_3;
        public int Value_4;
        public float Value_5;
        
        public bool IsValid => ID > 0;
    }

    [System.Serializable]
    public class SummonCostData
    {
        public int ID;
        public int Summon_cost_Gold;
        
        public bool IsValid => ID > 0;
    }

    [System.Serializable]
    public class DefaultData
    {
        public int ID;
        public int First_Money;
        
        public bool IsValid => ID > 0;
    }

    [System.Serializable]
    public class TowerData
    {
        public int ID;
        public float Grade;
        public int Type;
        public int Target;
        public float Range;
        public int Atk;
        public float Interval;
        public int Cri_Rate;
        public int Cri_Damage;
        public float Value_1;
        public float Value_1_Desc;
        public float Value_1_Icon;
        public int Value_2;
        public float Value_2_Desc;
        public float Value_2_Icon;
        public int Value_3;
        public float Value_3_Desc;
        public float Value_3_Icon;
        public float Value_4;
        public float Value_4_Desc;
        public float Value_4_Icon;
        public float Value_5;
        public float Value_5_Desc;
        public float Value_5_Icon;
        public float Level_Effect_Group_ID;
        public float Evolve_Effect_Group_ID;
        public float Enchant_Effect_Group_ID;
        
        public bool IsValid => ID > 0;
    }

    public enum ArenaType
    {
        Normal = 1,
        Hard = 2,
        Hell = 3,
        Demon = 4
    }

    public class CSVLoadManager : MonoBehaviour
    {
        [SerializeField] private string waveCSVFileName = "Wave_Data";
        [SerializeField] private string monsterCSVFileName = "Monster_Data";
        [SerializeField] private string bossCSVFileName = "Boss_Data";
        [SerializeField] private string summonCostCSVFileName = "SummonCost_Data";
        [SerializeField] private string defaultCSVFileName = "Default_Data";
        [SerializeField] private string towerCSVFileName = "Tower_Data";
        
        private static CSVLoadManager instance;
        public static CSVLoadManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CSVLoadManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CSVLoadManager");
                        instance = go.AddComponent<CSVLoadManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private Dictionary<ArenaType, List<WaveData>> waveDataCache = new Dictionary<ArenaType, List<WaveData>>();
        private Dictionary<int, MonsterData> monsterDataCache = new Dictionary<int, MonsterData>();
        private Dictionary<int, BossData> bossDataCache = new Dictionary<int, BossData>();
        private Dictionary<int, SummonCostData> summonCostDataCache = new Dictionary<int, SummonCostData>();
        private Dictionary<int, DefaultData> defaultDataCache = new Dictionary<int, DefaultData>();
        private Dictionary<int, TowerData> towerDataCache = new Dictionary<int, TowerData>();
        
        private MonsterData[] allMonstersCache;
        private BossData[] allBossesCache;
        private TowerData[] allTowersCache;
        
        private bool isInitialized = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCSVData();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeCSVData()
        {
            try
            {
                LoadWaveDataFromCSV();
                LoadMonsterDataFromCSV();
                LoadBossDataFromCSV();
                LoadSummonCostDataFromCSV();
                LoadDefaultDataFromCSV();
                LoadTowerDataFromCSV();
                
                CacheArrays();
                
                isInitialized = true;
                
                Debug.Log($"CSV 로드 완료 - Wave: {waveDataCache.Count}개 아레나, Monster: {monsterDataCache.Count}개, Boss: {bossDataCache.Count}개, Tower: {towerDataCache.Count}개");
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 데이터 로드 실패: {e.Message}");
            }
        }

        private void CacheArrays()
        {
            allMonstersCache = monsterDataCache.Values.ToArray();
            allBossesCache = bossDataCache.Values.ToArray();
            allTowersCache = towerDataCache.Values.ToArray();
        }

        private void LoadWaveDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(waveCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"웨이브 CSV 파일을 찾을 수 없습니다: {waveCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("웨이브 CSV 파일이 비어있습니다.");
                return;
            }

            List<WaveData> allWaveData = new List<WaveData>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                WaveData waveData = ParseWaveCSVLine(line);
                if (waveData != null)
                {
                    allWaveData.Add(waveData);
                }
            }

            CacheWaveDataByArena(allWaveData);
        }

        private WaveData ParseWaveCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 6) return null;

            WaveData waveData = new WaveData();

            waveData.ID = ParseInt(values[0]);
            waveData.Wave_Index = ParseInt(values[1]);
            waveData.Arena = ParseInt(values[2]);
            waveData.Wave_Time = ParseInt(values[3]);
            waveData.Monster_ID = ParseInt(values[4]);
            waveData.Boss_ID = ParseInt(values[5]);

            return waveData;
        }

        private void LoadMonsterDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(monsterCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"몬스터 CSV 파일을 찾을 수 없습니다: {monsterCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("몬스터 CSV 파일이 비어있습니다.");
                return;
            }

            monsterDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                MonsterData monsterData = ParseMonsterCSVLine(line);
                if (monsterData != null && monsterData.IsValid)
                {
                    monsterDataCache[monsterData.ID] = monsterData;
                }
            }
        }

        private void LoadBossDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(bossCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"보스 CSV 파일을 찾을 수 없습니다: {bossCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("보스 CSV 파일이 비어있습니다.");
                return;
            }

            bossDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                BossData bossData = ParseBossCSVLine(line);
                if (bossData != null && bossData.IsValid)
                {
                    bossDataCache[bossData.ID] = bossData;
                }
            }
        }

        private void LoadSummonCostDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(summonCostCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"소환 비용 CSV 파일을 찾을 수 없습니다: {summonCostCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("소환 비용 CSV 파일이 비어있습니다.");
                return;
            }

            summonCostDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                SummonCostData summonCostData = ParseSummonCostCSVLine(line);
                if (summonCostData != null && summonCostData.IsValid)
                {
                    summonCostDataCache[summonCostData.ID] = summonCostData;
                }
            }
        }

        private void LoadDefaultDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(defaultCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"기본 설정 CSV 파일을 찾을 수 없습니다: {defaultCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("기본 설정 CSV 파일이 비어있습니다.");
                return;
            }

            defaultDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                DefaultData defaultData = ParseDefaultCSVLine(line);
                if (defaultData != null && defaultData.IsValid)
                {
                    defaultDataCache[defaultData.ID] = defaultData;
                }
            }
        }

        private void LoadTowerDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(towerCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"타워 CSV 파일을 찾을 수 없습니다: {towerCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("타워 CSV 파일이 비어있습니다.");
                return;
            }

            towerDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                TowerData towerData = ParseTowerCSVLine(line);
                if (towerData != null && towerData.IsValid)
                {
                    towerDataCache[towerData.ID] = towerData;
                }
            }
        }

        private SummonCostData ParseSummonCostCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 2) return null;

            SummonCostData summonCostData = new SummonCostData();

            summonCostData.ID = ParseInt(values[0]);
            summonCostData.Summon_cost_Gold = ParseInt(values[1]);

            return summonCostData;
        }

        private DefaultData ParseDefaultCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 2) return null;

            DefaultData defaultData = new DefaultData();

            defaultData.ID = ParseInt(values[0]);
            defaultData.First_Money = ParseInt(values[1]);

            return defaultData;
        }

        private TowerData ParseTowerCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 27) return null;

            TowerData towerData = new TowerData();

            towerData.ID = ParseInt(values[0]);
            towerData.Grade = ParseFloat(values[1]);
            towerData.Type = ParseInt(values[2]);
            towerData.Target = ParseInt(values[3]);
            towerData.Range = ParseFloat(values[4]);
            towerData.Atk = ParseInt(values[5]);
            towerData.Interval = ParseFloat(values[6]);
            towerData.Cri_Rate = ParseInt(values[7]);
            towerData.Cri_Damage = ParseInt(values[8]);
            towerData.Value_1 = ParseFloat(values[9]);
            towerData.Value_1_Desc = ParseFloat(values[10]);
            towerData.Value_1_Icon = ParseFloat(values[11]);
            towerData.Value_2 = ParseInt(values[12]);
            towerData.Value_2_Desc = ParseFloat(values[13]);
            towerData.Value_2_Icon = ParseFloat(values[14]);
            towerData.Value_3 = ParseInt(values[15]);
            towerData.Value_3_Desc = ParseFloat(values[16]);
            towerData.Value_3_Icon = ParseFloat(values[17]);
            towerData.Value_4 = ParseFloat(values[18]);
            towerData.Value_4_Desc = ParseFloat(values[19]);
            towerData.Value_4_Icon = ParseFloat(values[20]);
            towerData.Value_5 = ParseFloat(values[21]);
            towerData.Value_5_Desc = ParseFloat(values[22]);
            towerData.Value_5_Icon = ParseFloat(values[23]);
            towerData.Level_Effect_Group_ID = ParseFloat(values[24]);
            towerData.Evolve_Effect_Group_ID = ParseFloat(values[25]);
            towerData.Enchant_Effect_Group_ID = ParseFloat(values[26]);

            return towerData;
        }

        private MonsterData ParseMonsterCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 12) return null;

            MonsterData monsterData = new MonsterData();

            monsterData.ID = ParseInt(values[0]);
            monsterData.Asset_ID = ParseInt(values[1]);
            monsterData.Type = ParseInt(values[2]);
            monsterData.Move_Speed = ParseFloat(values[3]);
            monsterData.HP = ParseInt(values[4]);
            monsterData.Gold = ParseInt(values[5]);
            monsterData.ATK = ParseInt(values[6]);
            monsterData.Value_1 = ParseFloat(values[7]);
            monsterData.Value_2 = ParseFloat(values[8]);
            monsterData.Value_3 = ParseFloat(values[9]);
            monsterData.Value_4 = ParseFloat(values[10]);
            monsterData.Value_5 = ParseFloat(values[11]);

            return monsterData;
        }

        private BossData ParseBossCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 12) return null;

            BossData bossData = new BossData();

            bossData.ID = ParseInt(values[0]);
            bossData.Asset_ID = ParseInt(values[1]);
            bossData.Type = ParseInt(values[2]);
            bossData.HP = ParseInt(values[3]);
            bossData.Move_Speed = ParseFloat(values[4]);
            bossData.ATK = ParseInt(values[5]);
            bossData.Gold = ParseInt(values[6]);
            bossData.Value_1 = ParseInt(values[7]);
            bossData.Value_2 = ParseInt(values[8]);
            bossData.Value_3 = ParseInt(values[9]);
            bossData.Value_4 = ParseInt(values[10]);
            bossData.Value_5 = ParseFloat(values[11]);

            return bossData;
        }

        private int ParseInt(string value, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(value) || value.Trim().ToLower() == "null")
                return defaultValue;
            
            return int.TryParse(value.Trim(), out int result) ? result : defaultValue;
        }

        private float ParseFloat(string value, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(value) || value.Trim().ToLower() == "null")
                return defaultValue;
            
            return float.TryParse(value.Trim(), out float result) ? result : defaultValue;
        }

        private void CacheWaveDataByArena(List<WaveData> allWaveData)
        {
            waveDataCache.Clear();
            
            var groupedByArena = allWaveData.GroupBy(w => (ArenaType)w.Arena);
            
            foreach (var group in groupedByArena)
            {
                var sortedData = group.OrderBy(w => w.Wave_Index).ToList();
                waveDataCache[group.Key] = sortedData;
            }
        }

        public List<WaveData> GetWaveDataByArena(ArenaType arenaType)
        {
            if (!isInitialized) return new List<WaveData>();

            return waveDataCache.ContainsKey(arenaType) ? waveDataCache[arenaType] : new List<WaveData>();
        }

        public WaveData GetWaveDataByWaveIndex(ArenaType arenaType, int waveIndex)
        {
            var arenaData = GetWaveDataByArena(arenaType);
            
            for (int i = 0; i < arenaData.Count; i++)
            {
                if (arenaData[i].Wave_Index == waveIndex)
                    return arenaData[i];
            }
            
            return null;
        }

        public int GetMaxWaveIndex(ArenaType arenaType)
        {
            var arenaData = GetWaveDataByArena(arenaType);
            if (arenaData.Count == 0) return 0;
            
            int maxIndex = 0;
            for (int i = 0; i < arenaData.Count; i++)
            {
                if (arenaData[i].Wave_Index > maxIndex)
                    maxIndex = arenaData[i].Wave_Index;
            }
            
            return maxIndex;
        }

        public MonsterData GetMonsterData(int monsterID)
        {
            if (!isInitialized) return null;

            return monsterDataCache.ContainsKey(monsterID) ? monsterDataCache[monsterID] : null;
        }

        public BossData GetBossData(int bossID)
        {
            if (!isInitialized) return null;

            return bossDataCache.ContainsKey(bossID) ? bossDataCache[bossID] : null;
        }

        public MonsterData[] GetAllMonsters()
        {
            return allMonstersCache;
        }

        public BossData[] GetAllBosses()
        {
            return allBossesCache;
        }

        public SummonCostData GetSummonCostData(int summonCount)
        {
            if (!isInitialized) return null;

            return summonCostDataCache.ContainsKey(summonCount) ? summonCostDataCache[summonCount] : null;
        }

        public DefaultData GetDefaultData(int id = 1)
        {
            if (!isInitialized) return null;

            return defaultDataCache.ContainsKey(id) ? defaultDataCache[id] : null;
        }

        public TowerData GetTowerData(int towerID)
        {
            if (!isInitialized) return null;

            return towerDataCache.ContainsKey(towerID) ? towerDataCache[towerID] : null;
        }

        public TowerData[] GetAllTowers()
        {
            return allTowersCache;
        }

        public bool HasWaveData(ArenaType arenaType, int waveIndex)
        {
            return GetWaveDataByWaveIndex(arenaType, waveIndex) != null;
        }

        public bool HasMonsterData(int monsterID)
        {
            return monsterDataCache.ContainsKey(monsterID);
        }

        public bool HasBossData(int bossID)
        {
            return bossDataCache.ContainsKey(bossID);
        }

        public bool HasSummonCostData(int summonCount)
        {
            return summonCostDataCache.ContainsKey(summonCount);
        }

        public bool HasTowerData(int towerID)
        {
            return towerDataCache.ContainsKey(towerID);
        }

        public void ReloadCSV()
        {
            waveDataCache.Clear();
            monsterDataCache.Clear();
            bossDataCache.Clear();
            summonCostDataCache.Clear();
            defaultDataCache.Clear();
            towerDataCache.Clear();
            isInitialized = false;
            InitializeCSVData();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Reload Wave CSV")]
        private static void ReloadCSVEditor()
        {
            if (Instance != null)
            {
                Instance.ReloadCSV();
                Debug.Log("CSV 데이터가 다시 로드되었습니다.");
            }
        }
#endif
    }
}