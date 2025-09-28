using UnityEngine;

namespace LuckyDefense
{
    public class Movement2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        
        private Vector3 moveDirection = Vector3.zero;
        private bool isMoving = false;
        private bool isDead = false;
        
        public float MoveSpeed => moveSpeed;
        public bool IsMoving => isMoving;
        public bool IsDead => isDead;

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        public void MoveTo(Vector3 direction)
        {
            if (isDead) return;
            
            moveDirection = direction.normalized;
            isMoving = moveDirection != Vector3.zero;
        }

        public void MoveStop()
        {
            moveDirection = Vector3.zero;
            isMoving = false;
        }

        public void Dead()
        {
            isDead = true;
            MoveStop();
        }

        public void Revive()
        {
            isDead = false;
        }

        private void Update()
        {
            if (!isMoving || isDead) return;
            
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        public void MoveToPosition(Vector3 targetPosition)
        {
            if (isDead) return;
            
            Vector3 direction = (targetPosition - transform.position).normalized;
            MoveTo(direction);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public float GetDistanceToTarget(Vector3 targetPosition)
        {
            return Vector3.Distance(transform.position, targetPosition);
        }

        public Vector3 GetDirectionToTarget(Vector3 targetPosition)
        {
            return (targetPosition - transform.position).normalized;
        }

        public bool HasReachedTarget(Vector3 targetPosition, float threshold = 0.1f)
        {
            return Vector3.Distance(transform.position, targetPosition) <= threshold;
        }

        public void ApplySlowEffect(float slowMultiplier)
        {
            if (isDead) return;
            
            slowMultiplier = Mathf.Clamp01(slowMultiplier);
            float currentSpeed = moveSpeed * slowMultiplier;
            
            StartCoroutine(TemporarySpeedChange(currentSpeed, 2f));
        }

        private System.Collections.IEnumerator TemporarySpeedChange(float newSpeed, float duration)
        {
            float originalSpeed = moveSpeed;
            moveSpeed = newSpeed;
            
            yield return new WaitForSeconds(duration);
            
            moveSpeed = originalSpeed;
        }

        public void Freeze(float duration)
        {
            if (isDead) return;
            
            StartCoroutine(FreezeCoroutine(duration));
        }

        private System.Collections.IEnumerator FreezeCoroutine(float duration)
        {
            bool wasMoving = isMoving;
            Vector3 savedDirection = moveDirection;
            
            MoveStop();
            
            yield return new WaitForSeconds(duration);
            
            if (wasMoving && !isDead)
            {
                MoveTo(savedDirection);
            }
        }

        public Vector3 GetVelocity()
        {
            return isMoving ? moveDirection * moveSpeed : Vector3.zero;
        }

        public void SetVelocity(Vector3 velocity)
        {
            if (isDead) return;
            
            moveDirection = velocity.normalized;
            moveSpeed = velocity.magnitude;
            isMoving = velocity != Vector3.zero;
        }
    }
}