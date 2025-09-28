using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LuckyDefense
{
    public class TowerGroup : MonoBehaviour
    {
        [Header("Tower Group Settings")]
        [SerializeField] private int towerTypeId;
        [SerializeField] private int maxStackCount = 3;
        
        [Header("Tower Weapons")]
        [SerializeField] private TowerWeapon[] towerWeapons;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem stackUpEffect;
        [SerializeField] private ParticleSystem upgradeEffect;
        
        private int currentStackCount = 0;
        private TowerData towerData;
        
        public int TowerTypeId => towerTypeId;
        public int CurrentStackCount => currentStackCount;
        public int MaxStackCount => maxStackCount;
        public bool CanAddStack => currentStackCount < maxStackCount;
        public bool IsEmpty => currentStackCount == 0;
        public List<TowerWeapon> ActiveWeapons => towerWeapons.Take(currentStackCount).ToList();

        public void Initialize()
        {
            this.towerTypeId = towerTypeId;
            LoadTowerData();
            ValidateWeapons();
            AddTower();
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

            PlayStackUpEffect();
            
            Debug.Log($"타워 무기 활성화 완료. 현재 스택: {currentStackCount}/{maxStackCount}");
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
            LoadTowerData();

            for (int i = 0; i < currentStackCount; i++)
            {
                if (towerWeapons[i] != null)
                {
                    InitializeTowerWeapon(towerWeapons[i]);
                }
            }

            Debug.Log($"타워 타입이 {newTowerTypeId}로 변경되었습니다.");
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
                position = transform.position
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
                Gizmos.color = Color.blue;
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
                            Gizmos.color = Color.blue;
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
    }
}