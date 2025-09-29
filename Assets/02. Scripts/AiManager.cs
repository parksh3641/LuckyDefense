using UnityEngine;
using System.Collections;

namespace LuckyDefense
{
    public class AiManager : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float summonInterval = 3.0f;
        [SerializeField] private float minSummonInterval = 1.0f;
        [SerializeField] private float maxSummonInterval = 3.0f;
        [SerializeField] private bool enableRandomInterval = true;
        [SerializeField] private bool aiActive = true;
        [SerializeField] private float actionDelay = 1.0f;
        
        [Header("References")]
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private GameManager gameManager;
        
        private Coroutine summonCoroutine;
        private bool isRunning = false;
        private int gamblingRotation = 0;

        private void Start()
        {
            InitializeAI();
        }

        private void InitializeAI()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;
            
            if (towerManager == null)
                towerManager = FindObjectOfType<TowerManager>();
            
            if (aiActive)
                StartAI();
        }

        public void StartAI()
        {
            if (!isRunning && aiActive)
            {
                isRunning = true;
                summonCoroutine = StartCoroutine(AITowerSummonRoutine());
                Debug.Log("AI 자동 소환 시작");
            }
        }

        public void StopAI()
        {
            if (isRunning)
            {
                isRunning = false;
                if (summonCoroutine != null)
                {
                    StopCoroutine(summonCoroutine);
                    summonCoroutine = null;
                }
                Debug.Log("AI 자동 소환 정지");
            }
        }

        private IEnumerator AITowerSummonRoutine()
        {
            while (isRunning && aiActive)
            {
                if (aiActive)
                {
                    TryAIGambling();
                    
                    yield return new WaitForSeconds(actionDelay);
                    
                    TryAISummonTower();
                    
                    yield return new WaitForSeconds(actionDelay);
                    
                    TryAIMythCombine();
                    TryAINormalCombine();
                }
                
                float currentInterval = GetCurrentSummonInterval();
                yield return new WaitForSeconds(currentInterval);
            }
        }

        private float GetCurrentSummonInterval()
        {
            if (enableRandomInterval)
            {
                return Random.Range(minSummonInterval, maxSummonInterval);
            }
            return summonInterval;
        }

        private bool CanAISummon()
        {
            if (towerManager == null || gameManager == null)
                return false;

            int currentCost = GetAICurrentSummonCost();
            int currentGold = gameManager.AIGold;
            bool hasGold = currentGold >= currentCost;
            bool hasSlot = HasAvailableSlotForAI();
            
            return hasGold && hasSlot;
        }

        private bool HasAvailableSlotForAI()
        {
            return towerManager.GetAITotalTowerCount() < 18;
        }

        private int GetAICurrentSummonCost()
        {
            return gameManager.GetAISummonCost();
        }

        private void TryAISummonTower()
        {
            if (towerManager == null || gameManager == null)
                return;

            bool success = towerManager.SpawnRandomTowerForAI();
            if (success)
            {
                Debug.Log($"AI 타워 소환 성공 - 현재 골드: {gameManager.AIGold}, AI 타워 수: {towerManager.GetAITotalTowerCount()}");
            }
        }
        
        private void TryAIMythCombine()
        {
            if (towerManager == null) return;
            
            if (towerManager.CanCombineAIMyth1())
            {
                bool success = towerManager.CombineAIMyth1();
                if (success)
                {
                    Debug.Log("AI 신화 타워 7번 합성 성공!");
                }
            }
            else if (towerManager.CanCombineAIMyth2())
            {
                bool success = towerManager.CombineAIMyth2();
                if (success)
                {
                    Debug.Log("AI 신화 타워 8번 합성 성공!");
                }
            }
        }
        
        private void TryAINormalCombine()
        {
            if (towerManager == null) return;
            
            for (int towerType = 1; towerType <= 4; towerType++)
            {
                if (GetAITowerTypeCount(towerType) >= 4)
                {
                    Vector2Int slot = FindAIFullStackSlot(towerType);
                    if (slot.x != -1)
                    {
                        bool success = towerManager.MixAITower(slot.x, slot.y);
                        if (success)
                        {
                            Debug.Log($"AI 일반 합성 성공: 타입 {towerType}");
                            return;
                        }
                    }
                }
            }
        }
        
        private int GetAITowerTypeCount(int towerTypeId)
        {
            int count = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    var slot = towerManager.GetAITowerSlot(row, col);
                    if (slot != null && slot.towerTypeId == towerTypeId)
                    {
                        count += slot.stackCount;
                    }
                }
            }
            return count;
        }
        
        private Vector2Int FindAIFullStackSlot(int towerTypeId)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    var slot = towerManager.GetAITowerSlot(row, col);
                    if (slot != null && slot.towerTypeId == towerTypeId && slot.IsFull)
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            return new Vector2Int(-1, -1);
        }
        
        private void TryAIGambling()
        {
            if (gameManager == null) return;
            
            gamblingRotation = (gamblingRotation + 1) % 3;
            
            switch (gamblingRotation)
            {
                case 0:
                    gameManager.AIGambleNormalTower();
                    break;
                case 1:
                    gameManager.AIGambleRareTower();
                    break;
                case 2:
                    gameManager.AIGambleHeroTower();
                    break;
            }
        }
        
        private void OnDestroy()
        {
            StopAI();
        }

        private void OnDisable()
        {
            StopAI();
        }
    }
}