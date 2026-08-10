using Core.Common;
using Core.Data;
using Core.Services;
using Core.SO;
using UI.Other;
using UI.Plinko;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class PlinkoController : GameController
    {
        [Header("View Setup")]
        [SerializeField] private PlinkoView _view;
        [SerializeField] private PlinkoBoardView _board;

        [Space(5), Header("Config")]
        [SerializeField] private PlinkoConfig _config;
        [SerializeField] private RectTransform _ballPrefab;
        [SerializeField] private RectTransform _boardContainer;
        [SerializeField] private ParticleSystem _hitVFXPrefab;
        [SerializeField] private AudioClip[] _hitSounds;
        [SerializeField] private AudioClip _jackpotSound;
        [SerializeField] private AudioSource _audioSource;

        [Space(5), Header("Economy Settings")]
        [SerializeField] private int _minBet = 10;
        [SerializeField] private int _betStep = 10;

        [Space(5), Header("Other Panels")]
        [SerializeField] private ResultPanelView _resultPanelView;

        private int _maxBet;
        private int _currentBet;
        private bool _isPlaying;

        public override void Enter()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleCoinsBalanceChanged;

            _view.OnBetChanged += HandleBetChanged;
            _view.OnBetChangedFallback += HandleBetChangedFallback;
            _view.OnBetUpClick += HandleBetUpClick;
            _view.OnBetDownClick += HandleBetDownClick;
            _view.OnDropButtonClick += HandleDropButtonClick;
        }

        public override void Initialize()
        {
            _maxBet = Mathf.RoundToInt(GameServices.EconomyService.GetCoinsBalance() * 0.9f);
            _currentBet = _minBet;

            _view.Init();
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleCoinsBalanceChanged;

            _view.OnBetChanged -= HandleBetChanged;
            _view.OnBetChangedFallback -= HandleBetChangedFallback;
            _view.OnBetUpClick -= HandleBetUpClick;
            _view.OnBetDownClick -= HandleBetDownClick;
            _view.OnDropButtonClick -= HandleDropButtonClick;

            _view.Dispose();
        }

        private void DropBall()
        {
            var generator = new PlinkoPathGenerator(_config, Time.frameCount);
            var path = generator.GeneratePath(_config.DropX);

            var ball = Instantiate(_ballPrefab, path.StartPoint, Quaternion.identity, _boardContainer);
            ball.SetAsFirstSibling();
            var animator = new BallAnimator(ball, _config);

            animator.Animate(path, bucketIdx => HandleFinish(bucketIdx, ball), HandlePegHit);
        }

        private void HandlePegHit(PlinkoHop hop)
        {
            // 1. Glow-спрайт гвоздя (swap и возврат внутри PlinkoBoard)
            if (_board != null)
                _board.HighlightPeg(hop.PegRow, hop.PegCol);

            // 2. Звук удара с pitch-вариацией (guard от пустого массива в Inspector)
            if (_audioSource != null && _hitSounds != null && _hitSounds.Length > 0)
            {
                var clip = _hitSounds[Random.Range(0, _hitSounds.Length)];
                _audioSource.PlayOneShot(clip, Random.Range(0.8f, 1.2f));
            }

            // 3. Частицы только на ~30% ударов — экономим перформанс на слабых девайсах
            if (_hitVFXPrefab != null && Random.value < 0.3f)
            {
                var vfx = Instantiate(_hitVFXPrefab, hop.EndPoint, Quaternion.identity);
                vfx.Play();
                Destroy(vfx.gameObject, 1f);
            }
        }

        private void HandleDropButtonClick()
        {
            if (_isPlaying)
                return;

            if (!GameServices.EconomyService.SpendCoins(_currentBet))
            {
                Debug.LogWarning("[Plinko] Not enough coins!");
                return;
            }

            _isPlaying = true;
            DropBall();
            _view.ToggleButtonsInteractable(false);
        }

        private void HandleFinish(int bucketIdx, Transform ball)
        {
            var bucket = _config.Buckets[bucketIdx];

            if (bucket.Multiplier == 6.17f && _jackpotSound != null)
            {
                _audioSource.PlayOneShot(_jackpotSound);
                // TODO: confetti через общий PopupController
            }

            Destroy(ball.gameObject, 0.15f);

            var rewardCoins = Mathf.RoundToInt(_currentBet * bucket.Multiplier);
            if (rewardCoins > 0)
                GameServices.EconomyService.AddCoins(rewardCoins);

            bool isWin = bucket.Multiplier > 0f;

            if (isWin)
                _resultPanelView.ShowResultPanel(isWin, false, rewardCoins);
            else
                _resultPanelView.ShowResultPanel(isWin, false, rewardCoins);

            var result = new GameResult(
                isWin: isWin,
                rewardCoins: rewardCoins,
                rewardXP: 5 * (int)Mathf.Max(1, bucket.Multiplier),
                questTag: GameConstants.TAG_DROP_10_PLINKO_BALLS,
                gameId: GameConstants.GAME_PLINKO_VIBE);

            GameServices.GameCompletionHandler.HandleGameResult(result);
            _isPlaying = false;
            _view.ToggleButtonsInteractable(true);
        }

        private void HandleBetDownClick()
        {
            if (_isPlaying || _currentBet <= _minBet) 
                return;
            _currentBet = Mathf.Max(_currentBet - _betStep, _minBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetUpClick()
        {
            if (_isPlaying || _currentBet >= _maxBet) 
                return;
            _currentBet = Mathf.Min(_currentBet + _betStep, _maxBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetChanged(int bet)
        {
            if (_isPlaying) 
                return;

            _currentBet = Mathf.Clamp(bet, _minBet, _maxBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetChangedFallback() => _view.RefreshInput(_currentBet.ToString("N0"));

        private void HandleCoinsBalanceChanged(float coins)
        {
            _maxBet = Mathf.RoundToInt(coins * 0.9f);
            if (_currentBet > _maxBet)
            {
                _currentBet = Mathf.Max(_minBet, _maxBet);
                _view.UpdateUI(_currentBet.ToString("N0"));
            }
        }
    }
}