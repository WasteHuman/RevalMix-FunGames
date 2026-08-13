using Core.Data.Quests;
using Core.Services.Quests;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestViewsHolder : MonoBehaviour
    {
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
            List<DailyQuest> quests = _dailyQuestsService.CurrentQuests.Values.ToList();

            for(int i = 0; i < quests.Count; i++)
            {
                var questData = quests[i];
                var view = _views[i];
                view.Init(questData);
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