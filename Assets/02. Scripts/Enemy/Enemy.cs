using System.Collections;
using Spine;
using UnityEngine;
using Spine.Unity;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LuckyDefense
{
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float maxHP = 0;
        [SerializeField] private float moveSpeed = 0;
        [SerializeField] private int attackDamage = 0;
        [SerializeField] private int goldReward = 0;

        [SerializeField] private GameObject hpViewer;
        [SerializeField] private Image hpFillAmount;
        [SerializeField] private TMP_Text hpText;

        [SerializeField] private GameObject timerObject;
        [SerializeField] private TMP_Text timerText;
        
        [Header("Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private BoxCollider boxCollider;

        [Header("Effect")] 
        [SerializeField] private SkeletonAnimation effect;
        
        private Vector3[] waypoints;
        private int currentWaypointIndex = 0;
        private EnemySpawner spawner;
        private float currentHP;
        private bool isDead = false;
        private bool isMoving = true;
        
        private float miniBossLifeTime = 60f;
        private float miniBossTimer = 0f;
        private bool isMiniBoss = false;
        
        private Transform rootParent;
        private Vector3 targetPosition;
        private Vector3 direction;
        private float distanceToTarget;
        
        private static readonly Quaternion rotationZero = Quaternion.Euler(0, 0, 0);
        private static readonly Quaternion rotation180 = Quaternion.Euler(0, 180, 0);
        
        public float MaxHP => maxHP;
        public int GoldReward => goldReward;
        public int AttackDamage => attackDamage;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
                
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
                
            CacheRootParent();
        }

        public void Initialize(Vector3[] waypointPath, EnemySpawner enemySpawner)
        {
            waypoints = waypointPath;
            spawner = enemySpawner;
            currentWaypointIndex = 0;
            currentHP = maxHP;
            isDead = false;
            isMoving = true;

            if (boxCollider != null)
                boxCollider.enabled = true;

            if (hpViewer != null)
                hpViewer.SetActive(false);

            PlayAnimation("run");

            if (waypoints != null && waypoints.Length > 0)
            {
                transform.position = waypoints[0];
                currentWaypointIndex = 1;
            }

            if (effect != null)
                effect.gameObject.SetActive(false);

            isMiniBoss = gameObject.CompareTag("MiniBoss");
            if (isMiniBoss)
            {
                miniBossTimer = miniBossLifeTime;
                if (timerObject != null)
                {
                    timerObject.SetActive(true);
                }
                if (hpViewer != null)
                {
                    hpViewer.SetActive(true);
                }
            }
            else
            {
                if (timerObject != null)
                    timerObject.SetActive(false);
            }
        }
        
        public void SetStats(int hp, float speed, int atk, int gold)
        {
            maxHP = hp;
            currentHP = hp;
            moveSpeed = speed;
            attackDamage = atk;
            goldReward = gold;
        }

        private void Update()
        {
            if (isDead) return;

            if (isMiniBoss)
            {
                miniBossTimer -= Time.deltaTime;
                UpdateTimerUI();
                UpdateHPUI();

                if (miniBossTimer <= 0f)
                {
                    DieByTimeout();
                    return;
                }
            }

            if (!isMoving) return;

            MoveToNextWaypoint();
        }
        
        private void CacheRootParent()
        {
            rootParent = transform;
            while (rootParent.parent != null)
                rootParent = rootParent.parent;
        }
        
        private void CorrectUIRotation(Transform uiTransform)
        {
            Transform uiRoot = uiTransform;
            while (uiRoot.parent != null)
                uiRoot = uiRoot.parent;

            Vector3 parentEuler = uiRoot.eulerAngles;
            uiTransform.rotation = rotationZero;
            uiTransform.Rotate(0, parentEuler.y, 0);
        }
        
        private void UpdateTimerUI()
        {
            if (timerText != null && isMiniBoss)
            {
                float displayTime = Mathf.Max(0f, miniBossTimer);
                timerText.text = $"{displayTime:F1}s";
        
                if (miniBossTimer < 30f)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = Color.white;
                }
        
                CorrectUIRotation(timerObject.transform);
            }
        }

        private void MoveToNextWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            targetPosition = waypoints[currentWaypointIndex];
            direction = (targetPosition - transform.position).normalized;
    
            transform.position += direction * (moveSpeed * Time.deltaTime);
            LookAtDirection();

            distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < 0.1f)
            {
                currentWaypointIndex++;
        
                if (currentWaypointIndex >= waypoints.Length)
                {
                    currentWaypointIndex = 0;
                }
            }
        }

        private void LookAtDirection()
        {
            if (currentWaypointIndex == 1 || currentWaypointIndex == 2 || currentWaypointIndex == 4)
            {
                transform.rotation = rotation180;
            }
            else
            {
                transform.rotation = rotationZero;
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            if (hpViewer != null && !hpViewer.activeSelf)
                hpViewer.SetActive(true);

            currentHP -= damage;
            UpdateHPUI();

            if (effect != null)
            {
                effect.gameObject.SetActive(true); 
                effect.AnimationState.ClearTrack(0);
                effect.AnimationState.SetAnimation(0, "animation", false);
                effect.AnimationState.Complete += OnAnimationComplete;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SFXType.Hit);
            }

            if (currentHP <= 0)
            {
                Die();
            }
        }
        
        
        void OnAnimationComplete(TrackEntry trackEntry)
        {
            effect.gameObject.SetActive(false);
            effect.AnimationState.Complete -= OnAnimationComplete;
        }
        
        private void UpdateHPUI()
        {
            if (hpViewer == null) return;

            if (hpFillAmount != null)
            {
                hpFillAmount.fillAmount = currentHP / maxHP;
            }

            if (hpText != null)
            {
                hpText.text = $"{(int)currentHP}";
            }

            CorrectUIRotation(hpViewer.transform);
        }
        
        private void OnEnemyDeath()
        {
            Die();
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            isMoving = false;

            if (boxCollider != null)
                boxCollider.enabled = false;
       
            SetUIActive(false);

            PlayAnimation("dead");
       
            if(GameManager.Instance != null)
            {
                if (gameObject.CompareTag("MiniBoss") || gameObject.CompareTag("Boss"))
                {
                    GameManager.Instance.AddGem(2);
               
                    if (gameObject.CompareTag("Boss"))
                    {
                        CheckBossDefeatForVictory();
                    }
                }
                else if (gameObject.CompareTag("Enemy"))
                {
                    GameManager.Instance.AddGold(2);
                }
            }

            StartCoroutine(DestroyAfterDelay());
        }

        private void CheckBossDefeatForVictory()
        {
            if (WaveManager.Instance != null)
            {
                if (WaveManager.Instance.IsLastWave())
                {
                    StartCoroutine(CheckVictoryAfterDelay());
                }
                else
                {
                    StartCoroutine(CheckNextWaveAfterDelay());
                }
            }
        }

        private IEnumerator CheckNextWaveAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
    
            GameObject[] remainingBosses = GameObject.FindGameObjectsWithTag("Boss");
            if (remainingBosses.Length == 0)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBossDefeatNotification();
                    WaveManager.Instance.SetWaveTimer(5f);
                }
            }
        }


        private IEnumerator CheckVictoryAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
    
            GameObject[] remainingBosses = GameObject.FindGameObjectsWithTag("Boss");
            if (remainingBosses.Length == 0)
            {
                if (UIManager.Instance != null)
                {
                    Time.timeScale = 0f;
                    UIManager.Instance.ShowGameWinUI();
                }
            }
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(1.0f);
    
            if (spawner != null)
            {
                spawner.DestroyEnemy(this);
            }
        }

        private void PlayAnimation(string animationName)
        {
            if (skeletonAnimation != null)
            {
                bool loop = animationName != "dead";
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            }
        }

        public void StopMovement()
        {
            isMoving = false;
            PlayAnimation("idle");
        }

        public void ResumeMovement()
        {
            if (!isDead)
            {
                isMoving = true;
                PlayAnimation("run");
            }
        }
        
        private void DieByTimeout()
        {
            if (isDead) return;

            isDead = true;
            isMoving = false;

            if (boxCollider != null)
                boxCollider.enabled = false;
    
            SetUIActive(false);

            PlayAnimation("dead");

            StartCoroutine(DestroyAfterDelay());
        }
        
        private void SetUIActive(bool active)
        {
            if(hpViewer != null)
                hpViewer.SetActive(active);
            
            if(timerObject != null && isMiniBoss)
                timerObject.SetActive(active);
        }
    }
}