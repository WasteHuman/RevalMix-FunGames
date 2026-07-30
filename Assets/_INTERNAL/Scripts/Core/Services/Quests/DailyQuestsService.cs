using Core.Data;
using System.Collections.Generic;

namespace Core.Services.Quests
{
    public class DailyQuestsService
    {
        private PlayerData _data;
        private List<DailyQuest> _currentQuests;

        public List<DailyQuest> CurrentQuests => _currentQuests;

        public void Init(PlayerData data)
        {
            _data = data;
            CheckDailyReset();
            // В реальной реализации здесь была бы генерация квестов
            _currentQuests = new List<DailyQuest>();
        }

        private void CheckDailyReset()
        {
            // Логика сброса квестов по дате будет добавлена позже
        }

        public void ProgressQuest(string tag, int amount = 1)
        {
            // Логика прогресса
        }
    }
}