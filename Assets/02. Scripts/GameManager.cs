using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LuckyDefense
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        
        [SerializeField] private int myGold = 100;
        [SerializeField] private int myGem = 0;
        [SerializeField] private int myMaxUnitCount = 25;
        [SerializeField] private int mySummonCost = 0;

        private const float spawnNormalPercent = 97.44f;
        private const float spawnRarePercent = 1.97f;
        private const float spawnHeroPercent = 0.5f;
        
        private const float gamblingNormalPercent = 60f;
        private const float gamblingRarePercent = 20f;
        private const float gamblingHeroPercent = 10f;

        private const int gamblingNormalGemCost = 1;
        private const int gamblingRareGemCost = 1;
        private const int gamblingHeroGemCost = 2;
        
        [SerializeField] private int aiGold = 100;
        [SerializeField] private int aiGem = 0;
        [SerializeField] private int aiMaxUnitCount = 25;
        [SerializeField] private int aiSummonCost = 0;
        
        public float SpawanNormalPercent => spawnNormalPercent;
        public float SpawnRarePercent => spawnRarePercent;
        public float SpawnHeroPercent => spawnHeroPercent;
        
        public float GamblingNormalPercent => gamblingNormalPercent;
        public float GamblingRarePercent => gamblingRarePercent;
        public float GamblingHeroPercent => gamblingHeroPercent;
        
        public int MyGold { get => myGold; set => myGold = value; }
        public int MyGem { get => myGem; set => myGem = value; }
        public int MyMaxUnitCount { get => myMaxUnitCount; set => myMaxUnitCount = value; }
        public int MySummonCost { get => mySummonCost; set => mySummonCost = value; }
        public int AIGold { get => aiGold; set => aiGold = value; }
        public int AISummonCost { get => aiSummonCost; set => aiSummonCost = value; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartGame();
            }
        }

        private void InitializeGameData()
        {
            var csvManager = CSVLoadManager.Instance;
            if (csvManager != null)
            {
                var defaultData = csvManager.GetDefaultData();
                if (defaultData != null)
                {
                    myGold = defaultData.First_Money;
                    aiGold = defaultData.First_Money;
                }
                
                var initialSummonCost = csvManager.GetSummonCostData(1);
                if (initialSummonCost != null)
                {
                    mySummonCost = initialSummonCost.Summon_cost_Gold;
                    aiSummonCost = initialSummonCost.Summon_cost_Gold;
                }
            }
            
            Debug.Log($"GameManager 초기화 완료 - 골드: {myGold}, 젬: {myGem}, 최대유닛: {myMaxUnitCount}");
        }
        
        public void UpdatePlayerSummonCost(int newCost)
        {
            mySummonCost = newCost;
        }

        public void UpdateAISummonCost(int newCost)
        {
            aiSummonCost = newCost;
        }

        public int GetAISummonCost()
        {
            return aiSummonCost;
        }

        public void AddGold(int amount)
        {
            myGold += amount;
            aiGold += amount;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayGoldAnimation(amount);
            }
        }

        public void AddGem(int amount)
        {
            myGem += amount;
            aiGem += amount;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayGemAnimation(amount);
            }
        }
        
        public bool GambleNormalTower()
        {
            if (myGem < gamblingNormalGemCost)
            {
                Debug.Log($"젬 부족: 필요 {gamblingNormalGemCost}, 보유 {myGem}");
                return false;
            }
            
            myGem -= gamblingNormalGemCost;
            
            float randomValue = UnityEngine.Random.Range(0f, 100f);
            
            if (randomValue < gamblingNormalPercent)
            {
                int normalTypeId = UnityEngine.Random.Range(1, 3);
                return SpawnGamblingTower(normalTypeId);
            }
            return false;
        }

        public bool GambleRareTower()
        {
            if (myGem < gamblingRareGemCost)
            {
                Debug.Log($"젬 부족: 필요 {gamblingRareGemCost}, 보유 {myGem}");
                return false;
            }
            
            myGem -= gamblingRareGemCost;
            
            float randomValue = UnityEngine.Random.Range(0f, 100f);
            
            if (randomValue < gamblingRarePercent)
            {
                int rareTypeId = UnityEngine.Random.Range(3, 5);
                return SpawnGamblingTower(rareTypeId);
            }
            return false;
        }

        public bool GambleHeroTower()
        {
            if (myGem < gamblingHeroGemCost)
            {
                Debug.Log($"젬 부족: 필요 {gamblingHeroGemCost}, 보유 {myGem}");
                return false;
            }
            
            myGem -= gamblingHeroGemCost;
            
            float randomValue = UnityEngine.Random.Range(0f, 100f);
            
            if (randomValue < gamblingHeroPercent)
            {
                int heroTypeId = UnityEngine.Random.Range(5, 7);
                return SpawnGamblingTower(heroTypeId);
            }
            return false;
        }

        private bool SpawnGamblingTower(int towerTypeId)
        {
            TowerManager towerManager = FindObjectOfType<TowerManager>();
            if (towerManager == null) return false;
            
            return towerManager.SpawnGamblingTower(towerTypeId);
        }
    }
}