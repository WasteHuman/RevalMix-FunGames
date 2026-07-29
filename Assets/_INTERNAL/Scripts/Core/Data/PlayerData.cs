using System;
using System.Collections.Generic;

namespace Core.Data
{
    [System.Serializable]
    public class PlayerData
    {
        public string Name;
        public int AvatarId;

        public float Coins;
        public int Energy;
        public DateTime LastEnergyUpdate;

        public int Level;
        public float XP;
        public int TotalWins;
        public int TotalGames;
        public int PlayTimeSeconds;

        public List<string> CompletedQuests;
        public Dictionary<string, int> DailyQuestProgress;

        public PlayerData()
        {
            CompletedQuests = new();
            DailyQuestProgress = new Dictionary<string, int>();

            Coins = GameConstants.INITIAL_COINS;
            Energy = GameConstants.INITIAL_ENERGY;
            LastEnergyUpdate = DateTime.Now;

            Level = 1;
            XP = 0f;

            TotalWins = 0;
            TotalGames = 0;
            PlayTimeSeconds = 0;
        }

        public void AddXP(int amount)
        {
            XP += amount;
            int requiredXP = Level * 100;
            while (XP >= requiredXP)
            {
                Level++;
                XP -= requiredXP;
                requiredXP = Level * 100;
            }
        }

        public float GetWinRate()
        {
            if (TotalGames == 0) return 0f;
            return (float)TotalWins / TotalGames * 100f;
        }
    }
}