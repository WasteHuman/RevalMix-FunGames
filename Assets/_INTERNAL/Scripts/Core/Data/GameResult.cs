namespace Core.Data
{
    public readonly struct GameResult
    {
        public readonly bool IsWin;
        public readonly float RewardCoins;
        public readonly float RewardXP;
        public readonly string QuestTag;
        public readonly string GameId;

        public GameResult(bool isWin, float rewardCoins, float rewardXP, string questTag, string gameId)
        {
            IsWin = isWin;
            RewardCoins = rewardCoins;
            RewardXP = rewardXP;
            QuestTag = questTag;
            GameId = gameId;
        }
    }
}