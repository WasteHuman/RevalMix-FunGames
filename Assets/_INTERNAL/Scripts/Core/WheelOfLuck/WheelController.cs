using Core.Gameplay;
using Core.Services.AdsService;
using Core.Services.Analytics;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UnityEngine;

namespace Core.WheelOfLuck
{
    public class WheelController : MonoBehaviour
    {
        [Header("Wheel")]
        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private RectTransform _pointer;

        [Space(5), Header("Views")]
        [SerializeField] private List<RewardView> _rewardViews = new();

        [Space(5), Header("Spin settings")]
        [SerializeField] private float _spinDuration = 4f;
        [SerializeField] private int _minFullRotations = 4;

        [Space(5), Header("Economy")]
        [SerializeField] private int _initialFreeSpins = 1;

        [Space(5), Header("Text")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private TextMeshProUGUI _spinsCountText;

        [Space(5), Header("Buttons")]
        [SerializeField] private ActionButton _startSpinButton;
        [SerializeField] private ActionButton _watchAndCollectButton;

        [Space(5), Header("Other")]
        [SerializeField] private bool _isMultipliersWheel = false;

        [Space(5), Header("Debug")]
        [SerializeField] private bool _isDebug = false;

        private const string PREF_FREE_SPINS = "Wheel_FreeSpins";
        private const string PREF_NEXT_AVAILABLE_TICKS = "Wheel_NextAvailableTicks";
        private static readonly TimeSpan COOLDOWN = TimeSpan.FromHours(12);

        private int _freeSpins;
        private DateTime _nextAvailableUtc;
        private bool _isSpinning;
        private WheelReward _pendingReward;
        private int _pendingIndex;

        private Coroutine _cooldownCoroutine;

        private Tween _spinTween;
        private Tween _pulseTween;

        private Action _prepareAndStartSpinAction;

        public event Action<WheelReward> OnSpinStarted;
        public event Action<WheelReward> OnSpinFinished;
        public event Action OnStateChanged;
        public event Action<float> OnMultiplierDropped;

        private void Awake()
        {
            if (!_isMultipliersWheel)
            {
                AnalyticsService.Instance.ReportGameStart(SceneNames.WHEEL_OF_LUCK);

                LoadState();
                UpdateCooldownLabel();
                StartCooldownUpdater();
                UpdatePulseState();

                if (!IsAvailable())
                {
                    _startSpinButton.Interactable = false;
                    _startSpinButton.Animations.StopPulseAnimation();
                }

                return;
            }
        }

        private void Start()
        {
            if (_startSpinButton == null || _watchAndCollectButton == null)
                return;

            _prepareAndStartSpinAction = () => PrepareAndStartSpin();

            _startSpinButton.OnButtonClick += _prepareAndStartSpinAction;
            _watchAndCollectButton.OnButtonClick += ClaimWithAd;

            _watchAndCollectButton.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            if (_initialFreeSpins < 0) 
                _initialFreeSpins = 0;
        }

        private void OnDisable()
        {
            _spinTween?.Kill();
            _pulseTween?.Kill();
        }

        private void OnDestroy()
        {
            _spinTween?.Kill();
            _pulseTween?.Kill();

            if (_startSpinButton == null || _watchAndCollectButton == null)
                return;

            _startSpinButton.OnButtonClick -= _prepareAndStartSpinAction;
            _watchAndCollectButton.OnButtonClick -= ClaimWithAd;

            StopCooldownUpdater();
        }

        [ContextMenu("DEBUG: Reset Cooldown")]
        private void ResetCooldownForDebug()
        {
            _nextAvailableUtc = DateTime.MinValue;
            _freeSpins = _initialFreeSpins;
            _spinsCountText.text = $"FREE SPINS:{_freeSpins}";
            SaveState();
            UpdateCooldownLabel();
            UpdatePulseState();
            OnStateChanged?.Invoke();
            _startSpinButton.Interactable = true;
            Debug.Log("[Wheel] DEBUG: cooldown and free spins reset");
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!_isDebug) 
                return;

            // Нажмите R в режиме Play для сброса кулдауна
            if (Input.GetKeyDown(KeyCode.R))
                ResetCooldownForDebug();
        }
#endif

