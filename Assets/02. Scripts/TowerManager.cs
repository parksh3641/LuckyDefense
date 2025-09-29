using UnityEngine;
using System.Collections;
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
        [SerializeField] protected GameObject[] mythTowerGroupPrefabs;
        
        [Header("Managers")]
        [SerializeField] protected GameManager gameManager;
        
        protected TowerSlot[,] myTowerGrid = new TowerSlot[3, 6];
        protected TowerSlot[,] aiTowerGrid = new TowerSlot[3, 6];
        protected CSVLoadManager csvManager;
        
        protected int mySummonCount = 0;
        protected int aiSummonCount = 0;
        
        private const int SELL_PRICE = 10;

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
            Vector2Int firstEmptySlot = new Vector2Int(-1, -1);

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
            
                    if (slot.IsEmpty)
                    {
                        if (firstEmptySlot.x == -1)
                        {
                            firstEmptySlot = new Vector2Int(row, col);
                        }
                    }
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                    {
                        sameTypeSlots.Add(new Vector2Int(row, col));
                    }
                }
            }

            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];

            if (firstEmptySlot.x != -1)
                return firstEmptySlot;

            return new Vector2Int(-1, -1);
        }

        protected virtual Vector2Int FindBestSlotForAI(int towerType)
        {
            List<Vector2Int> sameTypeSlots = new List<Vector2Int>();
            Vector2Int firstEmptySlot = new Vector2Int(-1, -1);

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = aiTowerGrid[row, col];
            
                    if (slot.IsEmpty)
                    {
                        if (firstEmptySlot.x == -1)
                        {
                            firstEmptySlot = new Vector2Int(row, col);
                        }
                    }
                    else if (slot.towerTypeId == towerType && slot.CanAddStack)
                    {
                        sameTypeSlots.Add(new Vector2Int(row, col));
                    }
                }
            }

            if (sameTypeSlots.Count > 0)
                return sameTypeSlots[Random.Range(0, sameTypeSlots.Count)];

            if (firstEmptySlot.x != -1)
                return firstEmptySlot;

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
                
                case 7:
                case 8:
                    int mythIndex = towerType - 7;
                    return mythIndex < mythTowerGroupPrefabs.Length ? mythTowerGroupPrefabs[mythIndex] : null;
                
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

        public virtual bool MovePlayerTower(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsValidPosition(fromRow, fromCol) || !IsValidPosition(toRow, toCol))
            {
                Debug.LogError("잘못된 위치입니다.");
                return false;
            }

            TowerSlot fromSlot = myTowerGrid[fromRow, fromCol];
            TowerSlot toSlot = myTowerGrid[toRow, toCol];

            if (fromSlot.IsEmpty)
            {
                Debug.LogError("이동할 타워가 없습니다.");
                return false;
            }

            if (!toSlot.IsEmpty)
            {
                Debug.LogError("목적지에 이미 타워가 있습니다.");
                return false;
            }

            TowerGroup towerGroup = fromSlot.towerGroup;
            Vector3 newPosition = spawnPositions[toRow * 6 + toCol].position;
            
            StartCoroutine(MoveTowerSmoothly(towerGroup, newPosition));

            toSlot.towerTypeId = fromSlot.towerTypeId;
            toSlot.stackCount = fromSlot.stackCount;
            toSlot.towerGroup = towerGroup;

            fromSlot.towerTypeId = 0;
            fromSlot.stackCount = 0;
            fromSlot.towerGroup = null;

            Debug.Log($"타워 이동 완료: ({fromRow},{fromCol}) -> ({toRow},{toCol})");
            return true;
        }

        private IEnumerator MoveTowerSmoothly(TowerGroup towerGroup, Vector3 targetPosition)
        {
            Vector3 startPosition = towerGroup.transform.position;
            float moveSpeed = 10f;
    
            while (Vector3.Distance(towerGroup.transform.position, targetPosition) > 0.01f)
            {
                towerGroup.transform.position = Vector3.MoveTowards(towerGroup.transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            towerGroup.transform.position = targetPosition;
        }

        public virtual bool SwapPlayerTowers(int row1, int col1, int row2, int col2)
        {
            if (!IsValidPosition(row1, col1) || !IsValidPosition(row2, col2))
            {
                Debug.LogError("잘못된 위치입니다.");
                return false;
            }

            TowerSlot slot1 = myTowerGrid[row1, col1];
            TowerSlot slot2 = myTowerGrid[row2, col2];

            if (slot1.IsEmpty || slot2.IsEmpty)
            {
                Debug.LogError("교환할 타워 중 하나가 비어있습니다.");
                return false;
            }

            TowerGroup tower1 = slot1.towerGroup;
            TowerGroup tower2 = slot2.towerGroup;

            Vector3 pos1 = spawnPositions[row1 * 6 + col1].position;
            Vector3 pos2 = spawnPositions[row2 * 6 + col2].position;

            StartCoroutine(SwapTowersSmoothly(tower1, tower2, pos2, pos1));

            int tempTowerTypeId = slot1.towerTypeId;
            int tempStackCount = slot1.stackCount;
            TowerGroup tempTowerGroup = slot1.towerGroup;

            slot1.towerTypeId = slot2.towerTypeId;
            slot1.stackCount = slot2.stackCount;
            slot1.towerGroup = slot2.towerGroup;

            slot2.towerTypeId = tempTowerTypeId;
            slot2.stackCount = tempStackCount;
            slot2.towerGroup = tempTowerGroup;

            Debug.Log($"타워 교환 완료: ({row1},{col1}) <-> ({row2},{col2})");
            return true;
        }

        private IEnumerator SwapTowersSmoothly(TowerGroup tower1, TowerGroup tower2, Vector3 target1, Vector3 target2)
        {
            float moveSpeed = 10f;
    
            while (Vector3.Distance(tower1.transform.position, target1) > 0.01f || Vector3.Distance(tower2.transform.position, target2) > 0.01f)
            {
                tower1.transform.position = Vector3.MoveTowards(tower1.transform.position, target1, moveSpeed * Time.deltaTime);
                tower2.transform.position = Vector3.MoveTowards(tower2.transform.position, target2, moveSpeed * Time.deltaTime);
                yield return null;
            }

            tower1.transform.position = target1;
            tower2.transform.position = target2;
        }

        public bool MixPlayerTower(int row, int col)
        {
            if (!IsValidPosition(row, col)) return false;
            
            TowerSlot slot = myTowerGrid[row, col];
            
            if (slot.IsEmpty || !slot.IsFull) return false;
            if (slot.towerTypeId >= 5) return false;
            
            int upgradedTypeId = GetUpgradedTowerType(slot.towerTypeId);
            if (upgradedTypeId == 0) return false;
            
            RemovePlayerTower(row, col);
            
            Vector2Int targetSlot = FindSlotForMixedTower(upgradedTypeId);
            
            if (targetSlot.x != -1)
            {
                PlaceTowerForPlayer(targetSlot.x, targetSlot.y, upgradedTypeId);
                Debug.Log($"타워 합성 완료: 위치 ({targetSlot.x}, {targetSlot.y})");
            }
            else
            {
                Debug.LogError("합성된 타워를 배치할 슬롯이 없습니다.");
                return false;
            }
            
            return true;
        }

        private int GetUpgradedTowerType(int currentTypeId)
        {
            if (currentTypeId == 1 || currentTypeId == 2)
            {
                return Random.Range(3, 5);
            }
            else if (currentTypeId == 3 || currentTypeId == 4)
            {
                return Random.Range(5, 7);
            }
            
            return 0;
        }

        private Vector2Int FindSlotForMixedTower(int towerTypeId)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
                    
                    if (!slot.IsEmpty && slot.towerTypeId == towerTypeId && slot.CanAddStack)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
                    
                    if (slot.IsEmpty)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            
            return new Vector2Int(-1, -1);
        }

        public bool SellPlayerTower(int row, int col)
        {
            if (!IsValidPosition(row, col)) return false;
            
            TowerSlot slot = myTowerGrid[row, col];
            if (slot.IsEmpty) return false;
            
            if (slot.stackCount > 1)
            {
                slot.stackCount--;
                slot.towerGroup.RemoveTower();
                
                if (gameManager != null)
                {
                    gameManager.AddGold(SELL_PRICE);
                }
                
                Debug.Log($"타워 1개 판매: 위치 ({row}, {col}), 남은 스택: {slot.stackCount}");
            }
            else
            {
                RemovePlayerTower(row, col);
                
                if (gameManager != null)
                {
                    gameManager.AddGold(SELL_PRICE);
                }
                
                Debug.Log($"타워 전체 판매: 위치 ({row}, {col})");
            }
            
            return true;
        }

        public bool CanMixTower(int row, int col)
        {
            if (!IsValidPosition(row, col)) return false;
            
            TowerSlot slot = myTowerGrid[row, col];
            
            return !slot.IsEmpty && slot.IsFull && slot.towerTypeId < 5;
        }
        
        public bool SpawnGamblingTower(int towerTypeId)
        {
            Vector2Int bestSlot = FindBestSlotForPlayer(towerTypeId);
            if (bestSlot.x == -1)
            {
                Debug.Log("도박 타워 배치 가능한 슬롯 없음");
                return false;
            }
            
            PlaceTowerForPlayer(bestSlot.x, bestSlot.y, towerTypeId);
            Debug.Log($"도박 타워 생성 완료: 타입 {towerTypeId}, 위치 ({bestSlot.x}, {bestSlot.y})");
            return true;
        }
        
        public bool CanCombineMyth1()
        {
            return HasTowerType(1) && HasTowerType(3) && HasTowerType(5) && HasEmptySlot();
        }
        
        public bool CanCombineMyth2()
        {
            return HasTowerType(2) && HasTowerType(4) && HasTowerType(6) && HasEmptySlot();
        }
        
        private bool HasTowerType(int towerTypeId)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
                    if (!slot.IsEmpty && slot.towerTypeId == towerTypeId && slot.stackCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private bool HasEmptySlot()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (myTowerGrid[row, col].IsEmpty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool CombineMyth1()
        {
            if (!CanCombineMyth1()) return false;
            
            ConsumeTowerType(1);
            ConsumeTowerType(3);
            ConsumeTowerType(5);
            
            Vector2Int emptySlot = GetFirstEmptySlot();
            if (emptySlot.x == -1) return false;
            
            PlaceTowerForPlayer(emptySlot.x, emptySlot.y, 7);
            Debug.Log("신화 타워 7번 생성 완료");
            return true;
        }
        
        public bool CombineMyth2()
        {
            if (!CanCombineMyth2()) return false;
            
            ConsumeTowerType(2);
            ConsumeTowerType(4);
            ConsumeTowerType(6);
            
            Vector2Int emptySlot = GetFirstEmptySlot();
            if (emptySlot.x == -1) return false;
            
            PlaceTowerForPlayer(emptySlot.x, emptySlot.y, 8);
            Debug.Log("신화 타워 8번 생성 완료");
            return true;
        }
        
        private void ConsumeTowerType(int towerTypeId)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    TowerSlot slot = myTowerGrid[row, col];
                    if (!slot.IsEmpty && slot.towerTypeId == towerTypeId && slot.stackCount > 0)
                    {
                        if (slot.stackCount > 1)
                        {
                            slot.stackCount--;
                            slot.towerGroup.RemoveTower();
                        }
                        else
                        {
                            RemovePlayerTower(row, col);
                        }
                        return;
                    }
                }
            }
        }
        
        private Vector2Int GetFirstEmptySlot()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    if (myTowerGrid[row, col].IsEmpty)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            return new Vector2Int(-1, -1);
        }

        public bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < 3 && col >= 0 && col < 6;
        }

        public bool IsPlayerSlotEmpty(int row, int col)
        {
            if (!IsValidPosition(row, col)) return false;
            return myTowerGrid[row, col].IsEmpty;
        }

        public TowerGroup GetPlayerTowerGroup(int row, int col)
        {
            if (!IsValidPosition(row, col)) return null;
            return myTowerGrid[row, col].towerGroup;
        }

        public Vector3 GetPlayerSlotPosition(int row, int col)
        {
            if (!IsValidPosition(row, col)) return Vector3.zero;
            int index = row * 6 + col;
            return spawnPositions[index].position;
        }

        public Vector2Int GetSlotFromPosition(Vector3 position)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    Vector3 slotPosition = GetPlayerSlotPosition(row, col);
                    if (Vector3.Distance(position, slotPosition) < 0.1f)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            return new Vector2Int(-1, -1);
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