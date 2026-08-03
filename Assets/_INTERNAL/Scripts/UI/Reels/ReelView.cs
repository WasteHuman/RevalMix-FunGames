using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Reels
{
    public class ReelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private Image _symbolTemplate;

        [Header("Settings")]
        [SerializeField] private float _symbolHeight = 100f;
        [SerializeField] private int _visibleCount = 3;
        [SerializeField] private Ease _spinEase = Ease.OutCubic;
        [SerializeField] private float _bounceAmplitude = 0.18f; // в долях высоты символа, 0 = без отскока
        [SerializeField] private float _bounceDuration = 0.12f;

        private Vector3 _originalPosition;
        private float _spinSpeed = 1f;

        private readonly List<Image> _pool = new();
        private List<Sprite> _sprites;
        private int _center;
        private bool _isSpinning;
        private Sequence _spinSequence;

        public int GetCenterSymbolIndex() => _center;
        public bool IsSpinning => _isSpinning;

        public void Init(List<Sprite> sprites)
        {
            _originalPosition = _content.localPosition;

            _sprites = sprites;
            ShowRestWindow();
        }

        private void OnDisable() => StopSpin();

        public async UniTask SpinAsync(float duration, int target, bool isTurbo = false)
        {
            if (_isSpinning || _sprites == null || _sprites.Count == 0)
                return;

            _isSpinning = true;

            int half = _visibleCount / 2;
            int filler = 20 + Random.Range(0, 6); // сколько символов "пройдёт" мимо
            SetSpeed(isTurbo);

            var column = new List<int>(filler + _visibleCount * 2);
            for (int o = -half; o <= half; o++)
                column.Add(Mod(_center - o));
            for (int i = 0; i < filler; i++)
                column.Add(Random.Range(0, _sprites.Count));
            for (int o = -half; o <= half; o++)
                column.Add(Mod(target - o));

            int shift = filler + _visibleCount;
            float endY = -shift * _symbolHeight;

            SetPoolSize(column.Count);
            for (int j = 0; j < column.Count; j++)
                Place(_pool[j], column[j], j - half);

            _content.localPosition = _originalPosition;

            _spinSequence = DOTween.Sequence();
            _spinSequence.Append(
                _content.DOLocalMoveY(endY, duration)
                        .SetEase(isTurbo ? Ease.Linear : _spinEase));

            if (!isTurbo && _bounceAmplitude > 0f)
            {
                _spinSequence.Append(
                    _content.DOLocalMoveY(_originalPosition.y - shift * _symbolHeight, duration)
                            .SetEase(Ease.OutSine));
                _spinSequence.Append(
                    _content.DOLocalMoveY(endY, _bounceDuration)
                            .SetEase(Ease.InSine));
            }

            await _spinSequence.AsyncWaitForCompletion();

            _center = target;
            ShowRestWindow();
            _spinSequence = null;
            _isSpinning = false;
        }

        public void StopSpin()
        {
            if (_spinSequence != null && _spinSequence.IsActive())
            {
                _spinSequence.Kill();
                _spinSequence = null;
            }

            _isSpinning = false;

            if (_sprites != null && _sprites.Count > 0)
                ShowRestWindow();
        }

        /// <summary>
        /// Состояние покоя: ровно _visibleCount символов, контент на нуле.
        /// </summary>
        private void ShowRestWindow()
        {
            int half = _visibleCount / 2;

            SetPoolSize(_visibleCount);
            for (int i = 0; i < _visibleCount; i++)
            {
                int o = half - i; // +half (верх) .. -half (низ)
                Place(_pool[i], Mod(_center - o), o);
            }

            _content.localPosition = _originalPosition;
        }

        private void SetSpeed(bool isTurbo)
        {
            _spinSpeed = isTurbo ? 2f : 1f;
            if (_spinSequence != null && _spinSequence.IsActive())
                _spinSequence.timeScale = _spinSpeed;
        }

        private void Place(Image img, int symbolIndex, float rowOffset)
        {
            img.sprite = _sprites[symbolIndex];
            img.enabled = img.sprite != null;
            img.raycastTarget = false;
            img.rectTransform.localPosition = new Vector3(0f, rowOffset * _symbolHeight, 0f);
        }

        private void SetPoolSize(int n)
        {
            while (_pool.Count < n)
            {
                var img = Instantiate(_symbolTemplate, _content);
                img.gameObject.SetActive(true);
                _pool.Add(img);
            }

            for (int i = 0; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(i < n);
        }

        private int Mod(int v)
        {
            int n = _sprites.Count;
            return ((v % n) + n) % n;
        }
    }
}