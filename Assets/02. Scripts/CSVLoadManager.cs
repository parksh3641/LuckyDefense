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
        public int Start_Time;
        public int Delay_Time;
        public int End_Time;
        public float HP_Value;
        public float Money_Value;
        public float Boss_Money_Value;
        public float Boss_HP_Value;
        
        public bool IsValidMonster => Monster_ID > 0;
        public bool IsBoss => Boss_HP_Value > 0 || Boss_Money_Value > 0;
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
        public int Range;
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
        [SerializeField] private bool debugMode = false;
        
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
                isInitialized = true;
                
                if (debugMode)
                {
                    DebugPrintLoadedData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 데이터 로드 실패: {e.Message}");
            }
        }

        private void LoadWaveDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(waveCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 웨이브 CSV 파일을 찾을 수 없습니다: {waveCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("웨이브 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            List<WaveData> allWaveData = new List<WaveData>();
            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    WaveData waveData = ParseWaveCSVLine(line);
                    if (waveData != null)
                    {
                        allWaveData.Add(waveData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"웨이브 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }

            CacheWaveDataByArena(allWaveData);
            
            Debug.Log($"총 {allWaveData.Count}개의 웨이브 데이터를 로드했습니다.");
        }

        private void LoadMonsterDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(monsterCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 몬스터 CSV 파일을 찾을 수 없습니다: {monsterCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("몬스터 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            monsterDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    MonsterData monsterData = ParseMonsterCSVLine(line);
                    if (monsterData != null && monsterData.IsValid)
                    {
                        monsterDataCache[monsterData.ID] = monsterData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"몬스터 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }
            
            Debug.Log($"총 {monsterDataCache.Count}개의 몬스터 데이터를 로드했습니다.");
        }

        private void LoadBossDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(bossCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 보스 CSV 파일을 찾을 수 없습니다: {bossCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("보스 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            bossDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    BossData bossData = ParseBossCSVLine(line);
                    if (bossData != null && bossData.IsValid)
                    {
                        bossDataCache[bossData.ID] = bossData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"보스 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }
            
            Debug.Log($"총 {bossDataCache.Count}개의 보스 데이터를 로드했습니다.");
        }

        private void LoadSummonCostDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(summonCostCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 소환 비용 CSV 파일을 찾을 수 없습니다: {summonCostCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("소환 비용 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            summonCostDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    SummonCostData summonCostData = ParseSummonCostCSVLine(line);
                    if (summonCostData != null && summonCostData.IsValid)
                    {
                        summonCostDataCache[summonCostData.ID] = summonCostData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"소환 비용 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }
            
            Debug.Log($"총 {summonCostDataCache.Count}개의 소환 비용 데이터를 로드했습니다.");
        }

        private void LoadDefaultDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(defaultCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 기본 설정 CSV 파일을 찾을 수 없습니다: {defaultCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("기본 설정 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            defaultDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    DefaultData defaultData = ParseDefaultCSVLine(line);
                    if (defaultData != null && defaultData.IsValid)
                    {
                        defaultDataCache[defaultData.ID] = defaultData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"기본 설정 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }
            
            Debug.Log($"총 {defaultDataCache.Count}개의 기본 설정 데이터를 로드했습니다.");
        }

        private void LoadTowerDataFromCSV()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(towerCSVFileName);
            
            if (csvFile == null)
            {
                Debug.LogError($"Resources 폴더에서 타워 CSV 파일을 찾을 수 없습니다: {towerCSVFileName}");
                return;
            }

            string[] lines = csvFile.text.Split('\n');
            
            if (lines.Length <= 1)
            {
                Debug.LogError("타워 CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            towerDataCache.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    TowerData towerData = ParseTowerCSVLine(line);
                    if (towerData != null && towerData.IsValid)
                    {
                        towerDataCache[towerData.ID] = towerData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"타워 CSV 라인 {i} 파싱 실패: {e.Message}");
                }
            }
            
            Debug.Log($"총 {towerDataCache.Count}개의 타워 데이터를 로드했습니다.");
        }

        private WaveData ParseWaveCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 12)
            {
                Debug.LogWarning($"웨이브 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            WaveData waveData = new WaveData();

            try
            {
                waveData.ID = ParseInt(values[0]);
                waveData.Wave_Index = ParseInt(values[1]);
                waveData.Arena = ParseInt(values[2]);
                waveData.Wave_Time = ParseInt(values[3]);
                waveData.Monster_ID = ParseInt(values[4], -1);
                waveData.Start_Time = ParseInt(values[5]);
                waveData.Delay_Time = ParseInt(values[6]);
                waveData.End_Time = ParseInt(values[7]);
                waveData.HP_Value = ParseFloat(values[8]);
                waveData.Money_Value = ParseFloat(values[9]);
                waveData.Boss_Money_Value = ParseFloat(values[10]);
                waveData.Boss_HP_Value = ParseFloat(values[11]);

                return waveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"WaveData 파싱 오류: {e.Message}");
                return null;
            }
        }

        private SummonCostData ParseSummonCostCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 2)
            {
                Debug.LogWarning($"소환 비용 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            SummonCostData summonCostData = new SummonCostData();

            try
            {
                summonCostData.ID = ParseInt(values[0]);
                summonCostData.Summon_cost_Gold = ParseInt(values[1]);

                return summonCostData;
            }
            catch (Exception e)
            {
                Debug.LogError($"SummonCostData 파싱 오류: {e.Message}");
                return null;
            }
        }

        private DefaultData ParseDefaultCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 2)
            {
                Debug.LogWarning($"기본 설정 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            DefaultData defaultData = new DefaultData();

            try
            {
                defaultData.ID = ParseInt(values[0]);
                defaultData.First_Money = ParseInt(values[1]);

                return defaultData;
            }
            catch (Exception e)
            {
                Debug.LogError($"DefaultData 파싱 오류: {e.Message}");
                return null;
            }
        }

        private TowerData ParseTowerCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 27)
            {
                Debug.LogWarning($"타워 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            TowerData towerData = new TowerData();

            try
            {
                towerData.ID = ParseInt(values[0]);
                towerData.Grade = ParseFloat(values[1]);
                towerData.Type = ParseInt(values[2]);
                towerData.Target = ParseInt(values[3]);
                towerData.Range = ParseInt(values[4]);
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
            catch (Exception e)
            {
                Debug.LogError($"TowerData 파싱 오류: {e.Message}");
                return null;
            }
        }

        private MonsterData ParseMonsterCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 12)
            {
                Debug.LogWarning($"몬스터 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            MonsterData monsterData = new MonsterData();

            try
            {
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
            catch (Exception e)
            {
                Debug.LogError($"MonsterData 파싱 오류: {e.Message}");
                return null;
            }
        }

        private BossData ParseBossCSVLine(string line)
        {
            string[] values = line.Split(',');
            
            if (values.Length < 12)
            {
                Debug.LogWarning($"보스 CSV 라인의 컬럼 수가 부족합니다: {values.Length}");
                return null;
            }

            BossData bossData = new BossData();

            try
            {
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
            catch (Exception e)
            {
                Debug.LogError($"BossData 파싱 오류: {e.Message}");
                return null;
            }
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
                var sortedData = group.OrderBy(w => w.Wave_Index)
                                     .ThenBy(w => w.Start_Time)
                                     .ToList();
                
                waveDataCache[group.Key] = sortedData;
            }
        }

        public List<WaveData> GetWaveDataByArena(ArenaType arenaType)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return new List<WaveData>();
            }

            return waveDataCache.ContainsKey(arenaType) ? 
                   new List<WaveData>(waveDataCache[arenaType]) : 
                   new List<WaveData>();
        }

        public List<WaveData> GetWaveDataByWaveIndex(ArenaType arenaType, int waveIndex)
        {
            var arenaData = GetWaveDataByArena(arenaType);
            return arenaData.Where(w => w.Wave_Index == waveIndex).ToList();
        }

        public WaveData[] GetWaveDataArray(ArenaType arenaType)
        {
            return GetWaveDataByArena(arenaType).ToArray();
        }

        public int GetMaxWaveIndex(ArenaType arenaType)
        {
            var arenaData = GetWaveDataByArena(arenaType);
            return arenaData.Count > 0 ? arenaData.Max(w => w.Wave_Index) : 0;
        }

        public MonsterData GetMonsterData(int monsterID)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return null;
            }

            return monsterDataCache.ContainsKey(monsterID) ? monsterDataCache[monsterID] : null;
        }

        public BossData GetBossData(int bossID)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return null;
            }

            return bossDataCache.ContainsKey(bossID) ? bossDataCache[bossID] : null;
        }

        public List<int> GetAllBossIds()
        {
            return new List<int>(bossDataCache.Keys);
        }

        public List<int> GetAllMonsterIds()
        {
            return new List<int>(monsterDataCache.Keys);
        }

        public MonsterData[] GetAllMonsters()
        {
            return monsterDataCache.Values.ToArray();
        }

        public BossData[] GetAllBosses()
        {
            return bossDataCache.Values.ToArray();
        }

        public SummonCostData GetSummonCostData(int summonCount)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return null;
            }

            return summonCostDataCache.ContainsKey(summonCount) ? summonCostDataCache[summonCount] : null;
        }

        public DefaultData GetDefaultData(int id = 1)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return null;
            }

            return defaultDataCache.ContainsKey(id) ? defaultDataCache[id] : null;
        }

        public TowerData GetTowerData(int towerID)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("CSVLoadManager가 아직 초기화되지 않았습니다.");
                return null;
            }

            return towerDataCache.ContainsKey(towerID) ? towerDataCache[towerID] : null;
        }

        public List<int> GetAllTowerIds()
        {
            return new List<int>(towerDataCache.Keys);
        }

        public TowerData[] GetAllTowers()
        {
            return towerDataCache.Values.ToArray();
        }

        public TowerData[] GetTowersByType(int towerType)
        {
            return towerDataCache.Values.Where(t => t.Type == towerType).ToArray();
        }

        public int GetSummonCost(int summonCount)
        {
            var costData = GetSummonCostData(summonCount);
            return costData?.Summon_cost_Gold ?? 10;
        }

        public int GetFirstMoney()
        {
            var defaultData = GetDefaultData();
            return defaultData?.First_Money ?? 100;
        }

        public List<int> GetAvailableMonsterIds(ArenaType arenaType, int waveIndex)
        {
            var waveData = GetWaveDataByWaveIndex(arenaType, waveIndex);
            return waveData.Where(w => w.IsValidMonster)
                          .Select(w => w.Monster_ID)
                          .Distinct()
                          .ToList();
        }

        public bool HasWaveData(ArenaType arenaType, int waveIndex)
        {
            return GetWaveDataByWaveIndex(arenaType, waveIndex).Count > 0;
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

        private void DebugPrintLoadedData()
        {
            Debug.Log("=== CSV 로드 결과 ===");
            
            Debug.Log($"웨이브 데이터:");
            foreach (var kvp in waveDataCache)
            {
                Debug.Log($"  아레나 {kvp.Key}: {kvp.Value.Count}개 데이터");
                
                var waveGroups = kvp.Value.GroupBy(w => w.Wave_Index);
                foreach (var waveGroup in waveGroups.Take(3))
                {
                    Debug.Log($"    웨이브 {waveGroup.Key}: {waveGroup.Count()}개");
                }
            }
            
            Debug.Log($"몬스터 데이터: {monsterDataCache.Count}개");
            foreach (var monster in monsterDataCache.Values.Take(3))
            {
                Debug.Log($"  몬스터 ID {monster.ID}: HP {monster.HP}, ATK {monster.ATK}");
            }
            
            Debug.Log($"보스 데이터: {bossDataCache.Count}개");
            foreach (var boss in bossDataCache.Values.Take(3))
            {
                Debug.Log($"  보스 ID {boss.ID}: HP {boss.HP}, ATK {boss.ATK}");
            }
            
            Debug.Log($"소환 비용 데이터: {summonCostDataCache.Count}개");
            foreach (var cost in summonCostDataCache.Values.Take(5))
            {
                Debug.Log($"  소환 {cost.ID}회: {cost.Summon_cost_Gold} 골드");
            }
            
            Debug.Log($"기본 설정 데이터: {defaultDataCache.Count}개");
            foreach (var defaultData in defaultDataCache.Values)
            {
                Debug.Log($"  시작 골드: {defaultData.First_Money}");
            }
            
            Debug.Log($"타워 데이터: {towerDataCache.Count}개");
            foreach (var tower in towerDataCache.Values.Take(3))
            {
                Debug.Log($"  타워 ID {tower.ID}: ATK {tower.Atk}, Range {tower.Range}, Type {tower.Type}");
            }
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

        public static void LoadFromResources(string waveFileName = "Wave_Data", string monsterFileName = "Monster_Data", 
            string bossFileName = "Boss_Data", string summonCostFileName = "SummonCost_Data", 
            string defaultFileName = "Default_Data", string towerFileName = "Tower_Data")
        {
            if (Instance != null)
            {
                Instance.waveCSVFileName = waveFileName;
                Instance.monsterCSVFileName = monsterFileName;
                Instance.bossCSVFileName = bossFileName;
                Instance.summonCostCSVFileName = summonCostFileName;
                Instance.defaultCSVFileName = defaultFileName;
                Instance.towerCSVFileName = towerFileName;
                Instance.ReloadCSV();
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Reload Wave CSV")]
        private static void ReloadCSVEditor()
        {
            if (Instance != null)
            {
                Instance.ReloadCSV();
                Debug.Log("웨이브 CSV 데이터가 다시 로드되었습니다.");
            }
        }
#endif
    }
}