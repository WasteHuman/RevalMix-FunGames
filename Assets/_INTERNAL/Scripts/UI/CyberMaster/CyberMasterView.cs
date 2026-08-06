using Core.Data.Cyber21;
using DG.Tweening;
using System;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CyberMaster
{
    public class CyberMasterView : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private RectTransform _rightSlot;
        [SerializeField] private RectTransform _leftSlot;
        [SerializeField] private RectTransform _deckTransform;

        [Space(5), Header("Buttons Setup")]
        [SerializeField] private ActionButton _hitButton;
        [SerializeField] private ActionButton _standButton;
        [SerializeField] private ActionButton _restartButton;

        [Space(5), Header("Card Setup")]
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Sprite _cardBackSprite;

        [Space(5), Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _betText;

        [Space(5), Header("Animations Setup")]
        [SerializeField] private float _flyDuration = 0.35f;
        [SerializeField] private float _flipDuration = 0.12f;
        [SerializeField] private float _flyStartScale = 0.6f;
        [Tooltip("Если true — новая карта заменяет предыдущую в слоте")]
        [SerializeField] private bool _replacePreviousCard = false;

        [Space(5), Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Image _resultPanelEffectImage;
        [SerializeField] private Image _resultPanelCupImage;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultScoreText;
        [SerializeField] private TextMeshProUGUI _resultRewardText;
        [SerializeField] private TMP_ColorGradient _blackjackGradient;
        [SerializeField] private TMP_ColorGradient _winGradient;
        [SerializeField] private TMP_ColorGradient _loseGradient;
        [SerializeField] private Sprite _winEffect;
        [SerializeField] private Sprite _loseEffect;
        [SerializeField] private Sprite _winCupSprite;
        [SerializeField] private Sprite _loseCupSprite;

        private bool _nextCardToLeft = true;

        public event Action OnHitButtonClicked;
        public event Action OnStandButtonClicked;
        public event Action OnRestartButtonClicked;

        private void InitButtons()
        {
            if (_hitButton != null)
                _hitButton.OnButtonClick += HandleHitButtomClick;

            if (_standButton != null)
                _standButton.OnButtonClick += HandleStandButtonClick;

            if (_restartButton != null)
            {
                _restartButton.OnButtonClick += HandleRestartButtonClick;
                _restartButton.gameObject.SetActive(false);
            }

            if (_resultPanel != null)
                _resultPanel.SetActive(false);
        }

        public void Init(int deckSize)
        {
            InitButtons();
            Debug.Log($"[CyberMasterView] Initialized with deck size: {deckSize}");
        }

        public void Dispose()
        {
            if(_hitButton != null)
                _hitButton.OnButtonClick -= HandleHitButtomClick;

            if(_standButton != null)
                _standButton.OnButtonClick -= HandleStandButtonClick;

            if(_restartButton != null)
                _restartButton.OnButtonClick -= HandleRestartButtonClick;

            KillCardTweens(_leftSlot);
            KillCardTweens(_rightSlot);
        }

        public void ClearHand()
        {
            _nextCardToLeft = true;
            ClearSlot(_leftSlot);
            ClearSlot(_rightSlot);
        }

        public void AddCardToHand(CardData cardData, float delay = 0f)
        {
            if (_deckTransform == null || _cardPrefab == null)
            {
                Debug.LogError("[CyberMasterView] Deck transform or card prefab is not assigned!");
                return;
            }

            // Выбираем слот по кругу: лево -> право -> лево...
            Transform targetSlot = _nextCardToLeft ? _leftSlot : _rightSlot;
            _nextCardToLeft = !_nextCardToLeft;

            if (targetSlot == null)
            {
                Debug.LogError("[CyberMasterView] Left/Right slot is not assigned!");
                return;
            }

            // Режим замены: старая карта схлопывается, новая занимает её место
            if (_replacePreviousCard)
            {
                foreach (Transform child in targetSlot)
                {
                    child.DOKill();
                    child.DOScale(0f, 0.15f).OnComplete(() => Destroy(child.gameObject));
                }
            }

            int indexInSlot = targetSlot.childCount;

            GameObject cardObj = Instantiate(_cardPrefab, targetSlot);
            RectTransform rect = cardObj.GetComponent<RectTransform>();

            // Стартуем рубашкой вверх из колоды
            SetCardFace(cardObj, false, cardData);
            rect.position = _deckTransform.position;
            rect.localScale = Vector3.one * (delay > 0f ? 0f : _flyStartScale);

            // Целевая точка внутри слота
            Vector3 targetLocal = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            if (delay > 0f)
            {
                seq.AppendInterval(delay);
                seq.AppendCallback(() => rect.localScale = Vector3.one * _flyStartScale);
            }

            // 1) Перелёт из колоды в слот + рост масштаба + лёгкий наклон
            seq.Append(rect.DOMove(targetSlot.TransformPoint(targetLocal), _flyDuration).SetEase(Ease.OutCubic));
            seq.Join(rect.DOScale(1f, _flyDuration).SetEase(Ease.OutCubic));
            seq.Join(rect.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f)), _flyDuration).SetEase(Ease.OutCubic));

            // 2) Флип: сжатие по X -> подмена рубашки на лицо -> раскрытие
            seq.Append(rect.DOScaleX(0f, _flipDuration).SetEase(Ease.InQuad));
            seq.AppendCallback(() => SetCardFace(cardObj, true, cardData));
            seq.Append(rect.DOScaleX(1f, _flipDuration).SetEase(Ease.OutBack));
            seq.Join(rect.DORotate(Vector3.zero, _flipDuration * 2f).SetEase(Ease.OutQuad));

            // Финальная доводка, чтобы карта встала ровно в слот
            seq.OnComplete(() => rect.localPosition = targetLocal);
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"{score}";

                // Подсветка, если близко к перебору
                if (score > 18)
                    _scoreText.color = Color.yellow;
                else if (score > 15)
                    _scoreText.color = Color.white;
                else
                    _scoreText.color = Color.green;
            }
        }

        public void UpdateBet(int bet)
        {
            if (_betText != null)
                _betText.text = $"Bet: {bet}";
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (_hitButton != null)
                _hitButton.Interactable = interactable;

            if (_standButton != null)
                _standButton.Interactable = interactable;
        }

        public void SetRestartButtonInteractable(bool interactable)
        {
            if (_restartButton != null)
            {
                _restartButton.gameObject.SetActive(interactable);
                _restartButton.Interactable = interactable;
            }
        }

        public void ShowResult(bool isWin, float reward, int finalScore, bool isBlackjack)
        {
            if (_resultPanel == null)
                return;

            _resultPanel.SetActive(true);

            if (_resultTitleText != null)
            {
                if (isBlackjack)
                {
                    _resultTitleText.text = "Blackjack!";
                    _resultTitleText.colorGradientPreset = _blackjackGradient;
                    _resultPanelEffectImage.sprite = _winEffect;
                    _resultPanelCupImage.sprite = _winCupSprite;
                }
                else if (isWin)
                {
                    _resultTitleText.text = "Win!";
                    _resultTitleText.colorGradientPreset = _winGradient;
                    _resultPanelEffectImage.sprite = _winEffect;
                    _resultPanelCupImage.sprite = _winCupSprite;
                }
                else
                {
                    _resultTitleText.text = "Lose!";
                    _resultTitleText.colorGradientPreset = _loseGradient;
                    _resultPanelEffectImage.sprite = _loseEffect;
                    _resultPanelCupImage.sprite = _loseCupSprite;
                }
            }

            if (_resultScoreText != null)
                _resultScoreText.text = $"Final Score: {finalScore}";

            if (_resultRewardText != null)
            {
                if (isWin)
                    _resultRewardText.text = $"+{reward} Coins";
                else
                    _resultRewardText.text = "No reward";
            }
        }

        public void HideResult()
        {
            if (_resultPanel != null)
                _resultPanel.SetActive(false);
        }

        private void SetCardFace(GameObject cardObj, bool showFace, CardData data)
        {
            if (cardObj.TryGetComponent<Image>(out var image))
            {
                int randomIndex = UnityEngine.Random.Range(0, data.CardSprites.Count);
                image.sprite = showFace ? data.CardSprites[randomIndex] : _cardBackSprite;
            }

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.gameObject.SetActive(true);
                if (showFace)
                    label.text = data.IsAce ? "A" : data.CardValue.ToString();
            }
        }

        private void ClearSlot(Transform slot)
        {
            if (slot == null) return;

            foreach (Transform child in slot)
            {
                child.DOKill();
                Destroy(child.gameObject);
            }
        }

        private void KillCardTweens(Transform slot)
        {
            if (slot == null) return;

            foreach (Transform child in slot)
                child.DOKill();
        }

        private void HandleRestartButtonClick() => OnRestartButtonClicked?.Invoke();

        private void HandleStandButtonClick() => OnStandButtonClicked?.Invoke();

        private void HandleHitButtomClick() => OnHitButtonClicked?.Invoke();
    }
}