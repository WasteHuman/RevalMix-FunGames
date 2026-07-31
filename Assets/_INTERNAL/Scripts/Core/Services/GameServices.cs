using Core.Gameplay;
using Core.Services.LeaderboardSystem;
using Core.Services.Player;
using Core.Services.Quests;
using Core.Services.SaveSystem;

namespace Core.Services
{
    public static class GameServices
    {
        public static PlayerService PlayerService { get; private set; }
        public static EnergyService EnergyService { get; private set; }
        public static GameCompletionHandler GameCompletionHandler { get; private set; }
        public static SaveService SaveService { get; private set; }
        public static EconomyService EconomyService { get; private set; }
        public static DailyQuestsService Quests { get; private set; }
        public static LeaderboardService Leaderboard { get; private set; }

        public static void InitializeAll()
        {
            SaveService = new();
            SaveService.Init();

            EconomyService = new();
            EconomyService.Init(SaveService.PlayerData.Coins);

            EnergyService = new(() => SaveService.SavePlayerData());
            EnergyService.Init(SaveService.PlayerData);

            PlayerService = new();
            PlayerService.Init(SaveService.PlayerData);

            Quests = new DailyQuestsService();
            Quests.Init(SaveService.PlayerData);

            Leaderboard = new LeaderboardService();
            Leaderboard.Init(SaveService.PlayerData);

            GameCompletionHandler = new(EconomyService, PlayerService, Quests, () => SaveService.SavePlayerData());
        }

        public static void SaveAll()
        {
            SaveService?.SavePlayerData();
            SaveService?.SaveSettings();
        }
    }
}