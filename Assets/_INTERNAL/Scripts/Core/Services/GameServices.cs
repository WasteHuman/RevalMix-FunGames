using Core.Gameplay;
using Core.Services.LeaderboardSystem;
using Core.Services.Player;
using Core.Services.Quests;
using Core.Services.SaveSystem;
using Cysharp.Threading.Tasks;

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
        public static AvatarService AvatarService { get; private set; }

        public static async UniTask InitializeAll()
        {
            SaveService = new();
            await SaveService.Init();

            PlayerService = new();
            PlayerService.Init(SaveService.PlayerData);

            EconomyService = new();
            EconomyService.Init(PlayerService.GetData().Coins);

            EnergyService = new(() => SaveService.SavePlayerData().Forget());
            EnergyService.Init(PlayerService.GetData());

            Quests = new DailyQuestsService();
            Quests.Init(PlayerService.GetData());

            Leaderboard = new LeaderboardService();
            Leaderboard.Init(PlayerService.GetData());

            AvatarService = new(PlayerService.GetData());

            GameCompletionHandler = new(EconomyService, PlayerService, Quests, () => SaveService.SavePlayerData().Forget());
        }

        public static async UniTask SaveAll()
        {
            SaveService.PlayerData.Coins = EconomyService.GetCoinsBalance();
            await SaveService.SavePlayerData();
            SaveService.SaveSettings();
        }
    }
}