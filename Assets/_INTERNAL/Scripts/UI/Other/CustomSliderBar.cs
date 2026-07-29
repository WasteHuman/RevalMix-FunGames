using UnityEngine;

namespace UI.Other
{
    public class CustomSliderBar : MonoBehaviour
    {
        [Header("Progress Setup")]
        [SerializeField, Range(0f, 1f)] private float _progress;

        [Space(5), Header("View Setup")]
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private RectTransform _viewportRect;

        private Vector2 _originalPosition;

        public float Progress => _progress;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetProgress(_progress);
        }
#endif

        private void Start()
        {
            _originalPosition = _fillRect.anchoredPosition;
        }

        public void ResetProgress()
        {
            _progress = 0f;
            _fillRect.anchoredPosition = new(_originalPosition.x, _originalPosition.y);
        }

        public void SetProgress(float progress)
        {
            if (_viewportRect == null || _fillRect == null)
                return;

            float viewWidth = _viewportRect.rect.width;

            float targetX = -viewWidth * (1f - progress);
            _fillRect.anchoredPosition = new(targetX, _fillRect.anchoredPosition.y);
        }
    }
}