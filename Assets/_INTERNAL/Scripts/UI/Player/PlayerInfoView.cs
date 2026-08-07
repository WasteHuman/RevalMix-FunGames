using Core.Services;
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

        private void Awake()
        {
            GameServices.PlayerService.OnXPChanged += HandleChangedXP;
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleCoinsBalanceChanged;
            GameServices.PlayerService.OnLevelChanged += HandleChangedLevel;
            GameServices.AvatarService.OnAvatarSetted += HandleSettetAvatar;
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

        private void OnDestroy()
        {
            GameServices.PlayerService.OnXPChanged -= HandleChangedXP;
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleCoinsBalanceChanged;
            GameServices.PlayerService.OnLevelChanged -= HandleChangedLevel;
            GameServices.AvatarService.OnAvatarSetted -= HandleSettetAvatar;
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
    }
}