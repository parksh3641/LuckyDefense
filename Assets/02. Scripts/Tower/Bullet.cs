using UnityEngine;

namespace LuckyDefense
{
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private GameObject hitEffect;
        
        private Transform target;
        private float damage;
        private float timer;
        private bool hasHit = false;
        
        public float Damage => damage;
        public Transform Target => target;

        public void Initialize(Transform targetTransform, float bulletDamage)
        {
            target = targetTransform;
            damage = bulletDamage;
            timer = 0f;
            hasHit = false;
            
            if (target != null)
            {
                LookAtTarget();
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            
            if (timer >= lifeTime)
            {
                DestroyBullet();
                return;
            }

            if (hasHit) return;

            if (target == null)
            {
                DestroyBullet();
                return;
            }

            MoveToTarget();
            CheckHit();
        }

        private void MoveToTarget()
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
        }

        private void LookAtTarget()
        {
            if (target == null) return;
            
            Vector3 direction = target.position - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }

        private void CheckHit()
        {
            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (hasHit) return;
            
            hasHit = true;

            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            PlayHitEffect();
            DestroyBullet();
        }

        private void PlayHitEffect()
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }
        }

        private void DestroyBullet()
        {
            Destroy(gameObject);
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public void SetLifeTime(float newLifeTime)
        {
            lifeTime = newLifeTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            if (other.CompareTag("Enemy") || other.CompareTag("Monster"))
            {
                if (other.transform == target)
                {
                    HitTarget();
                }
            }
        }
    }
}