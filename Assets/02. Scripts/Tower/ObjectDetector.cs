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
        
        [Header("Drag Settings")]
        [SerializeField] private LineRenderer dragLineRenderer;
        [SerializeField] private float dragMinDistance = 0.3f;
        [SerializeField] private float snapDistance = 0.5f;
        
        private Camera mainCamera;
        private TowerGroup selectedTowerGroup;
        private TowerWeapon selectedTowerWeapon;
        private GameObject currentSelectionIndicator;
        private Material originalMaterial;
        private Renderer selectedRenderer;
        
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        private Vector3 currentDragPosition;
        private Vector2Int dragStartSlot = new Vector2Int(-1, -1);
        private Vector2Int targetSlot = new Vector2Int(-1, -1);

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
                
            InitializeDragLine();
        }

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeDragLine()
        {
            if (dragLineRenderer == null)
            {
                GameObject lineObj = new GameObject("DragLine");
                dragLineRenderer = lineObj.AddComponent<LineRenderer>();
                dragLineRenderer.startWidth = 0.1f;
                dragLineRenderer.endWidth = 0.1f;
                dragLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                dragLineRenderer.startColor = Color.white;
                dragLineRenderer.endColor = Color.white;
                dragLineRenderer.positionCount = 2;
                dragLineRenderer.enabled = false;
            }
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
                HandleMouseDown();
            }
            
            if (isDragging && selectedTowerGroup != null)
            {
                HandleMouseDrag();
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                if (isDragging)
                {
                    HandleMouseUp();
                }
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

        private void HandleMouseDown()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.transform.CompareTag("Tower"))
                {
                    TowerGroup towerGroup = hit.transform.GetComponent<TowerGroup>();
                    if (towerGroup != null)
                    {
                        SelectTowerAndStartDrag(towerGroup);
                        return;
                    }
                }
            }
            
            ClearSelection();
        }

        private void SelectTowerAndStartDrag(TowerGroup towerGroup)
        {
            ClearSelection();
            
            selectedTowerGroup = towerGroup;
            dragStartPosition = towerGroup.transform.position;
            currentDragPosition = dragStartPosition;
            dragStartSlot = GetSlotFromPosition(dragStartPosition);
            
            isDragging = true;
            
            ShowSelectionVisual();
            ShowTowerAttackRange();
            StartDragLine();
            
            Debug.Log($"타워 선택 및 드래그 시작: {towerGroup.name}, 슬롯: ({dragStartSlot.x}, {dragStartSlot.y})");
        }

        private void StartDragLine()
        {
            dragLineRenderer.enabled = true;
            Vector3 lineStart = new Vector3(dragStartPosition.x, dragStartPosition.y, -1);
            dragLineRenderer.SetPosition(0, lineStart);
            dragLineRenderer.SetPosition(1, lineStart);
        }

        private void HandleMouseDrag()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            currentDragPosition = mouseWorldPos;
            
            FindClosestSlot(mouseWorldPos);
            
            Vector3 endPosition = targetSlot.x != -1 ? GetSlotPosition(targetSlot) : mouseWorldPos;
            endPosition.z = -1;
            
            dragLineRenderer.SetPosition(0, new Vector3(dragStartPosition.x, dragStartPosition.y, -1));
            dragLineRenderer.SetPosition(1, endPosition);
        }

        private void FindClosestSlot(Vector3 mousePosition)
        {
            targetSlot = new Vector2Int(-1, -1);
            float closestDistance = float.MaxValue;
            
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    Vector3 slotPosition = GetSlotPosition(new Vector2Int(row, col));
                    float distance = Vector2.Distance(
                        new Vector2(mousePosition.x, mousePosition.y),
                        new Vector2(slotPosition.x, slotPosition.y));
                    
                    if (distance < closestDistance && distance < snapDistance)
                    {
                        closestDistance = distance;
                        targetSlot = new Vector2Int(row, col);
                    }
                }
            }
        }

        private void HandleMouseUp()
        {
            dragLineRenderer.enabled = false;
            
            if (!isDragging || selectedTowerGroup == null)
            {
                isDragging = false;
                return;
            }
            
            float dragDistance = Vector3.Distance(dragStartPosition, currentDragPosition);
            
            if (dragDistance < dragMinDistance)
            {
                isDragging = false;
                return;
            }
            
            if (targetSlot.x != -1 && targetSlot.y != -1)
            {
                PerformTowerAction();
            }
            
            isDragging = false;
        }

        private void PerformTowerAction()
        {
            TowerManager towerManager = FindObjectOfType<TowerManager>();
            if (towerManager == null) return;
            
            if (dragStartSlot == targetSlot) return;
            
            if (towerManager.IsPlayerSlotEmpty(targetSlot.x, targetSlot.y))
            {
                towerManager.MovePlayerTower(dragStartSlot.x, dragStartSlot.y, targetSlot.x, targetSlot.y);
                Debug.Log($"타워 이동: ({dragStartSlot.x},{dragStartSlot.y}) -> ({targetSlot.x},{targetSlot.y})");
            }
            else
            {
                towerManager.SwapPlayerTowers(dragStartSlot.x, dragStartSlot.y, targetSlot.x, targetSlot.y);
                Debug.Log($"타워 교환: ({dragStartSlot.x},{dragStartSlot.y}) <-> ({targetSlot.x},{targetSlot.y})");
            }
            
            ClearSelection();
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = mainCamera.WorldToScreenPoint(dragStartPosition).z;
            return mainCamera.ScreenToWorldPoint(mousePos);
        }

        private Vector2Int GetSlotFromPosition(Vector3 position)
        {
            TowerManager towerManager = FindObjectOfType<TowerManager>();
            if (towerManager == null) return new Vector2Int(-1, -1);
            
            return towerManager.GetSlotFromPosition(position);
        }

        private Vector3 GetSlotPosition(Vector2Int slot)
        {
            TowerManager towerManager = FindObjectOfType<TowerManager>();
            if (towerManager == null) return Vector3.zero;
            
            return towerManager.GetPlayerSlotPosition(slot.x, slot.y);
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
            }
        }

        private float GetTowerRangeFromCSV(int towerTypeId)
        {
            var csvManager = CSVLoadManager.Instance;
            if (csvManager == null)
            {
                Debug.LogError("CSVLoadManager를 찾을 수 없습니다.");
                return 3f;
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
            if (towerAttackRange != null)
                towerAttackRange.OffAttackRange();
                
            HideSelectionVisual();
            
            selectedTowerGroup = null;
            selectedTowerWeapon = null;
            
            if (dragLineRenderer != null)
                dragLineRenderer.enabled = false;
                
            isDragging = false;
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
            
            Vector2Int slot = GetSlotFromPosition(towerPosition);
            if (slot.x != -1 && slot.y != -1)
            {
                towerManager.RemovePlayerTower(slot.x, slot.y);
                ClearSelection();
                Debug.Log($"타워 제거: 위치 ({slot.x}, {slot.y})");
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
            if (dragLineRenderer != null)
                dragLineRenderer.enabled = false;
        }
    }
}