        private void UpdatePulseState()
        {
            if (_wheelTransform == null)
                return;

            bool shouldPulse = CanSpin();

            if (shouldPulse)
            {
                if (_pulseTween == null || !_pulseTween.IsActive())
                {
                    _pulseTween?.Kill();
                    _pulseTween = _wheelTransform
                        .DOScale(1.05f, 1f)
                        .SetEase(Ease.OutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetTarget(_wheelTransform);
                }
            }
            else
            {
                _pulseTween?.Kill();
                _pulseTween = null;
            }
        }

        private void LoadState()
        {
            if (_isMultipliersWheel)
                return;

            _freeSpins = PlayerPrefs.GetInt(PREF_FREE_SPINS, _initialFreeSpins);
            long ticks = Convert.ToInt64(PlayerPrefs.GetString(PREF_NEXT_AVAILABLE_TICKS, "0"));
            _nextAvailableUtc = ticks == 0 ? DateTime.MinValue : new DateTime(ticks, DateTimeKind.Utc);

            _spinsCountText.text = $"FREE SPINS: {_freeSpins}";
        }

        private void SaveState()
        {
            if (_isMultipliersWheel)
                return;

            PlayerPrefs.SetInt(PREF_FREE_SPINS, _freeSpins);
            PlayerPrefs.SetString(PREF_NEXT_AVAILABLE_TICKS, _nextAvailableUtc == DateTime.MinValue ? "0" : _nextAvailableUtc.Ticks.ToString());
            PlayerPrefs.Save();
        }

        public bool IsAvailable()
        {
            return DateTime.UtcNow >= _nextAvailableUtc;
        }

        public int GetFreeSpins() => _freeSpins;

        public bool CanSpin()
        {
            if (_isMultipliersWheel)
                return true;

            if (!_isSpinning && _freeSpins > 0 && IsAvailable() && _rewardViews != null && _rewardViews.Count > 0)
                return true;

            return false;
        }

        public void PrepareAndStartSpin(Action onComplete = null)
        {
            if (!CanSpin())
            {
                Debug.LogWarning("[Wheel] Невозможно начать спин. Проверьте CanSpin()");
                return;
            }

            _pulseTween?.Kill();

            _pendingIndex = SelectRewardIndexByWeight();
            _pendingReward = _rewardViews[_pendingIndex].Reward;

            OnSpinStarted?.Invoke(_pendingReward);

            StartTweenSpin(_pendingIndex, onComplete);

            if (_startSpinButton == null)
                return;

            _startSpinButton.Interactable = false;
            _startSpinButton.Animations.StopPulseAnimation();
        }

        private int SelectRewardIndexByWeight()
        {
            float total = 0f;
            foreach (var r in _rewardViews) total += Mathf.Max(0f, r.Reward.Weight);

            if (total <= 0f)
                return UnityEngine.Random.Range(0, _rewardViews.Count);

            float t = UnityEngine.Random.value * total;
            float accum = 0f;
            for (int i = 0; i < _rewardViews.Count; i++)
            {
                accum += Mathf.Max(0f, _rewardViews[i].Reward.Weight);
                if (t <= accum)
                    return i;
            }

            return _rewardViews.Count - 1;
        }

        private void StartTweenSpin(int targetIndex, Action onComplete = null)
        {
            if (_wheelTransform == null)
            {
                Debug.LogWarning("[Wheel] Wheel Transform не назначен");
                return;
            }

            _isSpinning = true;
            _spinTween?.Kill();

            RewardView targetRewardView = _rewardViews[targetIndex];
            
            if (!targetRewardView.TryGetComponent<RectTransform>(out var rewardTransform))
            {
                Debug.LogWarning("[Wheel] RewardView dont have RectTransform");
                return;
            }

            Debug.Log($"[Wheel] Spinning for reward index {targetIndex}, reward: {_pendingReward}");
            Debug.Log($"[Wheel] RewardView reward: {targetRewardView.Reward}");
            Debug.Log($"[Wheel] Are they the same? {_pendingReward == targetRewardView.Reward}");

            float currentAngle = _wheelTransform.eulerAngles.z;

            float deltaNeeded = CalculateAngleToPerfectAlignment(rewardTransform);

            float targetAbsoluteAngle = currentAngle + deltaNeeded;

            float minRequiredAngle = currentAngle + (_minFullRotations * 360f);

            while (targetAbsoluteAngle < minRequiredAngle)
            {
                targetAbsoluteAngle += 360f;
            }

            float endAngle = targetAbsoluteAngle;

            _spinTween = _wheelTransform
                .DORotate(new Vector3(0f, 0f, endAngle), _spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    _wheelTransform.eulerAngles = new Vector3(0f, 0f, endAngle);

                    _freeSpins = Mathf.Max(0, _freeSpins - 1);
                    if(_spinsCountText != null)
                        _spinsCountText.text = $"FREE SPINS: {_freeSpins}";

                    _nextAvailableUtc = DateTime.UtcNow.Add(COOLDOWN);
                    SaveState();

                    _isSpinning = false;

                    Debug.Log($"[Wheel] Reward: {_pendingReward}");

                    OnSpinFinished?.Invoke(_pendingReward);
                    OnStateChanged?.Invoke();

                    UpdateCooldownLabel();

                    if (_startSpinButton != null || _watchAndCollectButton != null)
                    {
                        UpdatePulseState();

                        if (_pendingReward.Type != WheelReward.RewardType.Nothing)
                            _watchAndCollectButton.gameObject.SetActive(true);

                        _startSpinButton.Interactable = false;
                        _startSpinButton.Animations.StopPulseAnimation();
                    }

                    onComplete?.Invoke();
                });
        }

