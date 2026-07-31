using Core.Gameplay;
using Core.Services.Player;
using Core.Services.Quests;
using Core.Services.SaveSystem;

namespace Core.Services
{
    public static class GameServices
    {
        public static PlayerService PlayerService { get; private set; }
        public static SaveService SaveService { get; private set; }
        public static EconomyService EconomyService { get; private set; }
        public static DailyQuestsService Quests { get; private set; }
        public static LeaderboardService Leaderboard { get; private set; }

        public static void InitializeAll()
        {
            SaveService = new SaveService();
            SaveService.Init();

            EconomyService = new();
            EconomyService.Init(SaveService.PlayerData.Coins);

            PlayerService = new();
            PlayerService.Init(SaveService.PlayerData);

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