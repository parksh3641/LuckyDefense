using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
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
        [SerializeField] private GameObject gameOverViewObject;
        [SerializeField] private GameObject gameWinViewObject;
        
        [Header("Buttons")]
        [SerializeField] private Button spawnBtn;
        [SerializeField] private Button upgradeViewBtn;
        [SerializeField] private Button gamblingViewBtn;
        [SerializeField] private Button miniBossSpawnBtn;
        
        [Header("Gambling Buttons")]
        [SerializeField] private Button gamblingNormalBtn;
        [SerializeField] private Button gamblingRareBtn;
        [SerializeField] private Button gamblingHeroBtn;
        
        [Header("Myth Buttons")]
        [SerializeField] private Button mythButton1;
        [SerializeField] private Button mythButton2;
        
        [Header("UI Animations")]
        [SerializeField] private CoinAnimation[] coinAnimation;
        [SerializeField] private CoinAnimation[] gemAnimation;
        
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
        [SerializeField] private float miniBossSpawnInterval = 60f;
        
        private float miniBossTimer = 0f;
        private bool miniBossTimerActive = false;

        private void Awake()
        {
            Instance = this;
            
            SetupButtonEvents();
            SetInitialViewState();
            
            waveUI.gameObject.SetActive(false);
            
            for (int i = 0; i < coinAnimation.Length; i++)
            {
                coinAnimation[i].gameObject.SetActive(false);
            }
            
            for (int i = 0; i < gemAnimation.Length; i++)
            {
                gemAnimation[i].gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            StartMiniBossTimer();
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
                UpdateMiniBossTimer();
                UpdateMythButtons();
            }
        }

        private void SetupButtonEvents()
        {
            spawnBtn.onClick.AddListener(() => towerManager.SpawnRandomTowerForPlayer());
            upgradeViewBtn.onClick.AddListener(ToggleUpgradeView);
            gamblingViewBtn.onClick.AddListener(ToggleGamblingView);
            miniBossSpawnBtn.onClick.AddListener(OnMiniBossButtonClicked);
    
            if (gamblingNormalBtn != null)
                gamblingNormalBtn.onClick.AddListener(OnGamblingNormalClicked);
    
            if (gamblingRareBtn != null)
                gamblingRareBtn.onClick.AddListener(OnGamblingRareClicked);
    
            if (gamblingHeroBtn != null)
                gamblingHeroBtn.onClick.AddListener(OnGamblingHeroClicked);
    
            if (mythButton1 != null)
                mythButton1.onClick.AddListener(OnMythButton1Clicked);
    
            if (mythButton2 != null)
                mythButton2.onClick.AddListener(OnMythButton2Clicked);

            miniBossSpawnBtn.gameObject.SetActive(false);
    
            if (mythButton1 != null)
                mythButton1.gameObject.SetActive(false);
    
            if (mythButton2 != null)
                mythButton2.gameObject.SetActive(false);
        }

        private void SetInitialViewState()
        {
            mainViewObject.SetActive(true);
            upgradeViewObject.SetActive(false);
            gamblingViewObject.SetActive(false);
            gameOverViewObject.SetActive(false);
            gameWinViewObject.SetActive(false);
        }
        
        public void ToggleUpgradeView()
        {
            upgradeViewObject.SetActive(!upgradeViewObject.activeInHierarchy);
        }

        public void ToggleGamblingView()
        {
            gamblingViewObject.SetActive(!gamblingViewObject.activeInHierarchy);
        }
        
        public void ShowGameOverUI()
        {
            if (gameOverViewObject != null)
            {
                gameOverViewObject.SetActive(true);
            }
        }

        public void ShowGameWinUI()
        {
            if (gameWinViewObject != null)
            {
                gameWinViewObject.SetActive(true);
            }
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
            int currentCount = towerManager.GetPlayerTotalTowerCount();
            int maxCount = gameManager.MyMaxUnitCount;
            string countString = $"{currentCount}/{maxCount}";
    
            Color textColor = currentCount >= maxCount ? Color.red : Color.white;
    
            for (int i = 0; i < maxCountText.Length; i++)
            {
                maxCountText[i].text = countString;
                maxCountText[i].color = textColor;
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
        
        private void UpdateMiniBossTimer()
        {
            if (!miniBossTimerActive) return;

            bool wasMiniBossAlive = IsMiniBossAlive();
    
            miniBossTimer -= 0.1f;

            if (miniBossTimer <= 0f)
            {
                if (!IsMiniBossAlive())
                {
                    miniBossSpawnBtn.gameObject.SetActive(true);
                    miniBossTimerActive = false;
                }
                else
                {
                    miniBossTimer = miniBossSpawnInterval;
                }
            }
    
            if (wasMiniBossAlive && !IsMiniBossAlive())
            {
                miniBossTimer = miniBossSpawnInterval;
            }
        }
        
        private void OnMiniBossButtonClicked()
        {
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.SpawnMiniBoss();
            }
    
            miniBossSpawnBtn.gameObject.SetActive(false);
            miniBossTimer = miniBossSpawnInterval;
        }

        private bool IsMiniBossAlive()
        {
            GameObject[] miniBosses = GameObject.FindGameObjectsWithTag("MiniBoss");
            return miniBosses != null && miniBosses.Length > 0;
        }
        
        public void StartMiniBossTimer()
        {
            miniBossTimerActive = true;
            miniBossTimer = miniBossSpawnInterval;
            miniBossSpawnBtn.gameObject.SetActive(false);
        }

        public void RetryButtonClicked()
        {
            SceneManager.LoadScene("MainScene");
        }

        public void PlayGoldAnimation(int amount)
        {
            for (int i = 0; i < coinAnimation.Length; i++)
            {
                if (coinAnimation[i] != null)
                {
                    coinAnimation[i].gameObject.SetActive(false);
                    coinAnimation[i].gameObject.SetActive(true);
                    coinAnimation[i].Initialize(amount);
                }
            }
        }

        public void PlayGemAnimation(int amount)
        {
            for (int i = 0; i < gemAnimation.Length; i++)
            {
                if (gemAnimation[i] != null)
                {
                    gemAnimation[i].gameObject.SetActive(false);
                    gemAnimation[i].gameObject.SetActive(true);
                    gemAnimation[i].Initialize(amount);
                }
            }
        }
        
        private void OnGamblingNormalClicked()
        {
            if (gameManager != null)
            {
                bool success = gameManager.GambleNormalTower();
                if (success)
                {
                    Debug.Log("Normal 도박 성공!");
                }
                else
                {
                    Debug.Log("Normal 도박 실패 또는 젬 부족");
                }
            }
        }

        private void OnGamblingRareClicked()
        {
            if (gameManager != null)
            {
                bool success = gameManager.GambleRareTower();
                if (success)
                {
                    Debug.Log("Rare 도박 성공!");
                }
                else
                {
                    Debug.Log("Rare 도박 실패");
                }
            }
        }

        private void OnGamblingHeroClicked()
        {
            if (gameManager != null)
            {
                bool success = gameManager.GambleHeroTower();
                if (success)
                {
                    Debug.Log("Hero 도박 성공!");
                }
                else
                {
                    Debug.Log("Hero 도박 실패");
                }
            }
        }
        
        private void UpdateMythButtons()
        {
            if (towerManager == null) return;
    
            bool canCombineMyth1 = towerManager.CanCombineMyth1();
            bool canCombineMyth2 = towerManager.CanCombineMyth2();
    
            if (mythButton1 != null)
            {
                mythButton1.gameObject.SetActive(canCombineMyth1);
            }
    
            if (mythButton2 != null)
            {
                mythButton2.gameObject.SetActive(canCombineMyth2);
            }
        }
        
        private void OnMythButton1Clicked()
        {
            if (towerManager != null)
            {
                bool success = towerManager.CombineMyth1();
                if (success)
                {
                    Debug.Log("신화 타워 1번 합성 성공!");
                }
            }
        }

        private void OnMythButton2Clicked()
        {
            if (towerManager != null)
            {
                bool success = towerManager.CombineMyth2();
                if (success)
                {
                    Debug.Log("신화 타워 2번 합성 성공!");
                }
            }
        }
    }
}