using Core.Common;
using Core.Data;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using UI.CryptoVibe;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class CryptoVibeController : GameController
    {
        [Header("Setup")]
        [SerializeField] private CryptoVibeView _view;

        [Header("Settings")]
        [SerializeField] private float _minBet = 250f;
        [SerializeField] private float _maxMultiplier = 35f;
        [SerializeField] private float _growthRate = 0.5f;

        private CrashResultGenerator _resultGenerator;

        private float _currentBet;
        private float _currentMultiplier;
        private float _crashMultiplier;

        private bool _isPlaying;
        private bool _hasCrashed;

        public override void Initialize()
        {
            _resultGenerator =
                new CrashResultGenerator(_maxMultiplier);

            _view.OnStartClicked += HandleStartClick;
            _view.OnEjectClicked += HandleEjectClick;
            _view.OnBetChanged += HandleBetChanged;
            _view.OnRestartButtonClicked += HandleRestartButtonClick;

            HandleRestartButtonClick();
        }

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(
                GameConstants.GAME_CRYPTO_VIBE
            );

            _currentBet = _minBet;

            _view.UpdateBetText(_currentBet);
        }

        public override void Exit()
        {
            _view.OnStartClicked -= HandleStartClick;
            _view.OnEjectClicked -= HandleEjectClick;
            _view.OnBetChanged -= HandleBetChanged;
            _view.OnRestartButtonClicked -= HandleRestartButtonClick;
        }

        private async UniTask StartGame()
        {
            _isPlaying = true;
            _hasCrashed = false;

            _currentMultiplier = 1f;

            _crashMultiplier = _resultGenerator.Generate();

            _view.SetInteractable(true);

            Debug.Log($"[CryptoVibe] Crash: {_crashMultiplier:F2}x");

            _view.PlayFlyAnimation(_crashMultiplier, _growthRate);

            while (_isPlaying && !_hasCrashed)
            {
                _currentMultiplier += _growthRate * Time.deltaTime;

                if (_currentMultiplier >= _crashMultiplier)
                {
                    _currentMultiplier = _crashMultiplier;

                    _view.UpdateMultiplierText(_currentMultiplier);

                    TriggerCrash();
                    break;
                }

                _view.UpdateMultiplierText(_currentMultiplier);

                await UniTask.Yield();
            }
        }

        private void TriggerCrash() => _view.Crash(HandleCrashResult);

        private void HandleCrashResult()
        {
            _view.SetInteractable(false);

            EndGame(
                isWin: false,
                reward: 0,
                questTag: null
            );
        }

        // ------------------------------------------------------------------
        // EJECT
        // ------------------------------------------------------------------

        private void EjectRocket()
        {
            if (!_isPlaying || _hasCrashed)
                return;

            _isPlaying = false;

            float reward =
                _currentBet * _currentMultiplier;

            string questTag =
                _currentMultiplier >= 10f
                    ? GameConstants.TAG_REACH_10X_MULTIPLIER
                    : GameConstants.TAG_LAUNCH_3_ROCKETS;

            EndGame(
                isWin: true,
                reward: Mathf.RoundToInt(reward),
                questTag: questTag
            );
        }

        // ------------------------------------------------------------------
        // END
        // ------------------------------------------------------------------

        private void EndGame(
            bool isWin,
            int reward,
            string questTag)
        {
            _isPlaying = false;

            GameResult result = new(
                isWin: isWin,
                rewardCoins: reward,
                rewardXP: isWin ? 30f : 10f,
                questTag: questTag,
                gameId: GameConstants.GAME_CRYPTO_VIBE
            );

            GameServices.GameCompletionHandler
                .HandleGameResult(result);

            _view.ShowResult(
                isWin,
                reward
            );
        }

        // ------------------------------------------------------------------
        // INPUT
        // ------------------------------------------------------------------

        private void HandleStartClick()
        {
            if (_isPlaying)
                return;

            if (!GameServices.EconomyService
                .HasEnoughBalance(_currentBet))
            {
                Debug.LogWarning(
                    "[CryptoVibe] Not enough coins!"
                );

                return;
            }

            GameServices.EconomyService
                .SpendCoins(_currentBet);

            StartGame().Forget();
        }

        private void HandleEjectClick()
        {
            EjectRocket();
        }

        private void HandleBetChanged(float newBet)
        {
            _currentBet =
                Mathf.Clamp(
                    newBet,
                    _minBet,
                    float.MaxValue
                );

            _view.UpdateBetText(_currentBet);
        }

        private void HandleRestartButtonClick()
        {
            _isPlaying = false;
            _hasCrashed = false;

            _view.ResetView();
        }
    }
}