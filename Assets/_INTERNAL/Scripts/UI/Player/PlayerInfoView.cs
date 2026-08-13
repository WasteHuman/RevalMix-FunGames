using Core;
using Core.Services;
using DG.Tweening;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    public class PlayerInfoView : MonoBehaviour
    {
        [Header("Labels Setup")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private TextMeshProUGUI _xpLabel;
        [SerializeField] private TextMeshProUGUI _currentCoinsLabel;

        [Space(5), Header("Level Progress Bar Setup")]
        [SerializeField] private CustomProgressBar _sliderBar;

        [Space(5), Header("Avatar Image Setup")]
        [SerializeField] private RawImage _avatarImage;
        [SerializeField] private bool _changeAvatarSize = true;

        [Space(5), Header("Energy View Setup")]
        [SerializeField] private ActionButton _getFreeEnergyButton;
        [SerializeField] private TextMeshProUGUI _energyLabel;
        [SerializeField] private float _energyAnimationDuration = 1.5f;

        [Space(5), Header("Cheats Setup")]
        [SerializeField] private bool _isCheatActive = false;

        private int _displayedEnergy;

        private void Awake()
        {
            if(_getFreeEnergyButton != null)
                _getFreeEnergyButton.OnButtonClick += HandleGetFreeEnergyButtonClick;

            GameServices.PlayerService.OnXPChanged += HandleChangedXP;
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleCoinsBalanceChanged;
            GameServices.PlayerService.OnLevelChanged += HandleChangedLevel;
            GameServices.AvatarService.OnAvatarSetted += HandleSettetAvatar;

            if(_energyLabel != null)
            {
                GameServices.EnergyService.OnEnergyChanged += HandleEnergyChanged;
                _displayedEnergy = GameServices.EnergyService.CurrentEnergy;
                _energyLabel.text = $"{_displayedEnergy}";
            }

            GameServices.AvatarService.LoadSavedAvatar();
        }

        private void Start()
        {
            if(_nameLabel != null)
            {
                _nameLabel.enableAutoSizing = true;
                _nameLabel.text = GameServices.PlayerService.PlayerName;
            }

            if(_levelLabel != null)
                _levelLabel.text = GameServices.PlayerService.PlayerLevel.ToString();

            if(_sliderBar != null && _xpLabel != null)
                GameServices.PlayerService.RequestActualProgressState();

            GameServices.EconomyService.RequestCoinsBalance();

            if (_avatarImage != null)
            {
                if (_changeAvatarSize)
                    _avatarImage.rectTransform.sizeDelta = new(105f, 105f);

                _avatarImage.texture = GameServices.PlayerService.PlayerAvatar;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (!_isCheatActive)
                {
                    _isCheatActive = true;
                    Debug.LogWarning("Cheats Activated");
                }
                else
                {
                    _isCheatActive = false;
                    Debug.LogWarning("Cheats Deactivated");
                }
            }

            if (_isCheatActive && Input.GetKeyDown(KeyCode.R))
                GameServices.EnergyService.AddEnergy(GameConstants.MAX_ENERGY);
        }

        private void OnDestroy()
        {
            if (_getFreeEnergyButton != null)
                _getFreeEnergyButton.OnButtonClick -= HandleGetFreeEnergyButtonClick;

            GameServices.PlayerService.OnXPChanged -= HandleChangedXP;
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleCoinsBalanceChanged;
            GameServices.PlayerService.OnLevelChanged -= HandleChangedLevel;
            GameServices.AvatarService.OnAvatarSetted -= HandleSettetAvatar;

            if (_energyLabel != null)
                GameServices.EnergyService.OnEnergyChanged -= HandleEnergyChanged;
        }

        public void ToggleNameLabel(bool value)
        {
            if (value)
                _nameLabel.text = GameServices.PlayerService.PlayerName;

            _nameLabel.gameObject.SetActive(value);
        }

        private void HandleChangedXP(float xp, float requiredXP)
        {
            if (_sliderBar == null || _xpLabel == null)
                return;

            string progress = $"{xp:N0}/{requiredXP:N0} XP";
            _sliderBar.SetProgress(Mathf.Clamp01(xp / requiredXP));
            _xpLabel.text = progress;
        }

        private void HandleCoinsBalanceChanged(float amount)
        {
            if (_currentCoinsLabel == null)
                return;

            _currentCoinsLabel.text = $"{amount:N0}";
        }

        private void HandleSettetAvatar(Texture2D avatar) => _avatarImage.texture = avatar;

        private void HandleChangedLevel(int level) => _levelLabel.text = $"{level}";

        private void HandleEnergyChanged(int currentEnergy)
        {
            int oldValue = _displayedEnergy;
            _displayedEnergy = currentEnergy;

            DOVirtual.Int(oldValue, _displayedEnergy, _energyAnimationDuration, value =>
            {
                _energyLabel.text = $"{_displayedEnergy}";
            }).SetEase(Ease.OutQuad);
        }

        private void HandleGetFreeEnergyButtonClick()
        {
            if (!GameServices.EnergyService.TryGetFreeEnergy())
                return;
        }
    }
}