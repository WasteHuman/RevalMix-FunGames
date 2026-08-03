using Core.Common;
using Core.Data;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UI.Reels;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers
{
    public class ReelsController : GameController
    {
        [Header("Reels")]
        [SerializeField] private List<ReelView> _reels = new();

        [Space(5), Header("UI")]
        [SerializeField] private TMP_InputField _betInputField;
        [SerializeField] private TextMeshProUGUI _currentBetLabel;
        [SerializeField] private TextMeshProUGUI _winAmountLabel;
        [SerializeField] private ActionButton _spinButton;
        [SerializeField] private ActionButton _betPlusButton;
        [SerializeField] private ActionButton _betMinusButton;
        [SerializeField] private ActionButton _turboButton;
        [SerializeField] private ActionButton _maxBetButton;
        [SerializeField] private ActionButton _infoButton;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _infoPanel;

        [Space(5), Header("Data")]
        [SerializeField] private List<SymbolData> _symbolData = new();

        [Space(5), Header("Settings")]
        [SerializeField] private int _minBet = 10;
        [SerializeField] private int _betStep = 10;
        [SerializeField] private float _baseSpinDuration = 1f;
        [SerializeField] private float _reelDelay = 0.2f;
        [SerializeField] private List<Sprite> _symbols = new();

        private float _maxBet;
        private int _currentBet;
        private bool _isSpinning;
        private bool _isTurboMode;
        private float _spinDuration;

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_REELS);

            _currentBet = _minBet;
            _isTurboMode = false;
            _spinDuration = _baseSpinDuration;

            for(int i = 0; i < _reels.Count; i++)
                _reels[i].Init(_symbols);

            if(_infoPanel != null && _infoPanel.activeSelf)
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
                _turboButton.OnButtonClick += HandleTurboMoeButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.AddListener(HandleBetChanged);

            if(_maxBetButton != null)
                _maxBetButton.OnButtonClick += HandleMaxBetButtonClick;

            if(_infoButton != null)
                _infoButton.OnButtonClick += HandleInfoButtonClick;

            GameServices.EconomyService.RequestCoinsBalance();
            UpdateUI();
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleChangedCoinsBalance;

            if (_spinButton != null)
                _spinButton.OnButtonClick -= HandleSpinButtonClick;

            if (_betPlusButton != null)
                _betPlusButton.OnButtonClick -= HandleBetUpButtonClick;

            if (_betMinusButton != null)
                _betMinusButton.OnButtonClick -= HandleBetDownButtonClick;

            if (_turboButton != null)
                _turboButton.OnButtonClick -= HandleTurboMoeButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.RemoveListener(HandleBetChanged);

            if (_maxBetButton != null)
                _maxBetButton.OnButtonClick -= HandleMaxBetButtonClick;

            if (_infoButton != null)
                _infoButton.OnButtonClick -= HandleInfoButtonClick;
        }

        private void Update()
        {
            // Turbo mode by R key
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
            GameServices.EconomyService.SpendCoins(_currentBet);

            _isSpinning = true;
            SetInteractable(false);

            // 1. Определяем результат заранее (RNG)
            int reelCount = _reels.Count;
            int[] results = new int[reelCount];
            for (int i = 0; i < reelCount; i++)
                results[i] = UnityEngine.Random.Range(0, _symbolData.Count);

            // 2. Запускаем вращение барабанов по очереди
            List<UniTask> spinTasks = new();
            for (int i = 0; i < reelCount; i++)
            {
                float delay = i * _reelDelay;
                float duration = _spinDuration + (i * 0.2f); // Каждый следующий крутится чуть дольше

                // Задержка перед стартом конкретного барабана
                if (delay > 0) 
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));

                spinTasks.Add(_reels[i].SpinAsync(duration, results[i], _isTurboMode));
            }

            // Ждем окончания всех вращений
            await UniTask.WhenAll(spinTasks);

            // 3. Проверяем выигрыш
            CheckWin(results);

            _isSpinning = false;
            SetInteractable(true);
        }

        private void CheckWin(int[] symbolIndices)
        {
            // Логика подсчета одинаковых символов подряд слева направо
            if (symbolIndices.Length == 0) 
                return;

            int firstSymbol = symbolIndices[0];
            int matchCount = 1;

            for (int i = 1; i < symbolIndices.Length; i++)
            {
                if (symbolIndices[i] == firstSymbol)
                    matchCount++;
                else
                    break;
            }

            bool isWin = matchCount >= 2; // Минимум 2 для выигрыша

            if (isWin)
            {
                int baseReward = _symbolData[firstSymbol].BaseReward;
                int multiplier = GetMultiplier(matchCount);
                int totalWin = baseReward * multiplier + _currentBet;

                GameServices.EconomyService.AddCoins(totalWin);
                GameServices.PlayerService.AddXP(20);
                ShowResult(isWin, totalWin);
                Debug.Log($"WIN! {matchCount} symbols. Reward: {totalWin}");
            }
            else
            {
                GameServices.PlayerService.AddXP(5);
                Debug.Log("No win");
            }
        }

        private int CountMatches(List<int> symbols)
        {
            if (symbols.Count == 0) 
                return 0;

            // Найти наиболее частый символ
            Dictionary<int, int> symbolCounts = new();
            foreach (var symbol in symbols)
            {
                if (!symbolCounts.ContainsKey(symbol))
                    symbolCounts[symbol] = 0;
                symbolCounts[symbol]++;
            }

            int maxCount = 0;
            foreach (var kvp in symbolCounts)
            {
                if (kvp.Value > maxCount)
                    maxCount = kvp.Value;
            }

            return maxCount;
        }

        private int GetMultiplier(int matchCount)
        {
            return matchCount switch
            {
                2 => 2,
                3 => 16,
                4 => 20,
                5 => 40,
                _ => 2,
            };
        }

        private void ShowResult(bool isWin, int winAmount)
        {
            if (_winPanel != null)
                _winPanel.SetActive(isWin);

            if (_winAmountLabel != null)
                _winAmountLabel.text = isWin ? $"+{winAmount}" : "0";

            Debug.Log($"[Reels] Result: Win={isWin}, Amount={winAmount}, Matches={CountMatches(new List<int>(_reels.ConvertAll(r => r.GetCenterSymbolIndex())))}");

            // Скрыть панель победы через 3.5 секунды
            if (isWin && _winPanel != null)
                Invoke(nameof(HideWinPanel), 3.5f);
        }

        private void HideWinPanel()
        {
            if (_winPanel != null)
                _winPanel.SetActive(false);
        }

        private void SetBet(int value)
        {
            int max = Mathf.Max(_minBet, Mathf.FloorToInt(_maxBet));

            _currentBet = Mathf.Clamp(value, _minBet, max);

            _betInputField.SetTextWithoutNotify(_currentBet.ToString("N0"));

            _currentBetLabel.text = _currentBet.ToString("N0");
        }

        private void RefreshInput()
        {
            if (_betInputField != null)
                _betInputField.SetTextWithoutNotify(Mathf.FloorToInt(_currentBet).ToString());
        }

        private void SetInteractable(bool interactable)
        {
            _spinButton.Interactable = interactable;
            _betPlusButton.Interactable = interactable;
            _betMinusButton.Interactable = interactable;
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

        private void HandleTurboMoeButtonClick()
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
            if(_infoPanel.activeSelf)
                _infoPanel.SetActive(false);
            else
                _infoPanel.SetActive(true);
        }

        private void HandleChangedCoinsBalance(float coins) => _maxBet = Mathf.RoundToInt(coins * 0.9f);
    }
}