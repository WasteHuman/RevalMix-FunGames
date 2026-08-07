using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace UI.Other
{
    public class WarningMessageView : MonoBehaviour
    {
        [Header("Labels Setup")]
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _messageLabel;

        [Space(5), Header("Buttons Setup")]
        [SerializeField] private ActionButton _closeButton;

        [Space(5), Header("Window View Setup")]
        [SerializeField] private GameObject _panel;

        private Tween _openTween;
        private Tween _closeTween;

        private void Start()
        {
            _closeButton.OnButtonClick += HandleCloseButtonClick;
        }

        private void OnDestroy()
        {
            _closeButton.OnButtonClick -= HandleCloseButtonClick;
        }

        private void HandleCloseButtonClick() => Hide();

        public void Show(Action onComplete = null)
        {
            _panel.SetActive(true);

            _openTween?.Kill();

            _openTween = DOTween.Sequence()
                .Append(_panel.transform.DOScale(Vector3.one, 0.15f))
                .Append(_titleLabel.DOFade(1f, 0.3f))
                .Join(_messageLabel.DOFade(1f, 0.3f))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Hide()
        {
            _closeTween?.Kill();
            _closeTween = DOTween.Sequence()
                .Append(_titleLabel.DOFade(0f, 0.3f))
                .Join(_messageLabel.DOFade(0f, 0.3f))
                .Append(_panel.transform.DOScale(Vector3.zero, 0.15f))
                .OnComplete(() => _panel.SetActive(false));
        }

        public void SetWarningMessage(string title, string message)
        {
            _titleLabel.text = title;
            _messageLabel.text = message;
        }
    }
}