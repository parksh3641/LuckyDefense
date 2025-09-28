using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace LuckyDefense
{
    public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _clickScale = 0.95f;
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _animationDuration = 0.1f;
        [SerializeField] private Ease _animationEase = Ease.OutBack;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private bool _isPressed;
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _originalScale = _transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _targetScale = _originalScale * _clickScale;
            _transform.DOScale(_targetScale, _animationDuration).SetEase(_animationEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _transform.DOScale(_originalScale, _animationDuration).SetEase(_animationEase);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                _targetScale = _originalScale * _hoverScale;
                _transform.DOScale(_targetScale, _animationDuration).SetEase(_animationEase);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                _transform.DOScale(_originalScale, _animationDuration).SetEase(_animationEase);
            }
        }

        private void OnDisable()
        {
            _transform.DOKill();
            _transform.localScale = _originalScale;
        }
    }
}