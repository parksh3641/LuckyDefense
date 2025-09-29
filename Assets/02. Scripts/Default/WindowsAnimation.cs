using UnityEngine;
using DG.Tweening;

namespace LuckyDefense
{
    public class WindowsAnimation : MonoBehaviour
    {
        [Header("애니메이션 설정")] 
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private Ease easeType = Ease.OutBack;

        private Vector3 originalScale;
        private bool isAnimating = false;

        void Awake()
        {
            originalScale = transform.localScale;
        }

        void OnEnable()
        {
            if (isAnimating) return;

            PlayActivateAnimation();
        }

        public void PlayActivateAnimation()
        {
            if (isAnimating) return;

            isAnimating = true;
            transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(transform.DOScale(originalScale * 1.05f, animationDuration * 0.7f)
                    .SetEase(Ease.OutQuad))
                .Append(transform.DOScale(originalScale, animationDuration * 0.3f)
                    .SetEase(Ease.OutQuad))
                .OnComplete(() => { isAnimating = false; });
        }
    }
}