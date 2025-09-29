using UnityEngine;
using UnityEngine.UI;

namespace LuckyDefense
{
    public class TowerUIManager : MonoBehaviour
    {
        [SerializeField] private TowerMixButton towerMixButton;
        [SerializeField] private TowerSellButton towerSellButton;
        [SerializeField] private ObjectDetector objectDetector;
        
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            
            if (towerMixButton != null)
            {
                towerMixButton.gameObject.SetActive(false);
                
                Button mixBtn = towerMixButton.GetComponent<Button>();
                if (mixBtn != null && objectDetector != null)
                {
                    mixBtn.onClick.AddListener(() => objectDetector.OnMixButtonClicked());
                }
            }
            
            if (towerSellButton != null)
            {
                towerSellButton.gameObject.SetActive(false);
                
                Button sellBtn = towerSellButton.GetComponent<Button>();
                if (sellBtn != null && objectDetector != null)
                {
                    sellBtn.onClick.AddListener(() => objectDetector.OnSellButtonClicked());
                }
            }
            
            if (objectDetector == null)
            {
                objectDetector = FindObjectOfType<ObjectDetector>();
            }
        }

        public void OpenTowerMix(Transform towerTransform)
        {
            if (towerMixButton == null) return;
            
            towerMixButton.gameObject.SetActive(true);
            
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(towerTransform.position);
            towerMixButton.Setup(screenPosition);
        }

        public void CloseTowerMix()
        {
            if (towerMixButton != null)
            {
                towerMixButton.gameObject.SetActive(false);
            }
        }

        public void OpenTowerSell(Transform towerTransform)
        {
            if (towerSellButton == null) return;
            
            towerSellButton.gameObject.SetActive(true);
            
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(towerTransform.position);
            towerSellButton.Setup(screenPosition);
        }

        public void CloseTowerSell()
        {
            if (towerSellButton != null)
            {
                towerSellButton.gameObject.SetActive(false);
            }
        }
    }
}