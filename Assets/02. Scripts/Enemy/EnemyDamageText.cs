using TMPro;
using UnityEngine;
using System.Collections;

namespace LuckyDefense
{
    public class EnemyDamageText : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 50.0f;
        [SerializeField] private float downScale = 0.7f;
        [SerializeField] private float destroyTime = 0.3f;
        [SerializeField] private float fadeDelay = 0.3f;

        private TextMeshProUGUI damageText;
        private Vector3 initialScale;
        private int damageValue;
        private Coroutine animationCoroutine;

        private void Awake()
        {
            damageText = GetComponent<TextMeshProUGUI>();
            if (damageText == null)
            {
                damageText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (damageText != null && damageValue > 0)
            {
                damageText.text = damageValue.ToString();
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }
                animationCoroutine = StartCoroutine(AnimateText());
            }
        }

        public void Setup(int damage, Vector3 position)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            damageValue = damage;
            transform.position = position - new Vector3(0, 40, 0);
            transform.localScale = initialScale;
            
            if (damageText != null)
            {
                damageText.text = damage.ToString();
                
                Color currentColor = damageText.color;
                currentColor.a = 1f;
                damageText.color = currentColor;
            }
            
            if (gameObject.activeInHierarchy)
            {
                animationCoroutine = StartCoroutine(AnimateText());
            }
        }

        public void SetDamageColor(Color color)
        {
            if (damageText != null)
            {
                damageText.color = color;
            }
        }

        public void SetFontSize(float fontSize)
        {
            if (damageText != null)
            {
                damageText.fontSize = fontSize;
            }
        }

        private IEnumerator AnimateText()
        {
            float elapsedTime = 0f;
            Vector3 targetScale = initialScale * downScale;
            Vector3 startPosition = transform.position;
            
            while (elapsedTime < destroyTime)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / destroyTime);
                
                transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
                transform.position = startPosition + Vector3.up * (moveSpeed * elapsedTime);
                
                yield return null;
            }

            yield return new WaitForSeconds(fadeDelay);
            
            float fadeTime = 0.2f;
            elapsedTime = 0f;
            Color originalColor = damageText.color;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                
                Color fadeColor = originalColor;
                fadeColor.a = alpha;
                damageText.color = fadeColor;
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
        }
    }
}