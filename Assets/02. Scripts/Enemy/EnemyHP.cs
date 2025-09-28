using UnityEngine;
using System;

namespace LuckyDefense
{
    public class EnemyHP : MonoBehaviour
    {
        [SerializeField] private float maxHP = 100f;
        private float currentHP;
        
        public event Action OnDeath;
        public event Action<float, float> OnHealthChanged;
        
        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public float HPPercentage => maxHP > 0 ? currentHP / maxHP : 0f;
        public bool IsDead => currentHP <= 0;

        public void Initialize(float hp)
        {
            maxHP = hp;
            currentHP = hp;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        public void SetMaxHP(float hp)
        {
            maxHP = hp;
            currentHP = hp;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHP = Mathf.Max(0, currentHP - damage);
            OnHealthChanged?.Invoke(currentHP, maxHP);
            
            if (currentHP <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        public void SetCurrentHP(float hp)
        {
            currentHP = Mathf.Clamp(hp, 0, maxHP);
            OnHealthChanged?.Invoke(currentHP, maxHP);
            
            if (currentHP <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void RestoreToFull()
        {
            currentHP = maxHP;
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
    }
}