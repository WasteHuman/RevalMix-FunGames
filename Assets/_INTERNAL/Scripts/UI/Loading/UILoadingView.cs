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

        public void ResetProgress()
        {
            _fillTween?.Kill();
            _fillTween = null;

            if (_progressBarFill)
                _progressBarFill.fillAmount = 0f;
        }

        public void SetLoadingProgress(float progress)
        {
            _fillTween?.Kill();

            _fillTween = DOTween.To(
                () => _progressBarFill ? _progressBarFill.fillAmount : 0f,
                x => { if (_progressBarFill) _progressBarFill.fillAmount = x; },
                progress,
                _progressAnimDuration
            ).SetTarget(_progressBarFill);

            _fillTween.OnKill(() => _fillTween = null);
        }
    }
}