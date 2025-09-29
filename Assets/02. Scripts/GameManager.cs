using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LuckyDefense
{
    public partial class GameManager : MonoBehaviour
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
        }

        public void AddGem(int amount)
        {
            myGem += amount;
            aiGem += amount;
        }
    }
}