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
        [SerializeField] protected Transform towerParent;
        
        [Header("Tower Group Prefabs")]
        [SerializeField] protected GameObject[] normalTowerGroupPrefabs;
        [SerializeField] protected GameObject[] rareTowerGroupPrefabs;
        [SerializeField] protected GameObject[] heroTowerGroupPrefabs;
        
        [Header("Managers")]
        [SerializeField] protected GameManager gameManager;
        
        protected TowerSlot[,] towerGrid = new TowerSlot[3, 6];
        protected CSVLoadManager csvManager;
        protected int summonCount = 0;

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
                    towerGrid[row, col] = new TowerSlot
                    {
                        towerTypeId = 0,
                        stackCount = 0,
                        position = spawnPositions[index].position,
                        towerGroup = null
                    };
                }
            }
        }

        public virtual bool SpawnRandomTower()
        {
            int currentCost = GetCurrentSummonCost();
            int currentGold = gameManager.MyGold;
            
            if (currentGold < currentCost) return false;

            int selectedTowerType = GetRandomTowerType();
            if (selectedTowerType == 0) return false;

            Vector2Int bestSlot = FindBestSlot(selectedTowerType);
            if (bestSlot.x == -1) return false;

            PlaceTower(bestSlot.x, bestSlot.y, selectedTowerType);
            gameManager.MyGold = currentGold - currentCost;
            summonCount++;
            
            return true;
        }

        protected virtual int GetCurrentSummonCost()
        {
            if (csvManager.HasSummonCostData(summonCount + 1))
            {
                return csvManager.GetSummonCostData(summonCount + 1).Summon_cost_Gold;
            }
            return 10 + (summonCount * 10);
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

        protected virtual Vector2Int FindBestSlot(int towerType)
        {
            List<Vector2Int> sameTypeSlots = new List<Vector2Int>();
            List<Vector2Int> emptySlots = new List<Vector2Int>();

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = towerGrid[row, col];
                    
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

        protected virtual void PlaceTower(int row, int col, int towerType)
        {
            TowerSlot slot = towerGrid[row, col];
            
            if (slot.IsEmpty)
            {
                slot.towerTypeId = towerType;
                slot.stackCount = 1;
                CreateTowerGroup(row, col, towerType);
            }
            else if (slot.towerTypeId == towerType && slot.CanAddStack)
            {
                slot.stackCount++;
                slot.towerGroup.AddTower();
            }
        }

        protected virtual void CreateTowerGroup(int row, int col, int towerType)
        {
            GameObject prefab = GetTowerGroupPrefab(towerType);
            if (prefab == null) return;

            Vector3 position = towerGrid[row, col].position;
            GameObject groupObj = Instantiate(prefab, position, Quaternion.identity, towerParent);
            TowerGroup towerGroup = groupObj.GetComponent<TowerGroup>();
            
            towerGroup.Initialize();
            towerGrid[row, col].towerGroup = towerGroup;
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

        public virtual bool RemoveTower(int row, int col)
        {
            if (row < 0 || row >= 3 || col < 0 || col >= 6) return false;
            
            TowerSlot slot = towerGrid[row, col];
            if (slot.IsEmpty) return false;

            slot.towerGroup.RemoveAllTowers();
            Destroy(slot.towerGroup.gameObject);

            slot.towerTypeId = 0;
            slot.stackCount = 0;
            slot.towerGroup = null;
            return true;
        }

        public virtual int GetNextSummonCost()
        {
            return GetCurrentSummonCost();
        }

        public virtual int GetTotalTowerCount()
        {
            int count = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (!towerGrid[row, col].IsEmpty)
                        count++;
                }
            }
            return count;
        }

        public virtual List<TowerGroup> GetAllTowerGroups()
        {
            List<TowerGroup> groups = new List<TowerGroup>();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (towerGrid[row, col].towerGroup != null)
                        groups.Add(towerGrid[row, col].towerGroup);
                }
            }
            return groups;
        }
    }
}