using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    public class UILoadingView : MonoBehaviour
    {
        [SerializeField] private float _progressAnimDuration = 0.5f;
        [SerializeField] private Image _progressBarFill;

        private Tween _fillTween;

        private void OnDestroy()
        {
            _fillTween?.Kill();
        }

        public void ResetProgress() => _progressBarFill.fillAmount = 0f;

        public void SetLoadingProgress(float progress)
        {
            _fillTween?.Kill();

            _fillTween = _progressBarFill.DOFillAmount(progress, _progressAnimDuration);
        }
    }
}