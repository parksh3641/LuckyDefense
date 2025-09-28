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
        
        [Header("References")]
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private GameManager gameManager;
        
        private Coroutine summonCoroutine;
        private bool isRunning = false;

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
                float currentInterval = GetCurrentSummonInterval();
                yield return new WaitForSeconds(currentInterval);

                if (aiActive)
                {
                    TryAISummonTower();
                }
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