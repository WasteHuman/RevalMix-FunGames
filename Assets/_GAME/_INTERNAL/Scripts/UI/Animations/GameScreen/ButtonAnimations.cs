using DG.Tweening;
using System;
using UnityEngine;

namespace UI.Animations.GameScreen
{
    [Serializable]
    public class ButtonAnimations
    {
        [Header("Click Animations Setup")]
        [SerializeField] private float _clickAnimationDuration = 0.25f;
        [SerializeField] private Vector2 _clickedScale = Vector2.one;

        [Space(5), Header("Pulse Animation Setup")]
        [SerializeField] private float _pulseAnimationDuration = 1.0f;
        [SerializeField] private Vector2 _pulseScale = Vector2.one;
        [SerializeField] private bool _usingPulseAnimation = false;

        private RectTransform _rectTransform;

        private Tween _pulseTween;
        private Tween _clickUpTween;
        private Tween _clickDownTween;

        private Sequence _clickSequence;

        public bool Initialized => _rectTransform != null;

        public void Init(RectTransform target)
        {
            _rectTransform = target;

            if (_usingPulseAnimation)
                PulseAnimation();
        }

        public void StopAnimations()
        {
            _rectTransform.DOKill();
        }

        public void StopPulseAnimation() => _pulseTween?.Kill();

        public void PulseAnimation()
        {
            _pulseTween?.Kill();

            _pulseTween = _rectTransform
                .DOScale(_pulseScale, _pulseAnimationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void ClickDownAnimation()
        {
            _clickUpTween?.Kill();
            _clickDownTween?.Kill();

            _clickDownTween = _rectTransform
                .DOScale(_clickedScale, _clickAnimationDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => _rectTransform.localScale = _clickedScale);
        }

        public void ClickUpAnimation()
        {
            _clickDownTween?.Kill();
            _clickUpTween?.Kill();

            _clickUpTween = _rectTransform
                .DOScale(Vector2.one, _clickAnimationDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => _rectTransform.localScale = Vector2.one);
        }

        public void ButtonClickAnimation(Action onComplete = null)
        {
            _clickSequence?.Kill();

            _clickSequence = DOTween.Sequence();

            _clickSequence.Append(
                _rectTransform
                .DOScale(_clickedScale, _clickAnimationDuration)
                .SetEase(Ease.OutSine));

            _clickSequence.Append(_rectTransform
                .DOScale(Vector2.one, _clickAnimationDuration)
                .SetEase(Ease.InSine));

            _clickSequence.OnComplete(() => onComplete?.Invoke());
        }
    }
}