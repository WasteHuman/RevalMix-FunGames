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

        public List<string> CompletedQuests;

        public PlayerData()
        {
            CompletedQuests = new();
            Coins = GameConstants.INITIAL_COINS;
            Energy = GameConstants.INITIAL_ENERGY;
            LastEnergyUpdate = DateTime.Now;
        }
    }
}