        private float CalculateAngleToPerfectAlignment(RectTransform rewardTransform)
        {
            Vector3 wheelCenter = _wheelTransform.position;

            Vector3 rewardWorldPos = rewardTransform.position;

            Vector3 pointerWorldPos = _pointer != null ? _pointer.position : wheelCenter + Vector3.up * 100f;

            Vector3 toReward = rewardWorldPos - wheelCenter;
            Vector3 toPointer = pointerWorldPos - wheelCenter;

            float angleToReward = Mathf.Atan2(toReward.y, toReward.x) * Mathf.Rad2Deg;
            float angleToPointer = Mathf.Atan2(toPointer.y, toPointer.x) * Mathf.Rad2Deg;

            return angleToPointer - angleToReward;
        }

        public void ClaimWithoutAd()
        {
            if (_pendingReward == null)
            {
                Debug.LogWarning("[Wheel] Нет ожидаемой награды для выдачи");
                return;
            }

            ApplyReward(_pendingReward, bonusMultiplier: 1);
            _pendingReward = null;
            OnStateChanged?.Invoke();
            UpdateCooldownLabel();
            UpdatePulseState();

            _startSpinButton.Interactable = false;
        }

        public void ClaimWithAd()
        {
            if (_pendingReward == null)
            {
                Debug.LogWarning("[Wheel] No pending reward for give");
                return;
            }

            if (AdsController.Instance == null)
            {
                Debug.LogWarning("[Wheel] AdsController not found, give without ad");
                ClaimWithoutAd();
                return;
            }

            if (!AdsController.Instance.IsRewardedAdLoaded())
            {
                Debug.LogWarning("[Wheel] Rewarded ad not availvable, give without ad");
                ClaimWithoutAd();
                return;
            }

            AdsController.Instance.ShowRewardedAd(() =>
            {
                ApplyReward(_pendingReward, bonusMultiplier: 2);
                _pendingReward = null;
                OnStateChanged?.Invoke();
                UpdateCooldownLabel();
                _watchAndCollectButton.gameObject.SetActive(false);
                UpdatePulseState();
            });

            _startSpinButton.Interactable = false;
        }

        private void ApplyReward(WheelReward reward, int bonusMultiplier)
        {
            if (reward == null) 
                return;

            switch (reward.Type)
            {
                case WheelReward.RewardType.Coins:
                    int coins = (int)reward.Amount * Math.Max(1, bonusMultiplier);
                    EconomyController.Instance.AddCoins(coins);
                    Debug.Log($"[Wheel] Given coins: {coins}");

                    AnalyticsService.Instance.ReportGameWin(SceneNames.WHEEL_OF_LUCK);
                    break;

                case WheelReward.RewardType.FreeSpin:
                    int spins = (int)reward.Amount * Math.Max(1, bonusMultiplier);
                    _freeSpins += spins;
                    SaveState();
                    Debug.Log($"[Wheel] Given free spins: {spins}");

                    AnalyticsService.Instance.ReportGameWin(SceneNames.WHEEL_OF_LUCK);
                    break;

                case WheelReward.RewardType.Nothing:
                    Debug.Log("[Wheel] Nothing to give");
                    AnalyticsService.Instance.ReportGameLoss(SceneNames.WHEEL_OF_LUCK);
                    ClaimWithoutAd();
                    break;
                case WheelReward.RewardType.Multiplier:
                    if (!_isMultipliersWheel)
                        return;
                    OnMultiplierDropped?.Invoke(_pendingReward.Amount);
                    _pendingReward = null;
                    break;
            }

            if(_watchAndCollectButton != null && _startSpinButton != null)
            {
                _watchAndCollectButton.gameObject.SetActive(false);
                _startSpinButton.Interactable = true;
                UpdatePulseState();
            }
        }

        public TimeSpan GetRemainingCooldown()
        {
            if (IsAvailable()) 
                return TimeSpan.Zero;

            return _nextAvailableUtc - DateTime.UtcNow;
        }

        private void StartCooldownUpdater()
        {
            if (_cooldownText == null) 
                return;
            if (_cooldownCoroutine != null) 
                StopCoroutine(_cooldownCoroutine);
            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private void StopCooldownUpdater()
        {
            if (_cooldownCoroutine != null)
            {
                StopCoroutine(_cooldownCoroutine);
                _cooldownCoroutine = null;
            }
        }

        private IEnumerator CooldownRoutine()
        {
            while (true)
            {
                UpdateCooldownLabel();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdateCooldownLabel()
        {
            if (_cooldownText == null) 
                return;

            var remaining = GetRemainingCooldown();
            _cooldownText.text = FormatTimeSpan(remaining);
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts <= TimeSpan.Zero)
                return "00:00:00";

            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}