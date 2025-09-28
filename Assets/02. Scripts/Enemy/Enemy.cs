using System.Collections;
using Spine;
using UnityEngine;
using Spine.Unity;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LuckyDefense
{
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private int goldReward = 10;

        [SerializeField] private GameObject hpViewer;
        [SerializeField] private Image hpFillAmount;
        
        [Header("Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private BoxCollider boxCollider;

        [Header("Effect")] [SerializeField] private SkeletonAnimation effect;
        
        private Vector3[] waypoints;
        private int currentWaypointIndex = 0;
        private EnemySpawner spawner;
        private float currentHP;
        private bool isDead = false;
        private bool isMoving = true;
        
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
            if (isDead || !isMoving) return;
            
            MoveToNextWaypoint();
        }

        private void MoveToNextWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Vector3 targetPosition = waypoints[currentWaypointIndex];
            Vector3 direction = (targetPosition - transform.position).normalized;
    
            transform.position += direction * moveSpeed * Time.deltaTime;
            LookAtDirection(direction);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentWaypointIndex++;
        
                if (currentWaypointIndex >= waypoints.Length)
                {
                    currentWaypointIndex = 0;
                }
            }
        }

        private void LookAtDirection(Vector3 direction)
        {
            if (currentWaypointIndex == 1 || currentWaypointIndex == 2 || currentWaypointIndex == 4)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0); // 오른쪽
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);   // 왼쪽
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
            if (hpFillAmount == null || hpViewer == null) return;

            hpFillAmount.fillAmount = currentHP / maxHP;

            Transform rootParent = hpViewer.transform;
            while (rootParent.parent != null)
                rootParent = rootParent.parent;

            Vector3 parentEuler = rootParent.eulerAngles;
            hpViewer.transform.rotation = Quaternion.Euler(0, 0, 0);
            hpViewer.transform.Rotate(0, parentEuler.y, 0);
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
            
            if(hpViewer != null)
                hpViewer.SetActive(false);
        
            PlayAnimation("dead");
            
            if(GameManager.Instance != null)
                GameManager.Instance.AddGold(1);
    
            StartCoroutine(DestroyAfterDelay());
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(1.0f);
    
            if (spawner != null)
            {
                spawner.DestroyEnemy(this, false);
            }
        }

        private void ReachEnd()
        {
            if (isDead) return;
            
            isDead = true;
            isMoving = false;
            
            if (spawner != null)
            {
                spawner.DestroyEnemy(this, true);
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
    }
}