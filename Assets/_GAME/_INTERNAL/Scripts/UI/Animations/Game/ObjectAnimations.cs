using DG.Tweening;
using System;
using UnityEngine;

namespace UI.Animations.Game
{
    public abstract class ObjectAnimations : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform _object;

        [Space(5), Header("Animation Duration Setup")]
        [SerializeField] private float _appearAnimationDuration = 0.35f;
        [SerializeField] private float _hoverAnimationDuration = 0.5f;
        [SerializeField] private float _pulseAnimationDuration = 1f;

        [Space(5), Header("Hover Animation Setup")]
        [SerializeField] private float _yMoveOffset = 1.0f;
        [SerializeField] private LoopType _hoverLoopType = LoopType.Yoyo;
        [SerializeField, Tooltip("Set -1 for infinite loops count")] private int _hoverLoopCount = -1;

        [Space(5), Header("Pulse Animation Setup")]
        [SerializeField] private float _pulseTargetScale = 0.9f;
        [SerializeField] private LoopType _pulseLoopType = LoopType.Yoyo;
        [SerializeField, Tooltip("Set -1 for infinite loops count")] private int _pulseLoopsCount = -1;
        [SerializeField] private float _pulseDelay = 0.25f;

        [Space(5), Header("Animations Flags")]
        [SerializeField] private bool _hoverAnimationEnabled = false;
        [SerializeField] private bool _pulseAnimationEnabled = false;

        private Vector3 _originalScale;

        private Tween _appearTween;
        private Tween _hoverTween;
        private Tween _pulseTween;

        private void Awake()
        {
            if (_object == null)
                _object.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (_hoverAnimationEnabled)
                HoverAnimation();

            if (_pulseAnimationEnabled)
                PulseAnimation();
        }

        private void OnDisable()
        {
            _hoverTween?.Kill();
            _appearTween?.Kill();
            _pulseTween?.Kill();
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
            _appearTween?.Kill();
            _pulseTween?.Kill();
        }

        private void HoverAnimation()
        {
            _hoverTween?.Kill();

            Vector3 originalPosition = _object.localPosition;
            float targetY = _object.localPosition.y + _yMoveOffset;

            _hoverTween = _object
                .DOLocalMoveY(targetY, _hoverAnimationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(_hoverLoopCount, _hoverLoopType)
                .OnKill(() => _object.localPosition = originalPosition);
        }

        private void PulseAnimation()
        {
            _pulseTween?.Kill();

            _originalScale = _object.localScale;

            _pulseTween = _object
                .DOScale(_pulseTargetScale, _pulseAnimationDuration)
                .SetDelay(_pulseDelay)
                .SetEase(Ease.InOutSine)
                .SetLoops(_pulseLoopsCount, _pulseLoopType);
        }

        public void Appear(Vector3 originalScale, Action onComplete = null)
        {
            _object.localScale = Vector3.zero;
            _originalScale = originalScale;

            _appearTween?.Kill();

            _appearTween = _object
                .DOScale(originalScale, _appearAnimationDuration)
                .SetEase(Ease.InOutBounce)
                .OnComplete(() => onComplete?.Invoke())
                .OnKill(() => _object.localScale = _originalScale);
        }
    }
}