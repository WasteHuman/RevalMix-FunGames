using Core.Data.Quests;
using Core.Services.Quests;
using Core.SO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestViewsHolder : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private QuestSpritesConfig _config;

        [Space(5), Header("Views")]
        [SerializeField] private List<DailyQuestView> _views = new();

        private DailyQuestsService _dailyQuestsService;

        public void Init(DailyQuestsService dailyQuestsService)
        {
            _dailyQuestsService = dailyQuestsService;
            _dailyQuestsService.OnQuestUpdated += HandleUpdatedQuest;
        }

        public void Dispose()
        {
            _dailyQuestsService.OnQuestUpdated -= HandleUpdatedQuest;
        }

        public void SetupQuestViews()
        {
            int count = Mathf.Min(_dailyQuestsService.CurrentQuests.Count, _views.Count);

            for (int i = 0; i < count; i++)
            {
                var questData = _dailyQuestsService.CurrentQuests[i];
                var view = _views[i];
                view.Init(questData, _config);
            }
        }

        private void HandleUpdatedQuest(DailyQuest changedQuest)
        {
            var quest = _views.FirstOrDefault(v => v.Data == changedQuest);

            if(quest != null)
            {
                float progress = Mathf.Clamp01(changedQuest.CurrentProgress / changedQuest.TargetProgress);
                quest.UpdateQuestProgress(progress);
            }
        }
    }
}