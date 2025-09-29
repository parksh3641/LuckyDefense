using UnityEngine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LuckyDefense
{
    public enum TowerState
    {
        Idle,
        Attacking,
        Moving
    }

    public enum TargetType
    {
        Nearest = 1,
        Farthest = 2,
        LowestHP = 3,
        HighestHP = 4,
        Random = 5
    }

    public class TowerWeapon : MonoBehaviour
    {
        [Header("Tower Settings")]
        [SerializeField] private int towerTypeId;
        
        [Header("Spine Animation")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string idleAnimationName = "idle";
        [SerializeField] private string attackAnimationName = "attack";
        [SerializeField] private string moveAnimationName = "move";
        
        [Header("Combat")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private LayerMask enemyLayerMask = -1;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem muzzleFlashEffect;
        [SerializeField] private ParticleSystem upgradeEffect;
        
        private TowerData towerData;
        private TowerState currentState = TowerState.Idle;
        private Transform currentTarget;
        private float lastAttackTime;
        private Coroutine attackCoroutine;
        private List<Transform> enemiesInRange = new List<Transform>();
        
        private float attackDamage;
        private float attackInterval;
        private float attackRange;
        private int targetType;
        private float criticalRate;
        private float criticalDamage;
        private bool isActive = false;

        public int TowerTypeId => towerTypeId;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public TowerState CurrentState => currentState;
        public bool IsAttacking => currentState == TowerState.Attacking;
        public bool IsActive => isActive;

        private void Awake()
        {
            SetupSpineAnimation();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            isActive = true;
            InitializeTower();
            StartTargetDetection();
        }

        private void OnDisable()
        {
            isActive = false;
            StopAllCoroutines();
            CancelInvoke();
            StopAttack();
        }

        private void InitializeTower()
        {
            LoadTowerDataFromCSV();
            currentState = TowerState.Idle;
            SetDefaultDirection();
            PlayAnimation(idleAnimationName, true);
        }

        private void SetDefaultDirection()
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        private void LoadTowerDataFromCSV()
        {
            var csvManager = CSVLoadManager.Instance;
            if (csvManager != null)
            {
                towerData = csvManager.GetTowerData(towerTypeId);
                if (towerData != null)
                {
                    ApplyTowerStats();
                }
                else
                {
                    Debug.LogError($"타워 데이터를 찾을 수 없습니다. ID: {towerTypeId}");
                    SetDefaultStats();
                }
            }
            else
            {
                Debug.LogError("CSVLoadManager를 찾을 수 없습니다.");
                SetDefaultStats();
            }
        }

        private void ApplyTowerStats()
        {
            attackDamage = towerData.Atk;
            attackInterval = towerData.Interval;
            attackRange = towerData.Range;
            targetType = towerData.Target;
            criticalRate = towerData.Cri_Rate;
            criticalDamage = towerData.Cri_Damage;
            
            SetSpineAnimationSpeed();
        }

        private void SetDefaultStats()
        {
            attackDamage = 50f;
            attackInterval = 1.0f;
            attackRange = 3.0f;
            targetType = 1;
            criticalRate = 5f;
            criticalDamage = 150f;
            
            SetSpineAnimationSpeed();
        }

        private void SetSpineAnimationSpeed()
        {
            if (skeletonAnimation == null) return;
            
            float baseAnimationDuration = 1.0f;
            float animationSpeed = attackInterval / baseAnimationDuration;
            
            if (skeletonAnimation.skeleton.Data.FindAnimation(attackAnimationName) != null)
            {
                skeletonAnimation.timeScale = animationSpeed;
            }
        }

        private void SetupSpineAnimation()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.Complete += OnAnimationComplete;
            }
        }

        private void OnAnimationComplete(Spine.TrackEntry trackEntry)
        {
            if (trackEntry.Animation.Name == attackAnimationName)
            {
                if (currentState == TowerState.Attacking && currentTarget != null)
                {
                    PlayAnimation(attackAnimationName, false);
                }
                else
                {
                    PlayAnimation(idleAnimationName, true);
                }
            }
        }

        private void PlayAnimation(string animationName, bool loop)
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            }
        }

        private void StartTargetDetection()
        {
            if (isActive)
            {
                InvokeRepeating(nameof(DetectEnemies), 0f, 0.1f);
            }
        }

        private void DetectEnemies()
        {
            if (!isActive || currentState == TowerState.Moving) return;

            enemiesInRange.Clear();
            
            Collider[] enemyColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayerMask);
            
            foreach (var collider in enemyColliders)
            {
                if (collider.CompareTag("Enemy") || collider.CompareTag("MiniBoss")|| collider.CompareTag("Boss"))
                {
                    enemiesInRange.Add(collider.transform);
                }
            }

            if (enemiesInRange.Count > 0)
            {
                SelectTarget();
                StartAttacking();
            }
            else
            {
                StopAttacking();
            }
        }

        private void SelectTarget()
        {
            if (enemiesInRange.Count == 0) return;

            Transform selectedTarget = null;

            switch ((TargetType)targetType)
            {
                case TargetType.Nearest:
                    selectedTarget = GetNearestEnemy();
                    break;
                case TargetType.Farthest:
                    selectedTarget = GetFarthestEnemy();
                    break;
                case TargetType.LowestHP:
                    break;
                case TargetType.HighestHP:
                    break;
                case TargetType.Random:
                    selectedTarget = GetRandomEnemy();
                    break;
                default:
                    selectedTarget = GetNearestEnemy();
                    break;
            }

            currentTarget = selectedTarget;
        }

        private Transform GetNearestEnemy()
        {
            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemiesInRange)
            {
                if (enemy == null) continue;
                
                float distance = Vector3.Distance(transform.position, enemy.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private Transform GetFarthestEnemy()
        {
            Transform farthest = null;
            float maxDistance = 0f;

            foreach (var enemy in enemiesInRange)
            {
                if (enemy == null) continue;
                
                float distance = Vector3.Distance(transform.position, enemy.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthest = enemy;
                }
            }

            return farthest;
        }

        private Transform GetRandomEnemy()
        {
            if (enemiesInRange.Count == 0) return null;
            
            int randomIndex = Random.Range(0, enemiesInRange.Count);
            return enemiesInRange[randomIndex];
        }

        private void StartAttacking()
        {
            if (currentState != TowerState.Attacking && currentTarget != null)
            {
                currentState = TowerState.Attacking;
                PlayAnimation(attackAnimationName, false);
                
                if (attackCoroutine == null)
                {
                    attackCoroutine = StartCoroutine(AttackCoroutine());
                }
            }
        }

        public void StopAttack()
        {
            if (currentState == TowerState.Attacking)
            {
                currentState = TowerState.Idle;
                currentTarget = null;
                PlayAnimation(idleAnimationName, true);
                
                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                    attackCoroutine = null;
                }
            }
        }

        private void StopAttacking()
        {
            StopAttack();
        }

        private IEnumerator AttackCoroutine()
        {
            while (currentState == TowerState.Attacking && currentTarget != null)
            {
                if (Time.time >= lastAttackTime + attackInterval)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                    
                    yield return new WaitForSeconds(attackInterval);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        private void PerformAttack()
        {
            if (currentTarget == null) return;

            LookAtTarget();
            FireBullet();
            PlayMuzzleFlash();
        }

        private void LookAtTarget()
        {
            if (currentTarget == null) return;

            Vector3 direction = currentTarget.position - transform.position;
            if (direction.x < 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        private void FireBullet()
        {
            if (bulletPrefab == null || firePoint == null || currentTarget == null) return;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
    
            var bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                float finalDamage = CalculateDamage();
                bool isCritical = Random.Range(0f, 100f) < criticalRate;
        
                bulletComponent.Initialize(currentTarget, finalDamage);
                
                if (EnemySpawner.Instance != null)
                {
                    EnemySpawner.Instance.ShowDamageText(currentTarget.position, (int)finalDamage, isCritical);
                }
            }
        }

        private float CalculateDamage()
        {
            float damage = attackDamage;
            
            if (Random.Range(0f, 100f) < criticalRate)
            {
                damage *= (criticalDamage / 100f);
            }
            
            return damage;
        }

        private void PlayMuzzleFlash()
        {
            if (muzzleFlashEffect != null)
            {
                muzzleFlashEffect.Play();
            }
        }

        public void SetTarget(Transform target)
        {
            currentTarget = target;
            if (target != null && currentState != TowerState.Attacking)
            {
                StartAttacking();
            }
        }

        public void UpgradeWeapon()
        {
            if (towerData == null) return;
            
            attackDamage *= 1.2f;
            attackInterval *= 0.9f;
            criticalRate += 2f;
            
            SetSpineAnimationSpeed();
            PlayUpgradeEffect();
        }

        private void PlayUpgradeEffect()
        {
            if (upgradeEffect != null)
            {
                upgradeEffect.Play();
            }
        }

        public void SetTowerData(TowerData data)
        {
            towerData = data;
            towerTypeId = data.ID;
            ApplyTowerStats();
        }

        public void MoveTo(Vector3 targetPosition)
        {
            currentState = TowerState.Moving;
            StopAttack();
            PlayAnimation(moveAnimationName, true);
            
            StartCoroutine(MoveCoroutine(targetPosition));
        }

        private IEnumerator MoveCoroutine(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float moveSpeed = 5f;
            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float journeyTime = journeyLength / moveSpeed;
            float elapsedTime = 0;

            while (elapsedTime < journeyTime)
            {
                elapsedTime += Time.deltaTime;
                float fractionOfJourney = elapsedTime / journeyTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
                yield return null;
            }

            transform.position = targetPosition;
            currentState = TowerState.Idle;
            PlayAnimation(idleAnimationName, true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void OnDestroy()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
            }
            
            CancelInvoke();
            
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }
        }
    }
}