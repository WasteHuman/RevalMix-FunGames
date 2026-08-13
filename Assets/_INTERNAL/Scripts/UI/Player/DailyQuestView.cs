using Core.Data.Quests;
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

        public DailyQuest Data => _data;

        public void Init(DailyQuest data)
        {
            _data = data;

            if(_questImage != null)
                _questImage.sprite = data.Sprite;

            if(_descriptionLabel != null)
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