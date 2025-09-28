using UnityEngine;
using System.Collections.Generic;

namespace LuckyDefense
{
    [System.Serializable]
    public class TowerSlot
    {
        public int towerTypeId;
        public int stackCount;
        public Vector3 position;
        public TowerGroup towerGroup;
        
        public bool IsEmpty => towerTypeId == 0;
        public bool CanAddStack => !IsEmpty && stackCount < 3;
        public bool IsFull => stackCount >= 3;
    }

    public partial class TowerManager : MonoBehaviour
    {
        [Header("Field Settings")]
        [SerializeField] protected Transform[] spawnPositions;
        [SerializeField] private Transform[] aiSpawnPositions;
        [SerializeField] protected Transform towerParent;
        
        [Header("Tower Group Prefabs")]
        [SerializeField] protected GameObject[] normalTowerGroupPrefabs;
        [SerializeField] protected GameObject[] rareTowerGroupPrefabs;
        [SerializeField] protected GameObject[] heroTowerGroupPrefabs;
        
        [Header("Managers")]
        [SerializeField] protected GameManager gameManager;
        
        protected TowerSlot[,] myTowerGrid = new TowerSlot[3, 6];
        protected TowerSlot[,] aiTowerGrid = new TowerSlot[3, 6];
        protected CSVLoadManager csvManager;
        
        protected int mySummonCount = 0;
        protected int aiSummonCount = 0;

        protected virtual void Start()
        {
            InitializeGrid();
            csvManager = CSVLoadManager.Instance;
            gameManager = GameManager.Instance;
        }

