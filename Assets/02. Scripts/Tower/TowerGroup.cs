using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace LuckyDefense
{
    public enum TowerRank
    {
        Normal = 1,
        Rare = 2,
        Hero = 3,
        Legendary = 4
    }

    public class TowerGroup : MonoBehaviour
    {
        [Header("Tower Group Settings")]
        [SerializeField] private int towerTypeId;
        [SerializeField] private int maxStackCount = 3;
        [SerializeField] private SpriteRenderer rankImage;
        
        [Header("Tower Weapons")]
        [SerializeField] private TowerWeapon[] towerWeapons;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem stackUpEffect;
        [SerializeField] private ParticleSystem upgradeEffect;
        
        [Header("Rank Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color heroColor = new Color(0.5f, 0f, 1f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f, 1f);
        
        private int currentStackCount = 0;
        private TowerData towerData;
        private TowerRank currentRank;
        
        public int TowerTypeId => towerTypeId;
        public int CurrentStackCount => currentStackCount;
        public int MaxStackCount => maxStackCount;
        public bool CanAddStack => currentStackCount < maxStackCount;
        public bool IsEmpty => currentStackCount == 0;
        public List<TowerWeapon> ActiveWeapons => towerWeapons.Take(currentStackCount).ToList();
        public TowerRank CurrentRank => currentRank;

        public void Initialize()
        {
            this.towerTypeId = towerTypeId;
            SetTowerRankFromType();
    
            if (towerTypeId == 7 || towerTypeId == 8)
            {
                maxStackCount = 1;
            }
    
            LoadTowerData();
            ValidateWeapons();
            UpdateRankImage();
            AddTower();
        }

        public void Initialize(int typeId)
        {
            this.towerTypeId = typeId;
            SetTowerRankFromType();
    
            if (towerTypeId == 7 || towerTypeId == 8)
            {
                maxStackCount = 1;
            }
    
            LoadTowerData();
            ValidateWeapons();
            UpdateRankImage();
            AddTower();
        }

        private void SetTowerRankFromType()
        {
            if (towerTypeId >= 1 && towerTypeId <= 2)
            {
                currentRank = TowerRank.Normal;
            }
            else if (towerTypeId >= 3 && towerTypeId <= 4)
            {
                currentRank = TowerRank.Rare;
            }
            else if (towerTypeId >= 5 && towerTypeId <= 6)
            {
                currentRank = TowerRank.Hero;
            }
            else if (towerTypeId >= 7 && towerTypeId <= 8)
            {
                currentRank = TowerRank.Legendary;
            }
            else
            {
                currentRank = TowerRank.Normal;
            }
        }

        private void UpdateRankImage()
        {
            if (rankImage == null) return;

            Color targetColor = GetRankColor(currentRank);
            rankImage.color = targetColor;

            Debug.Log($"타워 랭크 이미지 색상 설정: {currentRank} - {targetColor}");
        }

        private Color GetRankColor(TowerRank rank)
        {
            switch (rank)
            {
                case TowerRank.Normal:
                    return normalColor;
                case TowerRank.Rare:
                    return rareColor;
                case TowerRank.Hero:
                    return heroColor;
                case TowerRank.Legendary:
                    return legendaryColor;
                default:
                    return normalColor;
            }
        }

        private void LoadTowerData()
        {
            var csvManager = CSVLoadManager.Instance;
            if (csvManager != null)
            {
                towerData = csvManager.GetTowerData(towerTypeId);
                if (towerData == null)
                {
                    Debug.LogError($"타워 데이터를 찾을 수 없습니다. ID: {towerTypeId}");
                }
            }
        }

        private void ValidateWeapons()
        {
            if (towerWeapons == null || towerWeapons.Length != maxStackCount)
            {
                Debug.LogError($"타워 무기 배열이 올바르지 않습니다. 필요: {maxStackCount}개, 현재: {towerWeapons?.Length ?? 0}개");
                return;
            }

            for (int i = 0; i < towerWeapons.Length; i++)
            {
                if (towerWeapons[i] != null)
                {
                    towerWeapons[i].gameObject.SetActive(false);
                }
            }
        }

        public bool AddTower()
        {
            if (!CanAddStack)
            {
                Debug.LogWarning($"더 이상 타워를 추가할 수 없습니다. 현재: {currentStackCount}/{maxStackCount}");
                return false;
            }

            if (towerWeapons == null || currentStackCount >= towerWeapons.Length)
            {
                Debug.LogError("타워 무기가 설정되지 않았거나 범위를 벗어났습니다.");
                return false;
            }

            TowerWeapon weapon = towerWeapons[currentStackCount];
            if (weapon == null)
            {
                Debug.LogError($"타워 무기 {currentStackCount}가 설정되지 않았습니다.");
                return false;
            }

            weapon.gameObject.SetActive(true);
            InitializeTowerWeapon(weapon);
            currentStackCount++;

            UpdateTowerPositions();  // 이 줄 추가
            PlayStackUpEffect();
            
            return true;
        }

        private void InitializeTowerWeapon(TowerWeapon weapon)
        {
            if (towerData != null)
            {
                weapon.SetTowerData(towerData);
            }
            else
            {
                Debug.LogWarning("타워 데이터가 없어 기본값으로 초기화합니다.");
            }
        }

        public bool RemoveTower()
        {
            if (IsEmpty)
            {
                Debug.LogWarning("제거할 타워가 없습니다.");
                return false;
            }

            int lastIndex = currentStackCount - 1;
            TowerWeapon weaponToRemove = towerWeapons[lastIndex];
            
            if (weaponToRemove != null)
            {
                weaponToRemove.gameObject.SetActive(false);
            }

            currentStackCount--;
            
            UpdateTowerPositions();

            Debug.Log($"타워 무기 비활성화 완료. 현재 스택: {currentStackCount}/{maxStackCount}");
            return true;
        }

        public void RemoveAllTowers()
        {
            for (int i = 0; i < towerWeapons.Length; i++)
            {
                if (towerWeapons[i] != null)
                {
                    towerWeapons[i].gameObject.SetActive(false);
                }
            }

            currentStackCount = 0;
            
            Debug.Log("모든 타워 무기가 비활성화되었습니다.");
        }

        public void SetStackCount(int newStackCount)
        {
            newStackCount = Mathf.Clamp(newStackCount, 0, maxStackCount);
            
            if (newStackCount == currentStackCount) return;

            if (newStackCount > currentStackCount)
            {
                int towersToAdd = newStackCount - currentStackCount;
                for (int i = 0; i < towersToAdd; i++)
                {
                    if (!AddTower()) break;
                }
            }
            else if (newStackCount < currentStackCount)
            {
                int towersToRemove = currentStackCount - newStackCount;
                for (int i = 0; i < towersToRemove; i++)
                {
                    if (!RemoveTower()) break;
                }
            }
        }

        public void UpgradeAllTowers()
        {
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null)
                {
                    towerWeapons[i].UpgradeWeapon();
                }
            }

            PlayUpgradeEffect();
            Debug.Log($"모든 타워 무기가 업그레이드되었습니다. ({currentStackCount}개)");
        }

        public void MoveGroup(Vector3 targetPosition)
        {
            transform.position = targetPosition;
        }

        public float GetTotalDamage()
        {
            float totalDamage = 0f;
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null)
                {
                    totalDamage += towerWeapons[i].AttackDamage;
                }
            }
            return totalDamage;
        }

        public List<Transform> GetAllTargets()
        {
            var allTargets = new List<Transform>();
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null && towerWeapons[i].IsAttacking)
                {
                    allTargets.Add(towerWeapons[i].transform);
                }
            }
            return allTargets;
        }

        public int GetAttackingTowerCount()
        {
            int count = 0;
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null && towerWeapons[i].IsAttacking)
                {
                    count++;
                }
            }
            return count;
        }

        public void SetTowerType(int newTowerTypeId)
        {
            if (towerTypeId == newTowerTypeId) return;

            towerTypeId = newTowerTypeId;
            SetTowerRankFromType();
            UpdateRankImage();
            LoadTowerData();

            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null)
                {
                    InitializeTowerWeapon(towerWeapons[i]);
                }
            }

            Debug.Log($"타워 타입이 {newTowerTypeId}로 변경되었습니다. 랭크: {currentRank}");
        }

        public void SetRankColor(TowerRank rank)
        {
            currentRank = rank;
            UpdateRankImage();
        }

        public void SetCustomRankColor(Color customColor)
        {
            if (rankImage != null)
            {
                rankImage.color = customColor;
            }
        }

        private void PlayStackUpEffect()
        {
            if (stackUpEffect != null)
            {
                stackUpEffect.Play();
            }
        }

        private void PlayUpgradeEffect()
        {
            if (upgradeEffect != null)
            {
                upgradeEffect.Play();
            }
        }

        public TowerGroupInfo GetGroupInfo()
        {
            return new TowerGroupInfo
            {
                towerTypeId = this.towerTypeId,
                currentStackCount = this.currentStackCount,
                maxStackCount = this.maxStackCount,
                totalDamage = GetTotalDamage(),
                attackingCount = GetAttackingTowerCount(),
                position = transform.position,
                rank = this.currentRank
            };
        }

        public TowerWeapon GetWeapon(int index)
        {
            if (index >= 0 && index < towerWeapons.Length)
            {
                return towerWeapons[index];
            }
            return null;
        }

        public bool IsWeaponActive(int index)
        {
            if (index >= 0 && index < currentStackCount && index < towerWeapons.Length)
            {
                return towerWeapons[index] != null && towerWeapons[index].gameObject.activeInHierarchy;
            }
            return false;
        }

        public void SetAllWeaponsTarget(Transform target)
        {
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null && towerWeapons[i].gameObject.activeInHierarchy)
                {
                    towerWeapons[i].SetTarget(target);
                }
            }
        }

        public void StopAllWeapons()
        {
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null)
                {
                    towerWeapons[i].StopAttack();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (towerWeapons != null)
            {
                Gizmos.color = GetRankColor(currentRank);
                for (int i = 0; i < towerWeapons.Length; i++)
                {
                    if (towerWeapons[i] != null)
                    {
                        Vector3 weaponPos = towerWeapons[i].transform.position;
                        Gizmos.DrawWireCube(weaponPos, Vector3.one * 0.5f);
                        
                        if (i < currentStackCount)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawCube(weaponPos, Vector3.one * 0.3f);
                            Gizmos.color = GetRankColor(currentRank);
                        }
                    }
                }
            }

            if (currentStackCount > 0 && towerWeapons[0] != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, towerWeapons[0].AttackRange);
            }
        }

        private void OnDestroy()
        {
            RemoveAllTowers();
        }
        
        private void UpdateTowerPositions()
        {
            Vector3[] positions = GetStackPositions(currentStackCount);
    
            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null && i < positions.Length)
                {
                    towerWeapons[i].transform.localPosition = positions[i];
                }
            }
        }

        private Vector3[] GetStackPositions(int stackCount)
        {
            switch (stackCount)
            {
                case 1:
                    return new Vector3[]
                    {
                        new Vector3(0f, 0f, 0f)
                    };
        
                case 2:
                    return new Vector3[]
                    {
                        new Vector3(-0.15f, 0f, 0f),
                        new Vector3(0.15f, -0.25f, 0f)
                    };
        
                case 3:
                    return new Vector3[]
                    {
                        new Vector3(-0.1f, 0f, 0f),
                        new Vector3(0.25f, -0.15f, 0f),
                        new Vector3(-0.1f, -0.35f, 0f)
                    };
        
                default:
                    return new Vector3[] { Vector3.zero };
            }
        }
    }

    [System.Serializable]
    public struct TowerGroupInfo
    {
        public int towerTypeId;
        public int currentStackCount;
        public int maxStackCount;
        public float totalDamage;
        public int attackingCount;
        public Vector3 position;
        public TowerRank rank;
    }
}