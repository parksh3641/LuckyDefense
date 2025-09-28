using UnityEngine;
using System.Collections.Generic;

namespace LuckyDefense
{
    public partial class TowerManager
    {
        [Header("AI Settings")]
        [SerializeField] private bool isAIManager = false;
        [SerializeField] private float aiSpawnInterval = 3f;
        [SerializeField] private AIStrategy aiStrategy = AIStrategy.Balanced;
        [SerializeField] private Transform[] aiSpawnPositions;
        
        private float aiSpawnTimer = 0f;
        private bool aiAutoSpawn = false;
        
        public enum AIStrategy
        {
            Aggressive,
            Defensive,
            Balanced,
            Economic
        }

        private void Update()
        {
            if (isAIManager && aiAutoSpawn)
            {
                aiSpawnTimer += Time.deltaTime;
                if (aiSpawnTimer >= aiSpawnInterval)
                {
                    aiSpawnTimer = 0f;
                    AISpawnTower();
                }
            }
        }

        private void Awake()
        {
            if (isAIManager && aiSpawnPositions != null && aiSpawnPositions.Length > 0)
            {
                spawnPositions = aiSpawnPositions;
            }
        }

        public void SetupAsAI()
        {
            isAIManager = true;
            summonCount = 0;
            
            if (aiSpawnPositions != null && aiSpawnPositions.Length > 0)
                spawnPositions = aiSpawnPositions;
        }

        public void StartAIAutoSpawn()
        {
            aiAutoSpawn = true;
            aiSpawnTimer = 0f;
        }

        public void StopAIAutoSpawn()
        {
            aiAutoSpawn = false;
        }

        public void SetAIStrategy(AIStrategy strategy)
        {
            aiStrategy = strategy;
            aiSpawnInterval = strategy switch
            {
                AIStrategy.Aggressive => 2f,
                AIStrategy.Defensive => 4f,
                AIStrategy.Balanced => 3f,
                AIStrategy.Economic => 5f,
                _ => 3f
            };
        }

        public bool AISpawnTower()
        {
            if (!isAIManager) return false;

            int targetTowerType = GetAITargetTowerType();
            if (targetTowerType == 0) return false;

            Vector2Int bestSlot = GetAIBestSlot(targetTowerType);
            if (bestSlot.x == -1) return false;

            int cost = GetAISummonCost();
            int currentGold = gameManager.AIGold;
            if (currentGold < cost) return false;

            PlaceTower(bestSlot.x, bestSlot.y, targetTowerType);
            gameManager.AIGold = currentGold - cost;
            summonCount++;

            return true;
        }

        private int GetAISummonCost()
        {
            if (csvManager.HasSummonCostData(summonCount + 1))
            {
                return csvManager.GetSummonCostData(summonCount + 1).Summon_cost_Gold;
            }
            return 10 + (summonCount * 10);
        }

        private int GetAITargetTowerType()
        {
            var emptySlots = GetEmptySlotCount();
            var existingGroups = GetTowerGroupsByType();
            
            return aiStrategy switch
            {
                AIStrategy.Aggressive => GetAggressiveTowerType(existingGroups),
                AIStrategy.Defensive => GetDefensiveTowerType(existingGroups, emptySlots),
                AIStrategy.Economic => GetEconomicTowerType(existingGroups),
                _ => GetRandomTowerType()
            };
        }

        private Vector2Int GetAIBestSlot(int towerType)
        {
            return aiStrategy switch
            {
                AIStrategy.Aggressive => FindAggressiveSlot(towerType),
                AIStrategy.Defensive => FindDefensiveSlot(towerType),
                _ => FindBestSlot(towerType)
            };
        }

        private int GetAggressiveTowerType(Dictionary<int, int> existingGroups)
        {
            var heroTypes = new int[] { 5, 6 };
            var rareTypes = new int[] { 3, 4 };
            
            foreach (var heroType in heroTypes)
            {
                if (existingGroups.ContainsKey(heroType) && CanStackTowerType(heroType))
                    return heroType;
            }
            
            if (Random.Range(0f, 100f) < 30f)
                return heroTypes[Random.Range(0, heroTypes.Length)];
            
            return rareTypes[Random.Range(0, rareTypes.Length)];
        }

        private int GetDefensiveTowerType(Dictionary<int, int> existingGroups, int emptySlots)
        {
            if (emptySlots > 10)
                return Random.Range(1, 3);
            
            var normalTypes = new int[] { 1, 2 };
            foreach (var normalType in normalTypes)
            {
                if (existingGroups.ContainsKey(normalType) && CanStackTowerType(normalType))
                    return normalType;
            }
            
            return normalTypes[Random.Range(0, normalTypes.Length)];
        }

        private int GetEconomicTowerType(Dictionary<int, int> existingGroups)
        {
            var normalTypes = new int[] { 1, 2 };
            
            foreach (var normalType in normalTypes)
            {
                if (existingGroups.ContainsKey(normalType) && CanStackTowerType(normalType))
                    return normalType;
            }
            
            return normalTypes[Random.Range(0, normalTypes.Length)];
        }

        private Vector2Int FindAggressiveSlot(int towerType)
        {
            var frontRowSlots = new List<Vector2Int>();
            var sameTypeSlots = new List<Vector2Int>();
            
            for (int col = 0; col < 6; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    TowerSlot slot = towerGrid[row, col];
                    
                    if (slot.IsEmpty && row == 0)
                        frontRowSlots.Add(new Vector2Int(row, col));
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                        sameTypeSlots.Add(new Vector2Int(row, col));
                }
            }
            
            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];
            
            if (frontRowSlots.Count > 0)
                return frontRowSlots[Random.Range(0, frontRowSlots.Count)];
            
            return FindBestSlot(towerType);
        }

        private Vector2Int FindDefensiveSlot(int towerType)
        {
            var backRowSlots = new List<Vector2Int>();
            var sameTypeSlots = new List<Vector2Int>();
            
            for (int col = 0; col < 6; col++)
            {
                for (int row = 2; row >= 0; row--)
                {
                    TowerSlot slot = towerGrid[row, col];
                    
                    if (slot.IsEmpty && row == 2)
                        backRowSlots.Add(new Vector2Int(row, col));
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                        sameTypeSlots.Add(new Vector2Int(row, col));
                }
            }
            
            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];
            
            if (backRowSlots.Count > 0)
                return backRowSlots[Random.Range(0, backRowSlots.Count)];
            
            return FindBestSlot(towerType);
        }

        private int GetEmptySlotCount()
        {
            int count = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (towerGrid[row, col].IsEmpty)
                        count++;
                }
            }
            return count;
        }

        private Dictionary<int, int> GetTowerGroupsByType()
        {
            var groups = new Dictionary<int, int>();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = towerGrid[row, col];
                    if (!slot.IsEmpty)
                    {
                        if (groups.ContainsKey(slot.towerTypeId))
                            groups[slot.towerTypeId]++;
                        else
                            groups[slot.towerTypeId] = 1;
                    }
                }
            }
            return groups;
        }

        private bool CanStackTowerType(int towerType)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = towerGrid[row, col];
                    if (slot.towerTypeId == towerType && slot.CanAddStack)
                        return true;
                }
            }
            return false;
        }
    }
}