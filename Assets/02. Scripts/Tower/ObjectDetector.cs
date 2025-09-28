using UnityEngine;
using UnityEngine.EventSystems;

namespace LuckyDefense
{
    public class ObjectDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private LayerMask towerLayerMask = -1;
        
        [Header("UI References")]
        [SerializeField] private TowerAttackRange towerAttackRange;
        
        [Header("Selection Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Material selectionMaterial;
        
        private Camera mainCamera;
        private TowerGroup selectedTowerGroup;
        private TowerWeapon selectedTowerWeapon;
        private GameObject currentSelectionIndicator;
        private Material originalMaterial;
        private Renderer selectedRenderer;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
        }

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (towerAttackRange == null)
                towerAttackRange = FindObjectOfType<TowerAttackRange>();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return;
                
                HandleMouseClick();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
        }

        private bool IsPointerOverUI()
        {
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            else
                return EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleMouseClick()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                HandleObjectHit(hit);
            }
            else
            {
                ClearSelection();
            }
        }

        private void HandleObjectHit(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Tower"))
            {
                TowerGroup towerGroup = hit.transform.GetComponent<TowerGroup>();
                if (towerGroup != null)
                {
                    SelectTowerGroup(towerGroup);
                }
                else
                {
                    ClearSelection();
                }
            }
            else
            {
                ClearSelection();
            }
        }

        private void SelectTowerGroup(TowerGroup towerGroup)
        {
            if (selectedTowerGroup == towerGroup) return;
            
            ClearSelection();
            
            selectedTowerGroup = towerGroup;
            
            ShowSelectionVisual();
            ShowTowerAttackRange();
            
            Debug.Log($"타워 그룹 선택: {towerGroup.name}, 타입: {towerGroup.TowerTypeId}, 스택: {towerGroup.CurrentStackCount}");
        }

        private void ShowTowerAttackRange()
        {
            if (selectedTowerGroup == null || towerAttackRange == null) return;
            
            int towerTypeId = selectedTowerGroup.TowerTypeId;
            float attackRange = GetTowerRangeFromCSV(towerTypeId);
            
            if (attackRange > 0)
            {
                towerAttackRange.OnAttackRange(selectedTowerGroup.transform.position, attackRange);
                Debug.Log($"타워 사거리 표시: ID {towerTypeId}, Range {attackRange}");
            }
        }

        private float GetTowerRangeFromCSV(int towerTypeId)
        {
            var csvManager = CSVLoadManager.Instance;
            if (csvManager == null)
            {
                Debug.LogError("CSVLoadManager를 찾을 수 없습니다.");
                return 0f;
            }
            
            var towerData = csvManager.GetTowerData(towerTypeId);
            if (towerData != null)
            {
                return towerData.Range;
            }
            else
            {
                Debug.LogWarning($"타워 데이터를 찾을 수 없습니다. ID: {towerTypeId}");
                return 3f;
            }
        }

        private void ShowSelectionVisual()
        {
            Transform targetTransform = selectedTowerGroup != null ? selectedTowerGroup.transform : selectedTowerWeapon.transform;
            if (targetTransform == null) return;
            
            if (selectionIndicator != null)
            {
                currentSelectionIndicator = Instantiate(selectionIndicator, targetTransform.position, Quaternion.identity);
                currentSelectionIndicator.transform.SetParent(targetTransform);
            }
            
            if (selectionMaterial != null)
            {
                selectedRenderer = targetTransform.GetComponent<Renderer>();
                if (selectedRenderer != null)
                {
                    originalMaterial = selectedRenderer.material;
                    selectedRenderer.material = selectionMaterial;
                }
            }
        }

        private void ClearSelection()
        {
            towerAttackRange.OffAttackRange();
            HideSelectionVisual();
            
            selectedTowerGroup = null;
            selectedTowerWeapon = null;
            
            Debug.Log("타워 선택 해제");
        }
        
        private void HideSelectionVisual()
        {
            if (currentSelectionIndicator != null)
            {
                Destroy(currentSelectionIndicator);
                currentSelectionIndicator = null;
            }
            
            if (selectedRenderer != null && originalMaterial != null)
            {
                selectedRenderer.material = originalMaterial;
                selectedRenderer = null;
                originalMaterial = null;
            }
        }

        public void UpgradeSelectedTower()
        {
            if (selectedTowerGroup != null)
            {
                selectedTowerGroup.UpgradeAllTowers();
                Debug.Log($"타워 그룹 업그레이드: {selectedTowerGroup.name}");
            }
            else if (selectedTowerWeapon != null)
            {
                selectedTowerWeapon.UpgradeWeapon();
                Debug.Log($"타워 무기 업그레이드: {selectedTowerWeapon.name}");
            }
        }

        public void RemoveSelectedTower()
        {
            if (selectedTowerGroup == null && selectedTowerWeapon == null) return;
            
            TowerManager towerManager = FindObjectOfType<TowerManager>();
            if (towerManager == null) return;
            
            Vector3 towerPosition = selectedTowerGroup != null ? 
                selectedTowerGroup.transform.position : 
                selectedTowerWeapon.transform.position;
            
            Transform[] playerPositions = towerManager.transform.GetComponentsInChildren<Transform>();
            
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int index = row * 6 + col;
                    if (index < playerPositions.Length - 1)
                    {
                        Transform posTransform = playerPositions[index + 1];
                        if (Vector3.Distance(towerPosition, posTransform.position) < 0.5f)
                        {
                            towerManager.RemovePlayerTower(row, col);
                            ClearSelection();
                            Debug.Log($"타워 제거: 위치 ({row}, {col})");
                            return;
                        }
                    }
                }
            }
        }

        public TowerGroup GetSelectedTowerGroup()
        {
            return selectedTowerGroup;
        }

        public TowerWeapon GetSelectedTowerWeapon()
        {
            return selectedTowerWeapon;
        }

        public bool HasSelectedTower()
        {
            return selectedTowerGroup != null || selectedTowerWeapon != null;
        }

        public void ForceDeselect()
        {
            ClearSelection();
        }

        private void OnDestroy()
        {
            HideSelectionVisual();
        }
    }
}