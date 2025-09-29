using UnityEngine;
using DG.Tweening;

namespace LuckyDefense
{
    public class ClickPulseAnimation : MonoBehaviour
    {
        [Header("펄스 애니메이션 설정")] public float pulseInterval = 2f;
        public float pulseDuration = 0.8f;
        public bool autoStart = true;

        private Vector3 originalScale;
        private Sequence pulseSequence;
        private bool isRunning = false;

        void Awake()
        {
            originalScale = transform.localScale;
        }

        void Start()
        {
            if (autoStart)
            {
                StartPulseAnimation();
            }
        }

        public void StartPulseAnimation()
        {
            if (isRunning) return;

            isRunning = true;
            CreatePulseSequence();
        }

        public void StopPulseAnimation()
        {
            if (!isRunning) return;

            isRunning = false;
            if (pulseSequence != null)
            {
                pulseSequence.Kill();
            }

            transform.localScale = originalScale;
        }

        void CreatePulseSequence()
        {
            pulseSequence = DOTween.Sequence();

            pulseSequence.Append(transform.DOScale(originalScale * 1.1f, pulseDuration * 0.3f)
                    .SetEase(Ease.OutQuad))
                .Append(transform.DOScale(originalScale * 0.95f, pulseDuration * 0.4f)
                    .SetEase(Ease.InOutQuad))
                .Append(transform.DOScale(originalScale, pulseDuration * 0.3f)
                    .SetEase(Ease.OutQuad))
                .AppendInterval(pulseInterval - pulseDuration)
                .SetLoops(-1, LoopType.Restart);
        }

        void OnDestroy()
        {
            if (pulseSequence != null)
            {
                pulseSequence.Kill();
            }
        }
    }
}