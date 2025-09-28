using System.Collections;
using UnityEngine;

namespace LuckyDefense
{
    public partial class GameManager
    {
        [Header("AI Settings")]
        [SerializeField] private TowerManager aiTowerManager;
        [SerializeField] private bool enableAI = true;
        [SerializeField] private float aiSpawnInterval = 3f;
        
        private Coroutine aiCoroutine;

        private void InitializeAI()
        {
            if (enableAI && aiTowerManager != null)
            {
                var csvManager = CSVLoadManager.Instance;
                if (csvManager != null)
                {
                    var defaultData = csvManager.GetDefaultData();
                    if (defaultData != null)
                    {
                        aiGold = defaultData.First_Money;
                    }
                }
                
                aiTowerManager.SetupAsAI();
                StartAI();
            }
        }

        private void StartAI()
        {
            if (aiCoroutine == null)
            {
                aiCoroutine = StartCoroutine(AISpawnCoroutine());
            }
        }

        private void StopAI()
        {
            if (aiCoroutine != null)
            {
                StopCoroutine(aiCoroutine);
                aiCoroutine = null;
            }
        }

        private IEnumerator AISpawnCoroutine()
        {
            var waitTime = new WaitForSeconds(aiSpawnInterval);
            
            while (enableAI)
            {
                yield return waitTime;
                
                if (aiTowerManager != null && CanAISpawn())
                {
                    aiTowerManager.AISpawnTower();
                }
            }
        }

        private bool CanAISpawn()
        {
            int nextCost = aiTowerManager.GetNextSummonCost();
            return AIGold >= nextCost;
        }

        public void SetAIEnabled(bool enabled)
        {
            enableAI = enabled;
            
            if (enabled)
            {
                StartAI();
            }
            else
            {
                StopAI();
            }
        }

        public void SetAISpawnInterval(float interval)
        {
            aiSpawnInterval = Mathf.Max(1f, interval);
        }

        public void CallInitializeAI()
        {
            InitializeAI();
        }
    }
}