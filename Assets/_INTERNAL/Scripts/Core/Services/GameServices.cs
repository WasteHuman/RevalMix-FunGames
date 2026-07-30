using Core.Services.Quests;
using Core.Services.SaveSystem;

namespace Core.Services
{
    public static class GameServices
    {
        public static SaveService SaveService { get; private set; }
        public static DailyQuestsService Quests { get; private set; }
        public static LeaderboardService Leaderboard { get; private set; }

        public static void InitializeAll()
        {
            SaveService = new SaveService();
            SaveService.Init();

            Quests = new DailyQuestsService();
            Quests.Init(SaveService.PlayerData);

            Leaderboard = new LeaderboardService();
            Leaderboard.Init(SaveService.PlayerData);
        }

        public static void SaveAll()
        {
            SaveService?.SavePlayerData();
            SaveService?.SaveSettings();
        }
    }
}