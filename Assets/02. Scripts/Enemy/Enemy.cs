using UnityEngine;
using Spine.Unity;

namespace LuckyDefense
{
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private int goldReward = 10;
        
        [Header("Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private CircleCollider2D circleCollider2D;
        
        private Vector3[] waypoints;
        private int currentWaypointIndex = 0;
        private EnemySpawner spawner;
        private float currentHP;
        private bool isDead = false;
        private bool isMoving = true;
        private EnemyHP enemyHP;
        
        public float CurrentHP => enemyHP != null ? enemyHP.CurrentHP : currentHP;
        public float MaxHP => maxHP;
        public int GoldReward => goldReward;
        public int AttackDamage => attackDamage;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (circleCollider2D == null)
                circleCollider2D = GetComponent<CircleCollider2D>();
                
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
                
            enemyHP = GetComponent<EnemyHP>();
            if (enemyHP == null)
            {
                enemyHP = gameObject.AddComponent<EnemyHP>();
            }
        }

        public void Initialize(Vector3[] waypointPath, EnemySpawner enemySpawner)
        {
            waypoints = waypointPath;
            spawner = enemySpawner;
            currentWaypointIndex = 0;
            currentHP = maxHP;
            isDead = false;
            isMoving = true;
            
            if (enemyHP != null)
            {
                enemyHP.Initialize(maxHP);
                enemyHP.OnDeath += OnEnemyDeath;
            }
            
            if (circleCollider2D != null)
                circleCollider2D.enabled = true;
                
            PlayAnimation("run");
            
            if (waypoints != null && waypoints.Length > 0)
            {
                transform.position = waypoints[0];
                currentWaypointIndex = 1;
            }
        }

        public void SetStats(int hp, float speed, int atk, int gold)
        {
            maxHP = hp;
            currentHP = hp;
            moveSpeed = speed;
            attackDamage = atk;
            goldReward = gold;
            
            if (enemyHP != null)
            {
                enemyHP.SetMaxHP(hp);
            }
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
            if (direction.x > 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (direction.x < 0)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            }
            else
            {
                currentHP -= damage;
                if (currentHP <= 0)
                {
                    Die();
                }
            }
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
            
            if (circleCollider2D != null)
                circleCollider2D.enabled = false;
                
            PlayAnimation("dead");
            
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

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        private void OnDestroy()
        {
            if (enemyHP != null)
            {
                enemyHP.OnDeath -= OnEnemyDeath;
            }
        }
    }
}