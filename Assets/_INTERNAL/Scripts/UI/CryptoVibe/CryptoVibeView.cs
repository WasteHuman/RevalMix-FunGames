using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CryptoVibe
{
    public class CryptoVibeView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _betInputField;
        [SerializeField] private TextMeshProUGUI _currentBetLabel;
        [SerializeField] private TextMeshProUGUI _multiplierLabel;
        [SerializeField] private ActionButton _startButton;
        [SerializeField] private ActionButton _ejectButton;
        [SerializeField] private ResultPanelView _resultPanelView;

        [Space(5), Header("Visuals")]
        [SerializeField] private RectTransform _graphContainer;
        [SerializeField] private RectTransform _rocketTransform;
        [SerializeField] private Image _rocketImage;

        [SerializeField] private Sprite _rocketSprite;
        [SerializeField] private Sprite _crashedRocketSprite;

        [Space(5), Header("Fly Settings")]
        [SerializeField] private List<RectTransform> _flyWaypoints = new();
        [SerializeField] private List<RectTransform> _fallWaypoints = new();
        [SerializeField, Min(0.1f)] private float _flightBaseDuration = 3f;
        [SerializeField, Min(0.1f)] private float _fallDuration = 1f;
        [SerializeField] private float _fallSpeed = 500f;
        [SerializeField] private AnimationCurve _crashProgressCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private Vector2 _originalRocketPosition;

        private Tween _fallTween;
        private Sequence _rocketSequence;

        private float _crashProgress;

        public event Action OnStartClicked;
        public event Action OnEjectClicked;
        public event Action OnRestartButtonClicked;
        public event Action<float> OnBetChanged;

        private void Start()
        {
            if (_startButton != null)
                _startButton.OnButtonClick += HandleStartButtonClick;

            if (_ejectButton != null)
                _ejectButton.OnButtonClick += HandleEjectButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.AddListener(HandleBetInput);

            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick += HandleRestartButtonClick;

            if (_rocketTransform != null)
                _originalRocketPosition = _rocketTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            if (_startButton != null)
                _startButton.OnButtonClick -= HandleStartButtonClick;

            if (_ejectButton != null)
                _ejectButton.OnButtonClick -= HandleEjectButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.RemoveListener(HandleBetInput);

            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick -= HandleRestartButtonClick;
        }

        // ------------------------------------------------------------------
        // RESET
        // ------------------------------------------------------------------

        public void ResetView()
        {
            if (_rocketTransform != null)
            {
                _rocketTransform.anchoredPosition = _originalRocketPosition;
                _rocketTransform.gameObject.SetActive(true);
            }

            if (_rocketImage != null)
                _rocketImage.sprite = _rocketSprite;

            if (_graphContainer != null)
                _graphContainer.anchoredPosition = Vector2.zero;

            UpdateMultiplierText(1f);
            SetInteractable(false);
            _rocketSequence?.Kill();
            _fallTween?.Kill();
        }

        // ------------------------------------------------------------------
        // GRAPH
        // ------------------------------------------------------------------

        public void PlayFlyAnimation(float crashMultiplier, float growRate = 0.5f)
        {
            _rocketSequence?.Kill();
            _ejectButton.Interactable = true;

            float normalizedMultiplier = Mathf.InverseLerp(
                1f,
                10f,
                crashMultiplier
            );

            _crashProgress = _crashProgressCurve.Evaluate(normalizedMultiplier);

            float duration = _flightBaseDuration * crashMultiplier * growRate;

            _rocketSequence = DOTween.Sequence();

            _rocketSequence
                .Append(
                    _rocketTransform
                        .DOPath(
                            GetPath(_flyWaypoints),
                            duration,
                            PathType.CatmullRom
                        )
                        .SetEase(Ease.Linear)
                );
        }

        public void Crash(Action onComplete)
        {
            _rocketSequence?.Kill();
            _ejectButton.Interactable = false;

            if (_rocketImage != null)
                _rocketImage.sprite = _crashedRocketSprite;

            PlayFallAnimationTwoStep(_rocketTransform.anchoredPosition, onComplete);
        }

        private void PlayFallAnimationTwoStep(Vector3 currentPosition, Action onComplete)
        {
            int startIndex = GetStartIndexForFall(currentPosition);

            if (startIndex < 0)
            {
                Vector3 fallback = GetFallbackTarget(currentPosition);

                _fallTween = _rocketTransform
                    .DOMove(fallback, 1f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => onComplete?.Invoke());

                return;
            }

            Vector3 connectionPoint = _fallWaypoints[startIndex].position;
            connectionPoint.z = currentPosition.z;

            var remainingPath = new List<Vector3>
            {
                connectionPoint
            };

            for (int i = startIndex + 1; i < _fallWaypoints.Count; i++)
            {
                Vector3 point = _fallWaypoints[i].position;

                if (point.y > currentPosition.y + 0.001f)
                    continue;

                point.z = currentPosition.z;
                AddPointIfNotTooClose(remainingPath, point);
            }

            _rocketSequence?.Kill();
            _rocketSequence = DOTween.Sequence();

            float connectionDistance = Vector3.Distance(currentPosition, connectionPoint);

            if (connectionDistance > 0.01f)
            {
                float connectionDuration = connectionDistance / _fallSpeed;

                _rocketSequence.Append(
                    _rocketTransform
                        .DOMove(connectionPoint, connectionDuration)
                        .SetEase(Ease.Linear)
                );
            }

            if (remainingPath.Count > 1)
            {
                Vector3[] path = remainingPath.ToArray();
                float duration = GetApproximatePathLength(path) / _fallSpeed;

                _rocketSequence.Append(
                    _rocketTransform
                        .DOPath(
                            path,
                            duration,
                            PathType.CatmullRom
                        )
                        .SetEase(Ease.InQuad)
                );
            }

            _rocketSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Vector3 GetFallbackTarget(Vector3 currentPosition)
        {
            Vector3 target = currentPosition;

            target.y -= 500f;

            return target;
        }

        private void AddPointIfNotTooClose(List<Vector3> points, Vector3 point)
        {
            const float minSqrDistance = 0.0001f;

            if ((points[^1] - point).sqrMagnitude > minSqrDistance)
                points.Add(point);
        }

        private float GetApproximatePathLength(Vector3[] path)
        {
            float length = 0f;

            for (int i = 1; i < path.Length; i++)
                length += Vector3.Distance(path[i - 1], path[i]);

            return length;
        }

        private int GetStartIndexForFall(Vector3 currentPosition)
        {
            if (_fallWaypoints == null || _fallWaypoints.Count == 0)
                return -1;

            int bestIndex = -1;
            float bestY = float.NegativeInfinity;

            for (int i = 0; i < _fallWaypoints.Count; i++)
            {
                Vector3 point = _fallWaypoints[i].position;

                if (point.y <= currentPosition.y && point.y > bestY)
                {
                    bestY = point.y;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private Vector3[] GetPath(List<RectTransform> path)
        {
            return path
                .Select(point => point.position)
                .ToArray();
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------

        public void UpdateBetText(float bet)
        {
            if (_currentBetLabel != null)
                _currentBetLabel.text = $"{bet:F0}";

            if (_betInputField != null && !_betInputField.isFocused)
                _betInputField.text = bet.ToString("F0");
        }

        public void UpdateMultiplierText(float multiplier)
        {
            if (_multiplierLabel != null)
                _multiplierLabel.text = $"{multiplier:F2}x";
        }

        public void SetInteractable(bool isPlaying)
        {
            if (_startButton != null)
            {
                if (!_startButton.gameObject.activeSelf)
                    _startButton.ForceInit();

                _startButton.gameObject.SetActive(!isPlaying);
            }

            if (_ejectButton != null)
            {
                if (!_ejectButton.gameObject.activeSelf)
                    _ejectButton.ForceInit();

                _ejectButton.gameObject.SetActive(isPlaying);
            }

            if (_betInputField != null)
                _betInputField.interactable = !isPlaying;
        }

        public void PlayCrashEffect()
        {
            if (_rocketImage != null)
                _rocketImage.sprite = _crashedRocketSprite;
        }

        public void ShowResult(bool isWin, int reward)
        {
            if (_resultPanelView != null)
                _resultPanelView.ShowResultPanel(
                    isWin,
                    false,
                    reward
                );
        }

        // ------------------------------------------------------------------
        // INPUT
        // ------------------------------------------------------------------

        private void HandleBetInput(string input)
        {
            if (float.TryParse(input, out float bet))
                OnBetChanged?.Invoke(bet);
            else
                OnBetChanged?.Invoke(0f);
        }

        private void HandleStartButtonClick()
        {
            OnStartClicked?.Invoke();
        }

        private void HandleEjectButtonClick()
        {
            OnEjectClicked?.Invoke();
        }

        private void HandleRestartButtonClick()
        {
            OnRestartButtonClicked?.Invoke();
        }
    }
}