        protected virtual void InitializeGrid()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int index = row * 6 + col;
                    myTowerGrid[row, col] = new TowerSlot
                    {
                        towerTypeId = 0,
                        stackCount = 0,
                        position = spawnPositions[index].position,
                        towerGroup = null
                    };
                    aiTowerGrid[row, col] = new TowerSlot
                    {
                        towerTypeId = 0,
                        stackCount = 0,
                        position = aiSpawnPositions[index].position,
                        towerGroup = null
                    };
                }
            }
        }

        public virtual bool SpawnRandomTowerForPlayer()
        {
            int currentCost = GetMyCurrentSummonCost();
            int currentGold = gameManager.MyGold;
    
            if (currentGold < currentCost) 
            {
                Debug.Log($"플레이어 골드 부족: 필요 {currentCost}, 보유 {currentGold}");
                return false;
            }

            int selectedTowerType = GetRandomTowerType();
            if (selectedTowerType == 0) return false;

            Vector2Int bestSlot = FindBestSlotForPlayer(selectedTowerType);
            if (bestSlot.x == -1) 
            {
                Debug.Log($"플레이어 배치 가능한 슬롯 없음");
                return false;
            }

            PlaceTowerForPlayer(bestSlot.x, bestSlot.y, selectedTowerType);
            gameManager.MyGold = currentGold - currentCost;
            gameManager.UpdatePlayerSummonCost(GetNextSummonCost(mySummonCount));
            mySummonCount++;
            Debug.Log($"플레이어 타워 생성 완료: 타입 {selectedTowerType}, 위치 ({bestSlot.x}, {bestSlot.y})");
            return true;
        }

        public virtual bool SpawnRandomTowerForAI()
        {
            int currentCost = GetAiCurrentSummonCost();
            int currentGold = gameManager.AIGold;
    
            if (currentGold < currentCost) 
            {
                Debug.Log($"AI 골드 부족: 필요 {currentCost}, 보유 {currentGold}");
                return false;
            }

            int selectedTowerType = GetRandomTowerType();
            if (selectedTowerType == 0) return false;

            Vector2Int bestSlot = FindBestSlotForAI(selectedTowerType);
            if (bestSlot.x == -1) 
            {
                Debug.Log($"AI 배치 가능한 슬롯 없음");
                return false;
            }

            PlaceTowerForAI(bestSlot.x, bestSlot.y, selectedTowerType);
            gameManager.AIGold = currentGold - currentCost;
            gameManager.UpdateAISummonCost(GetNextSummonCost(aiSummonCount));
            aiSummonCount++;
            Debug.Log($"AI 타워 생성 완료: 타입 {selectedTowerType}, 위치 ({bestSlot.x}, {bestSlot.y})");
            return true;
        }

        protected virtual int GetMyCurrentSummonCost()
        {
            if (csvManager.HasSummonCostData(mySummonCount + 1))
            {
                return csvManager.GetSummonCostData(mySummonCount + 1).Summon_cost_Gold;
            }
            return 10 + (mySummonCount * 10);
        }
        
        protected virtual int GetAiCurrentSummonCost()
        {
            if (csvManager.HasSummonCostData(aiSummonCount + 1))
            {
                return csvManager.GetSummonCostData(aiSummonCount + 1).Summon_cost_Gold;
            }
            return 10 + (aiSummonCount * 10);
        }
        

        protected virtual int GetRandomTowerType()
        {
            float randomValue = Random.Range(0f, 100f);
            
            if (randomValue < gameManager.SpawnHeroPercent)
            {
                return GetRandomFromArray(heroTowerGroupPrefabs, 5);
            }
            else if (randomValue < gameManager.SpawnHeroPercent + gameManager.SpawnRarePercent)
            {
                return GetRandomFromArray(rareTowerGroupPrefabs, 3);
            }
            else
            {
                return GetRandomFromArray(normalTowerGroupPrefabs, 1);
            }
        }
        
        private int GetRandomFromArray(GameObject[] prefabArray, int baseId)
        {
            if (prefabArray == null || prefabArray.Length == 0) return baseId;
            return baseId + Random.Range(0, prefabArray.Length);
        }

        protected virtual Vector2Int FindBestSlotForPlayer(int towerType)
        {
            List<Vector2Int> sameTypeSlots = new List<Vector2Int>();
            List<Vector2Int> emptySlots = new List<Vector2Int>();

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
                    
                    if (slot.IsEmpty)
                    {
                        emptySlots.Add(new Vector2Int(row, col));
                    }
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                    {
                        sameTypeSlots.Add(new Vector2Int(row, col));
                    }
                }
            }

            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];

            if (emptySlots.Count > 0)
                return emptySlots[Random.Range(0, emptySlots.Count)];

            return new Vector2Int(-1, -1);
        }

        protected virtual Vector2Int FindBestSlotForAI(int towerType)
        {
            List<Vector2Int> sameTypeSlots = new List<Vector2Int>();
            List<Vector2Int> emptySlots = new List<Vector2Int>();

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = aiTowerGrid[row, col];
                    
                    if (slot.IsEmpty)
                    {
                        emptySlots.Add(new Vector2Int(row, col));
                    }
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                    {
                        sameTypeSlots.Add(new Vector2Int(row, col));
                    }
                }
            }

            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];

            if (emptySlots.Count > 0)
                return emptySlots[Random.Range(0, emptySlots.Count)];

            return new Vector2Int(-1, -1);
        }

        protected virtual void PlaceTowerForPlayer(int row, int col, int towerType)
        {
            TowerSlot slot = myTowerGrid[row, col];
            
            if (slot.IsEmpty)
            {
                slot.towerTypeId = towerType;
                slot.stackCount = 1;
                CreateTowerGroupForPlayer(row, col, towerType);
            }
            else if (slot.towerTypeId == towerType && slot.CanAddStack)
            {
                slot.stackCount++;
                slot.towerGroup.AddTower();
            }
        }

        protected virtual void PlaceTowerForAI(int row, int col, int towerType)
        {
            TowerSlot slot = aiTowerGrid[row, col];
            
            if (slot.IsEmpty)
            {
                slot.towerTypeId = towerType;
                slot.stackCount = 1;
                CreateTowerGroupForAI(row, col, towerType);
            }
            else if (slot.towerTypeId == towerType && slot.CanAddStack)
            {
                slot.stackCount++;
                slot.towerGroup.AddTower();
            }
        }

        protected virtual void CreateTowerGroupForPlayer(int row, int col, int towerType)
        {
            GameObject prefab = GetTowerGroupPrefab(towerType);
            if (prefab == null) return;

            Vector3 position = myTowerGrid[row, col].position;
            GameObject groupObj = Instantiate(prefab, position, Quaternion.identity, towerParent);
            TowerGroup towerGroup = groupObj.GetComponent<TowerGroup>();
            
            towerGroup.Initialize();
            myTowerGrid[row, col].towerGroup = towerGroup;
        }

        protected virtual void CreateTowerGroupForAI(int row, int col, int towerType)
        {
            GameObject prefab = GetTowerGroupPrefab(towerType);
            if (prefab == null) return;

            Vector3 position = aiTowerGrid[row, col].position;
            GameObject groupObj = Instantiate(prefab, position, Quaternion.identity, towerParent);
            TowerGroup towerGroup = groupObj.GetComponent<TowerGroup>();
            
            towerGroup.Initialize();
            aiTowerGrid[row, col].towerGroup = towerGroup;
        }

        protected virtual GameObject GetTowerGroupPrefab(int towerType)
        {
            switch (towerType)
            {
                case 1:
                case 2:
                    int normalIndex = towerType - 1;
                    return normalIndex < normalTowerGroupPrefabs.Length ? normalTowerGroupPrefabs[normalIndex] : null;
                
                case 3:
                case 4:
                    int rareIndex = towerType - 3;
                    return rareIndex < rareTowerGroupPrefabs.Length ? rareTowerGroupPrefabs[rareIndex] : null;
                
                case 5:
                case 6:
                    int heroIndex = towerType - 5;
                    return heroIndex < heroTowerGroupPrefabs.Length ? heroTowerGroupPrefabs[heroIndex] : null;
                
                default:
                    return null;
            }
        }

        public virtual bool RemovePlayerTower(int row, int col)
        {
            if (row < 0 || row >= 3 || col < 0 || col >= 6) return false;
            
            TowerSlot slot = myTowerGrid[row, col];
            if (slot.IsEmpty) return false;

            slot.towerGroup.RemoveAllTowers();
            Destroy(slot.towerGroup.gameObject);

            slot.towerTypeId = 0;
            slot.stackCount = 0;
            slot.towerGroup = null;
            return true;
        }

        public virtual bool RemoveAITower(int row, int col)
        {
            if (row < 0 || row >= 3 || col < 0 || col >= 6) return false;
            
            TowerSlot slot = aiTowerGrid[row, col];
            if (slot.IsEmpty) return false;

            slot.towerGroup.RemoveAllTowers();
            Destroy(slot.towerGroup.gameObject);

            slot.towerTypeId = 0;
            slot.stackCount = 0;
            slot.towerGroup = null;
            return true;
        }

        public int GetNextSummonCost(int number)
        {
            if (csvManager != null && csvManager.HasSummonCostData(number + 2))
            {
                return csvManager.GetSummonCostData(number + 2).Summon_cost_Gold;
            }
            return 10 + ((number + 1) * 10);
        }

        public int GetNextSummonCost()
        {
            if (csvManager != null && csvManager.HasSummonCostData(mySummonCount + 1))
            {
                return csvManager.GetSummonCostData(mySummonCount + 1).Summon_cost_Gold;
            }
            return 10 + (mySummonCount) * 10;
        }

        public virtual int GetPlayerTotalTowerCount()
        {
            int count = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (!myTowerGrid[row, col].IsEmpty)
                        count++;
                }
            }
            return count;
        }

        public virtual int GetAITotalTowerCount()
        {
            int count = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (!aiTowerGrid[row, col].IsEmpty)
                        count++;
                }
            }
            return count;
        }

        public virtual int GetTotalTowerCount()
        {
            return GetPlayerTotalTowerCount() + GetAITotalTowerCount();
        }
    }
}