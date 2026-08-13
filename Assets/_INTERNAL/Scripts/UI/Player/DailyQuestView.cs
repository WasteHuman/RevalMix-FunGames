using Core.Data.Quests;
using Core.SO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    public class DailyQuestView : MonoBehaviour
    {
        [Header("View Setup")]
        [SerializeField] private Image _questImage;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private TextMeshProUGUI _rewardLabel;
        [SerializeField] private Image _progressBar;

        private DailyQuest _data;
        private QuestSpritesConfig _config;

        public DailyQuest Data => _data;

        public void Init(DailyQuest data, QuestSpritesConfig spritesConfig)
        {
            _data = data;
            _config = spritesConfig;

            if(_questImage != null)
            {
                if(_config.GetSprite(_data.Id) == null)
                {
                    Debug.LogWarning($"[Quest View] Quest data ID: {_data.Id}/Config ID: {_config.GetSprite(_data.Id)}." +
                        $" Sprite is null!");
                    return;
                }
                _questImage.sprite = _config.GetSprite(_data.Id);
            }

            if (_descriptionLabel != null)
                _descriptionLabel.text = data.Description;

            if(_rewardLabel != null)
                _rewardLabel.text = data.RewardCoins.ToString("N0");

            if (_progressBar != null)
                _progressBar.fillAmount = Mathf.Clamp01(_data.CurrentProgress / _data.TargetProgress);
        }

        public void UpdateQuestProgress(float progress)
        {
            _progressBar.DOKill();
            _progressBar.DOFillAmount(progress, 0.5f);
        }
    }
}