using DG.Tweening;
using System;
using UnityEngine;

namespace UI.Animations.Game
{
    public abstract class ObjectAnimations : MonoBehaviour
    {
        [Header("Animation Duration Setup")]
        [SerializeField] private float _appearAnimationDuration = 0.35f;
        [SerializeField] private float _hoverAnimationDuration = 0.5f;

        [Space(5), Header("Hover Animation Setup")]
        [SerializeField] private float _yMoveOffset = 1.0f;

        [Space(5), Header("Animations Flags")]
        [SerializeField] private bool _hoverAnimationEnabled = true;

        private Vector3 _originalScale;

        private Tween _appearTween;
        private Tween _hoverTween;

        private void OnEnable()
        {
            if (_hoverAnimationEnabled)
                HoverAnimation();
        }

        private void OnDisable()
        {
            _hoverTween?.Kill();
            _appearTween?.Kill();
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
            _appearTween?.Kill();
        }

        private void HoverAnimation()
        {
            _hoverTween?.Kill();

            Vector3 originalPosition = transform.localPosition;
            float targetY = transform.localPosition.y + _yMoveOffset;

            _hoverTween = transform
                .DOLocalMoveY(targetY, _hoverAnimationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .OnKill(() => transform.localPosition = originalPosition);
        }

        public void Appear(Vector3 originalScale, Action onComplete = null)
        {
            transform.localScale = Vector3.zero;
            _originalScale = originalScale;

            _appearTween?.Kill();

            _appearTween = transform
                .DOScale(originalScale, _appearAnimationDuration)
                .SetEase(Ease.InOutBounce)
                .OnComplete(() => onComplete?.Invoke())
                .OnKill(() => transform.localScale = _originalScale);
        }
    }
}