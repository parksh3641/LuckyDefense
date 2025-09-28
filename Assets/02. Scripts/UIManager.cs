using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace LuckyDefense
{
    public class UIManager : MonoBehaviour
    {
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

        [SerializeField] private TextMeshProUGUI monstersText;
        [SerializeField] private Image monstersFillAmount;
        
        [Header("Managers")]
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private GameManager gameManager;

        private void Awake()
        {
            SetupButtonEvents();
            SetInitialViewState();
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
    }
}