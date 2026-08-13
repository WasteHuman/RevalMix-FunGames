using Core.Common;
using Core.Data;
using Core.Data.Reels;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UI.Other;
using UI.Reels;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers
{
    public class ReelsController : GameController
    {
        private string KEY_IS_ARCADE_ALREADY_PLAYED = "Reels_Arcade";

        [Header("Reels")]
        [SerializeField] private List<ReelView> _reels = new();

        [Space(5), Header("UI")]
        [SerializeField] private TMP_InputField _betInputField;
        [SerializeField] private TextMeshProUGUI _autoSpinLabel;
        [SerializeField] private TextMeshProUGUI _currentBetLabel;
        [SerializeField] private TextMeshProUGUI _winAmountLabel;
        [SerializeField] private ActionButton _spinButton;
        [SerializeField] private ActionButton _betPlusButton;
        [SerializeField] private ActionButton _betMinusButton;
        [SerializeField] private ActionButton _turboButton;
        [SerializeField] private ActionButton _maxBetButton;
        [SerializeField] private ActionButton _infoButton;
        [SerializeField] private ActionButton _autoButton;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private WarningMessageView _warningMessageView;

        [Space(5), Header("Auto Spin View Setup")]
        [SerializeField] private TMP_ColorGradient _activeColor;
        [SerializeField] private TMP_ColorGradient _inactiveColor;

        [Space(5), Header("Data")]
        [SerializeField] private List<SymbolData> _symbolData = new();

        [Space(5), Header("Settings")]
        [SerializeField] private ReelsType _reelsType = ReelsType.Classic;
        [SerializeField] private int _minBet = 10;
        [SerializeField] private int _betStep = 10;
        [SerializeField] private float _baseSpinDuration = 1f;
        [SerializeField] private float _reelDelay = 0.2f;
        [SerializeField] private float _autoSpinDelay = 1f;
        [SerializeField] private List<Sprite> _symbols = new();
        [SerializeField] private int _diamondReelsMultiplier = 25;

        private float _maxBet;
        private int _currentBet;
        private bool _isSpinning;
        private bool _isTurboMode;
        private float _spinDuration;

        // Автоспин
        private bool _isAutoSpinEnabled = false;
        private UniTask _autoSpinTask;
        private CancellationTokenSource _autoSpinCts;

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_REELS);

            if (_reelsType == ReelsType.Diamond)
                KEY_IS_ARCADE_ALREADY_PLAYED = "Reels_Diamond_Arcade";

            _currentBet = _minBet;
            _isTurboMode = false;
            _spinDuration = _baseSpinDuration;
            _maxBet = Mathf.RoundToInt(GameServices.EconomyService.GetCoinsBalance() * 0.9f);

            for (int i = 0; i < _reels.Count; i++)
                _reels[i].Init(_symbols);

            if (_infoPanel != null && _infoPanel.activeSelf)
                _infoPanel.SetActive(false);
        }

        public override void Initialize()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleChangedCoinsBalance;

            if (_spinButton != null) 
                _spinButton.OnButtonClick += HandleSpinButtonClick;
            if (_betPlusButton != null) 
                _betPlusButton.OnButtonClick += HandleBetUpButtonClick;
            if (_betMinusButton != null) 
                _betMinusButton.OnButtonClick += HandleBetDownButtonClick;
            if (_turboButton != null) 
                _turboButton.OnButtonClick += HandleTurboModeButtonClick;
            if (_betInputField != null) 
                _betInputField.onEndEdit.AddListener(HandleBetChanged);
            if (_maxBetButton != null) 
                _maxBetButton.OnButtonClick += HandleMaxBetButtonClick;
            if (_infoButton != null) 
                _infoButton.OnButtonClick += HandleInfoButtonClick;
            if(_autoButton != null)
                _autoButton.OnButtonClick += HandleAutoButtonClick;

            if (_autoSpinLabel != null)
                _inactiveColor = _autoSpinLabel.colorGradientPreset;

            UpdateUI();
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleChangedCoinsBalance;

            if (_spinButton != null) 
                _spinButton.OnButtonClick -= HandleSpinButtonClick;
            if (_betPlusButton != null) 
                _betPlusButton.OnButtonClick -= HandleBetUpButtonClick;
            if (_betMinusButton != null) 
                _betMinusButton.OnButtonClick -= HandleBetDownButtonClick;
            if (_turboButton != null) 
                _turboButton.OnButtonClick -= HandleTurboModeButtonClick;
            if (_betInputField != null) 
                _betInputField.onEndEdit.RemoveListener(HandleBetChanged);
            if (_maxBetButton != null) 
                _maxBetButton.OnButtonClick -= HandleMaxBetButtonClick;
            if (_infoButton != null) 
                _infoButton.OnButtonClick -= HandleInfoButtonClick;
            if (_autoButton != null)
                _autoButton.OnButtonClick -= HandleAutoButtonClick;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                _isTurboMode = !_isTurboMode;
                _spinDuration = _isTurboMode ? _baseSpinDuration / 2f : _baseSpinDuration;
                Debug.Log($"[Reels] Turbo mode: {_isTurboMode}, duration: {_spinDuration}");
            }
        }

        private void UpdateUI()
        {
            if (_betInputField != null) 
                _betInputField.text = $"{_currentBet}";

            if (_spinButton != null) 
                _spinButton.Interactable = !_isSpinning && GameServices.EconomyService.GetCoinsBalance() >= _currentBet;
            if (_betPlusButton != null) 
                _betPlusButton.Interactable = !_isSpinning && _currentBet < _maxBet;
            if (_betMinusButton != null) 
                _betMinusButton.Interactable = !_isSpinning && _currentBet > _minBet;

            if (_turboButton != null)
            {
                if (_turboButton.TryGetComponent<Image>(out var turboImage))
                    turboImage.color = _isTurboMode ? Color.green : Color.white;
            }
        }

        private async UniTask StartSpin()
        {
            if (!base.SpendEnergy())
            {
                _warningMessageView
                    .Show(() => 
                    _warningMessageView.SetWarningMessage("Not enough energy!", $"You don't have enough energy ({5}) for this game."));
                if (_isAutoSpinEnabled)
                    StopAutoSpin();
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_currentBet))
            {
                _warningMessageView
                    .Show(() =>
                    _warningMessageView.SetWarningMessage("Not enough coins!", $"You don't have enough coins ({_currentBet}) for this bet."));
                return;
            }

            GameServices.EconomyService.SpendCoins(_currentBet);

            _isSpinning = true;
            SetInteractable(false);

            int reelCount = _reels.Count;
            int[][] results = new int[reelCount][];
            int[] middleRow = new int[reelCount]; // Центральные символы для проверки выигрыша

            // 1. Генерируем результаты (лента из 3 символов для каждого барабана)
            for (int i = 0; i < reelCount; i++)
            {
                results[i] = new int[3];
                int mid = UnityEngine.Random.Range(0, _symbolData.Count);

                results[i][0] = (mid + 1) % _symbolData.Count; // Верхний
                results[i][1] = mid;                           // Средний
                results[i][2] = (mid - 1 + _symbolData.Count) % _symbolData.Count; // Нижний

                middleRow[i] = mid;
            }

            // 2. Запускаем вращение с задержками
            List<UniTask> spinTasks = new();
            var cts = new CancellationTokenSource();

            for (int i = 0; i < reelCount; i++)
            {
                float delay = i * _reelDelay;
                float duration = _spinDuration + (i * 0.2f);

                if (delay > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));

                spinTasks.Add(_reels[i].SpinAsync(duration, results[i], _isTurboMode, cts.Token));
            }

            await UniTask.WhenAll(spinTasks);

            // 3. Проверяем выигрыш по центральному ряду
            CheckWin(middleRow);

            _isSpinning = false;
            SetInteractable(true);
        }

        private async UniTask AutoSpinLoop()
        {
            Debug.Log("[Reels] Auto spin started");

            try
            {
                while (_isAutoSpinEnabled)
                {
                    // Проверяем баланс
                    if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
                    {
                        Debug.Log("[Reels] Auto spin stopped: not enough coins");
                        StopAutoSpin();
                        break;
                    }

                    // Делаем спин
                    await StartSpin();

                    // Если автоспин был остановлен во время спина (например, игрок нажал Stop)
                    if (!_isAutoSpinEnabled)
                        break;

                    // Пауза между спинами
                    await UniTask.Delay(TimeSpan.FromSeconds(_autoSpinDelay));
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение при отмене
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Reels] Auto spin error: {ex}");
                StopAutoSpin();
            }
            finally
            {
                Debug.Log("[Reels] Auto spin stopped");
                _isAutoSpinEnabled = false;
                UpdateUI();
            }
        }

        private void StartAutoSpin()
        {
            if (_isAutoSpinEnabled)
                return;

            _isAutoSpinEnabled = true;
            _autoSpinCts = new CancellationTokenSource();
            _autoSpinTask = AutoSpinLoop().AttachExternalCancellation(_autoSpinCts.Token);
            UpdateUI();
        }

        private void StopAutoSpin()
        {
            if (!_isAutoSpinEnabled)
                return;

            _isAutoSpinEnabled = false;
            _autoSpinCts?.Cancel();
            _autoSpinCts?.Dispose();
            _autoSpinCts = null;
            UpdateUI();
        }

        private void CheckWin(int[] symbolIndices)
        {
            if (symbolIndices.Length == 0)
                return;

            int matchCount = CountSequentialMatches(symbolIndices);

            bool isWin = IsWinningCombination(symbolIndices, matchCount);
            bool isAlreadyPlayed = PlayerPrefs.HasKey(KEY_IS_ARCADE_ALREADY_PLAYED);

            if (isWin)
            {
                int totalWin = CalculateWin(symbolIndices[0], matchCount);

                ShowResult(true, totalWin, symbolIndices);

                GameResult result = new(
                    isWin: true,
                    rewardCoins: totalWin,
                    rewardXP: 20,
                    questTag: GameConstants.TAG_SPIN_10_REELS,
                    gameId: GameConstants.GAME_REELS,
                    arcadePlayed: isAlreadyPlayed
                );

                Debug.Log($"WIN! {matchCount} symbols. Reward: {totalWin}");
                GameServices.GameCompletionHandler.HandleGameResult(result);
            }
            else
            {
                Debug.Log("No win");

                ShowResult(false, 0, symbolIndices);

                GameResult result = new(
                    isWin: false,
                    rewardCoins: 0,
                    rewardXP: 5,
                    questTag: GameConstants.TAG_SPIN_10_REELS,
                    gameId: GameConstants.GAME_REELS,
                    arcadePlayed: isAlreadyPlayed
                );

                GameServices.GameCompletionHandler.HandleGameResult(result);
                PlayerPrefs.SetInt(KEY_IS_ARCADE_ALREADY_PLAYED, 1);
            }
        }

        private int CalculateWin(int symbolIndex, int matchCount)
        {
            int baseReward = _symbolData[symbolIndex].BaseReward;
            int multiplier = GetMultiplier(matchCount);

            return _reelsType switch
            {
                ReelsType.Classic =>
                    baseReward * multiplier + _currentBet,

                ReelsType.Diamond =>
                    baseReward * multiplier + _currentBet * _diamondReelsMultiplier,

                _ => 0
            };
        }

        private bool IsWinningCombination(int[] symbolIndices, int matchCount)
        {
            return _reelsType switch
            {
                ReelsType.Classic => matchCount >= 2,

                ReelsType.Diamond =>
                    matchCount == 3 &&
                    IsDiamondSymbol(symbolIndices[0]),

                _ => false
            };
        }

        private int CountSequentialMatches(int[] symbolIndices)
        {
            if (symbolIndices.Length == 0)
                return 0;

            int firstSymbol = symbolIndices[0];
            int matchCount = 1;

            for (int i = 1; i < symbolIndices.Length; i++)
            {
                if (symbolIndices[i] != firstSymbol)
                    break;

                matchCount++;
            }

            return matchCount;
        }

        private bool IsDiamondSymbol(int symbolIndex)
        {
            return _symbolData[symbolIndex].Type == SymbolType.Diamond;
        }

        private int CountMatches(List<int> symbols)
        {
            if (symbols.Count == 0) 
                return 0;

            Dictionary<int, int> symbolCounts = new();
            foreach (var symbol in symbols)
            {
                if (!symbolCounts.ContainsKey(symbol))
                    symbolCounts[symbol] = 0;

                symbolCounts[symbol]++;
            }

            int maxCount = 0;
            foreach (var kvp in symbolCounts) 
                if (kvp.Value > maxCount)
                    maxCount = kvp.Value;
            return maxCount;
        }

        private int GetMultiplier(int matchCount) => matchCount switch
        {
            2 => 2,
            3 => 16,
            4 => 20,
            5 => 40,
            6 => 80,
            _ => 2,
        };

        private void ShowResult(bool isWin, int winAmount, int[] middleRow)
        {
            if (_winPanel != null) 
                _winPanel.SetActive(isWin);
            if (_winAmountLabel != null) 
                _winAmountLabel.text = isWin ? $"+{winAmount}" : "0";

            Debug.Log($"[Reels] Result: Win={isWin}, Amount={winAmount}, Matches={CountMatches(new List<int>(middleRow))}");

            if (isWin && _winPanel != null) 
                Invoke(nameof(HideWinPanel), 3.5f);
        }

        private void HideWinPanel()
        {
            if (_winPanel != null)
                _winPanel.SetActive(false);
        }

        private void SetInteractable(bool interactable)
        {
            _spinButton.Interactable = interactable;
            _betPlusButton.Interactable = interactable;
            _betMinusButton.Interactable = interactable;
        }

        private void RefreshInput()
        {
            if (_betInputField != null)
                _betInputField.SetTextWithoutNotify(Mathf.FloorToInt(_currentBet).ToString());
        }

        private void SetBet(int value)
        {
            int max = Mathf.Max(_minBet, Mathf.FloorToInt(_maxBet));

            _currentBet = Mathf.Clamp(value, _minBet, max);

            _betInputField.SetTextWithoutNotify(_currentBet.ToString("N0"));

            _currentBetLabel.text = _currentBet.ToString("N0");
        }

        private void HandleSpinButtonClick()
        {
            if (_isSpinning)
                return;

            if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
            {
                Debug.LogWarning("[Reels] Not enough coins to spin");
                return;
            }

            StartSpin().Forget();
        }

        private void HandleBetUpButtonClick()
        {
            if (_isSpinning)
                return;

            if (_currentBet < _maxBet)
            {
                _currentBet = Mathf.Min(_currentBet + _betStep, Mathf.RoundToInt(_maxBet));
                UpdateUI();
            }
        }

        private void HandleBetDownButtonClick()
        {
            if (_isSpinning)
                return;

            if (_currentBet > _minBet)
            {
                _currentBet = Mathf.Max(_currentBet - _betStep, _minBet);
                UpdateUI();
            }
        }

        private void HandleTurboModeButtonClick()
        {
            _isTurboMode = !_isTurboMode;
            _spinDuration = _isTurboMode ? _baseSpinDuration / 2f : _baseSpinDuration;
            Debug.Log($"[Reels] Turbo mode: {_isTurboMode}, duration: {_spinDuration}");
        }

        private void HandleBetChanged(string raw)
        {
            if (int.TryParse(raw, out int bet))
                SetBet(bet);
            else
                RefreshInput();
        }

        private void HandleMaxBetButtonClick()
        {
            _currentBet = (int)_maxBet;
            UpdateUI();
        }

        private void HandleInfoButtonClick()
        {
            if (_infoPanel.activeSelf)
                _infoPanel.SetActive(false);
            else
                _infoPanel.SetActive(true);
        }

        private void HandleAutoButtonClick()
        {
            if (_isSpinning)
                return;

            if (_isAutoSpinEnabled)
            {
                StopAutoSpin();
                _autoSpinLabel.colorGradientPreset = _inactiveColor;
            }
            else
            {
                if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
                {
                    Debug.LogWarning("[Reels] Not enough coins for auto spin");
                    return;
                }
                StartAutoSpin();
                _autoSpinLabel.colorGradientPreset = _activeColor;
            }
        }

        private void HandleChangedCoinsBalance(float coins) => _maxBet = Mathf.RoundToInt(coins * 0.9f);
    }
}