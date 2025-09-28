using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace LuckyDefense
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        
        [Header("View Objects")]
        [SerializeField] private GameObject mainViewObject;
        [SerializeField] private GameObject upgradeViewObject;
        [SerializeField] private GameObject gamblingViewObject;
        
        [Header("Buttons")]
        [SerializeField] private Button spawnBtn;
        [SerializeField] private Button upgradeViewBtn;
        [SerializeField] private Button gamblingViewBtn;
        
        [Header("UI Text")]
        [SerializeField] private TextMeshProUGUI[] goldText;
        [SerializeField] private TextMeshProUGUI[] gemText;
        [SerializeField] private TextMeshProUGUI[] maxCountText;
        [SerializeField] private TextMeshProUGUI summonCostText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI waveTimerText;
        [SerializeField] private WaveUI waveUI;

        [Header("Enemy Count UI")]
        [SerializeField] private TextMeshProUGUI totalEnemyCountText;
        [SerializeField] private Image totalEnemyFillamount;
        
        [Header("Managers")]
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private GameManager gameManager;
        
        [Header("Enemy Settings")]
        [SerializeField] private int maxEnemyCount = 100;

        private void Awake()
        {
            Instance = this;
            
            SetupButtonEvents();
            SetInitialViewState();
            
            waveUI.gameObject.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(UpdateUICoroutine());
        }

        private void OnDestroy()
        {
            spawnBtn.onClick.RemoveAllListeners();
            upgradeViewBtn.onClick.RemoveAllListeners();
            gamblingViewBtn.onClick.RemoveAllListeners();
        }

        private IEnumerator UpdateUICoroutine()
        {
            var waitTime = new WaitForSeconds(0.1f);
            
            while (true)
            {
                yield return waitTime;
                UpdateAllUI();
            }
        }

        private void SetupButtonEvents()
        {
            spawnBtn.onClick.AddListener(() => towerManager.SpawnRandomTowerForPlayer());
            upgradeViewBtn.onClick.AddListener(ToggleUpgradeView);
            gamblingViewBtn.onClick.AddListener(ToggleGamblingView);
        }

        private void SetInitialViewState()
        {
            mainViewObject.SetActive(true);
            upgradeViewObject.SetActive(false);
            gamblingViewObject.SetActive(false);
        }
        
        public void ToggleUpgradeView()
        {
            upgradeViewObject.SetActive(!upgradeViewObject.activeInHierarchy);
        }

        public void ToggleGamblingView()
        {
            gamblingViewObject.SetActive(!gamblingViewObject.activeInHierarchy);
        }

        private void UpdateAllUI()
        {
            UpdateGoldUI();
            UpdateGemUI();
            UpdateMaxCountUI();
            UpdateSummonCostUI();
            UpdateWaveUI();
            UpdateEnemyCountUI();
        }

        private void UpdateGoldUI()
        {
            int currentGold = gameManager.MyGold;

            for (int i = 0; i < goldText.Length; i++)
            {
                goldText[i].text = currentGold.ToString();
            }
        }

        private void UpdateGemUI()
        {
            string gemString = gameManager.MyGem.ToString();
            
            for (int i = 0; i < gemText.Length; i++)
            {
                gemText[i].text = gemString;
            }
        }

        private void UpdateMaxCountUI()
        {
            int currentCount = towerManager.GetTotalTowerCount();
            string countString = $"{currentCount}/{gameManager.MyMaxUnitCount}";
            
            for (int i = 0; i < maxCountText.Length; i++)
            {
                maxCountText[i].text = countString;
            }
        }

        private void UpdateSummonCostUI()
        {
            int cost = towerManager.GetNextSummonCost();
            bool canAfford = gameManager.MyGold >= cost;
            
            summonCostText.text = cost.ToString();
            summonCostText.color = canAfford ? Color.white : Color.red;
        }

        private void UpdateWaveUI()
        {
            if (WaveManager.Instance == null) return;

            if (waveText != null)
            {
                waveText.text = WaveManager.Instance.GetWaveString();
            }

            if (waveTimerText != null)
            {
                waveTimerText.text = WaveManager.Instance.GetWaveTimeString();
            }
        }

        private void UpdateEnemyCountUI()
        {
            int currentEnemyCount = GetCurrentEnemyCount();
            
            if (totalEnemyCountText != null)
            {
                totalEnemyCountText.text = $"{currentEnemyCount} / {maxEnemyCount}";
            }
            
            if (totalEnemyFillamount != null)
            {
                float fillRatio = (float)currentEnemyCount / maxEnemyCount;
                totalEnemyFillamount.fillAmount = fillRatio;
            }
        }

        private int GetCurrentEnemyCount()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
            
            return enemies.Length + monsters.Length;
        }

        public void SetMaxEnemyCount(int newMaxCount)
        {
            maxEnemyCount = newMaxCount;
        }

        public int GetMaxEnemyCount()
        {
            return maxEnemyCount;
        }

        public float GetEnemyCountRatio()
        {
            return (float)GetCurrentEnemyCount() / maxEnemyCount;
        }

        public bool IsEnemyCountAtMax()
        {
            return GetCurrentEnemyCount() >= maxEnemyCount;
        }

        public void ShowWaveUI(int number)
        {
            waveUI.gameObject.SetActive(true);
            waveUI.SetWaveText(number);
        }
    }
}