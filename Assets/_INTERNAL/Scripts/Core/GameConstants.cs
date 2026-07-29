namespace Core
{
    public static class GameConstants
    {
        #region Main Scene Names
        public const string MAIN_MENU = "Main_Menu";
        public const string LEADERBOARD = "Leaderboard";
        public const string QUESTS = "Quests";
        public const string PROFILE = "Profile";
        public const string SETTINGS = "Settings";
        public const string WHEEL_OF_LUCK = "WheelOfLuck";
        #endregion

        #region Game Scene Names
        public const string GAME_REELS = "Game_Reels";
        public const string GAME_VAULT = "Game_Vault";
        public const string GAME_NEON_WHEEL = "Game_Neon_Wheel";
        public const string GAME_CYBER_MASTER = "Game_Cyber_Master";
        public const string GAME_CRYPTO_VIBE = "Game_Crypto_Vibe";
        public const string GAME_DIAMOND_RETRO = "Game_Diamond_Retro";
        public const string GAME_WHEEL_OF_REVOLUT = "Game_Wheel_Of_Revolut";
        public const string GAME_PLINKO_VIBE = "Game_Plinko_Vibe";
        public const string GAME_INFINITE_SCORE = "Game_Infinite_Score";
        public const string GAME_ELECTRIC_DICE = "Game_Electric_Dice";
        #endregion

        #region Player Prefs
        public const string KEY_HAS_PROFILE = "Has_Profile";
        public const string KEY_PLAYER_DATA = "Player_Data_JSON";
        public const string KEY_SETTINGS = "Settings_JSON";
        public const string KEY_LAST_DAILY_DATE = "Last_Daily_Date";
        #endregion

        #region Economy & Limits
        public const float INITIAL_COINS = 1000f;
        public const int INITIAL_ENERGY = 20;
        public const int MAX_ENERGY = 20;
        public const float ENERGY_REGEN_MINUTES = 60f;
        #endregion

        #region Quest Tags
        public const string TAG_ANY_WIN = "WIN_ANY";
        public const string TAG_PLAY_SLOTS = "PLAY_SLOTS";
        public const string TAG_PLAY_WHEEL = "PLAY_WHEEL";
        #endregion
    }
}