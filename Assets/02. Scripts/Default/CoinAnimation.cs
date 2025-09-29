using System.Collections;
using UnityEngine;
using TMPro;

namespace LuckyDefense
{
    public class CoinAnimation : MonoBehaviour
    {
        [Header("Animation Settings")] [SerializeField]
        private float moveDistance = 100f;

        [SerializeField] private float duration = 1f;
        [SerializeField] private TMP_Text coinText;

        private RectTransform rectTransform;
        private Vector3 startPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            startPosition = rectTransform.anchoredPosition;
        }

        public void Initialize(int amount)
        {
            coinText.text = "+" + amount;
            rectTransform.anchoredPosition = startPosition;
            gameObject.SetActive(true);

            StopAllCoroutines();

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PlayAnimation());
            }
        }

        private IEnumerator PlayAnimation()
        {
            Vector3 endPosition = startPosition + new Vector3(0, moveDistance, 0);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = endPosition;
            gameObject.SetActive(false);

        }
    }
}