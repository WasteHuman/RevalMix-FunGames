namespace Core.Services.LeaderboardSystem
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string Name;
        public float WithdrawalAmount;
        public bool IsCurrentPlayer;
    }
}