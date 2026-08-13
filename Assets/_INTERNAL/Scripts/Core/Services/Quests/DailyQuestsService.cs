using Core.Data;
using Core.Data.Quests;
using Core.Gameplay;
using Core.Services.Player;
using Core.SO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Services.Quests
{
    public class DailyQuestsService
    {
        private PlayerData _data;
        private EconomyService _economyService;
        private PlayerService _playerService;
        private Dictionary<string, DailyQuest> _currentQuests;
        private DateTime _nextRefreshTimeUtc;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "quests_data.sav");

        public IReadOnlyDictionary<string, DailyQuest> CurrentQuests => _currentQuests;
        public event Action<Dictionary<string, DailyQuest>> OnQuestsUpdated;
        public event Action<DailyQuest> OnQuestUpdated;

        // Шаблоны квестов для генерации
        private static QuestTemplate[] QuestTemplates;

        public void Init(PlayerData data, QuestSpritesConfig config, EconomyService economyService, PlayerService playerService)
        {
            QuestTemplates = new QuestTemplate[]
            {
                new(config.PlayEveryArcadeSprite, "play_every_arcade", "Play Every Arcade", GameConstants.TAG_PLAY_EVERY_ARCADE, 1, 500, 200),
                new(config.SpinReelsSprite, "spin_10_reels", "Spin 10 Reels", GameConstants.TAG_SPIN_10_REELS, 10, 100, 30),
                new(config.CollectDiamondsSprite, "collect_5_diamonds", "Collect 5 Diamonds", GameConstants.TAG_COLLECT_5_DIAMONDS, 5, 100, 100),
                new(config.TriggerTurboModeSprite, "trigger_turbo_mode", "Trigger Turbo Mode", GameConstants.TAG_TRIGGER_TURBO_BOOST, 1, 150, 50),
                new(config.ReachMultiplierSprite, "reach_10x_multiplier", "Reach a x10 Multiplier", GameConstants.TAG_REACH_10X_MULTIPLIER, 1, 200, 50),
                new(config.ClaimFreeEnergySprite, "claim_free_energy", "Claim Free Energy", GameConstants.TAG_CLAIM_FREE_ENERGY, 1, 120, 35),
                new(config.OpenTheVaultSprite, "open_the_vault", "Open the Vault", GameConstants.TAG_OPEN_THE_VAULT, 1, 180, 45),
                new(config.Hit21Sprite, "hit_21", "Hit 21 Exactly", GameConstants.TAG_HIT_21, 1, 500, 1000),
                new(config.LaunchRocketsSprite, "launch_3_rockets", "Launch 3 Rockets", GameConstants.TAG_LAUNCH_3_ROCKETS, 3, 500, 150),
                new(config.DropPlinkoBallsSprite, "drop_10_plinko_balls", "Drop 10 Plinko Balls", GameConstants.TAG_DROP_10_PLINKO_BALLS, 10, 200, 500),
                new(config.SpinTheLuckyWheelSprite, "spin_lucky_wheel", "Spin the Lucky Wheel", GameConstants.TAG_SPIN_LUCKY_WHEEL, 1, 100, 25),
                new(config.RollDoubleDiceSprite, "roll_double_dice", "Roll Double Dice", GameConstants.TAG_ROLL_DOUBLE_DICE, 1, 100, 75),
                new(config.EarnRCoinsSprite, "earn_2500_coins", "Earn 2,500 R-Coins", GameConstants.TAG_EARN_2500_RCOINS, 2500, 150, 50),
                new(config.CompleteCombosSprite, "complete_5_combos", "Complete 5 Combos", GameConstants.TAG_COMPLETE_5_COMBOS, 5, 175, 500),
                new(config.UpgradeLevelSprite, "upgrade_level", "Upgrade Your Level", GameConstants.TAG_UPGRADE_YOUR_LEVEL, 1, 100, 25),
                new(config.WinGamesSprite, "win_3_games", "Win 3 Games", GameConstants.TAG_WIN_3_GAMES, 3, 250, 250),
            };

#if UNITY_EDITOR
            DeleteAllQuests();
#endif

            _data = data;
            _economyService = economyService;
            _playerService = playerService;

            _nextRefreshTimeUtc = DateTime.UtcNow.Date.AddDays(1);

            CheckDailyReset();

            // Если квесты ещё не сгенерированы, создаём новые
            if (_currentQuests == null || _currentQuests.Count == 0)
                GenerateNewQuests();
        }

        /// <summary>
        /// Проверить и выполнить сброс квестов если наступил новый день
        /// </summary>
        private void CheckDailyReset()
        {
            string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_DATE, "");

            if (todayUtc != lastDate)
            {
                Debug.Log($"[DailyQuests] New day detected! Resetting quests. {lastDate} -> {todayUtc}");
                PlayerPrefs.SetString(GameConstants.KEY_LAST_DAILY_DATE, todayUtc);
                GenerateNewQuests();
            }
            else
                LoadQuestsFromData();
        }

        /// <summary>
        /// Сгенерировать квесты
        /// </summary>
        private void GenerateNewQuests()
        {
            _currentQuests = new();

            var shuffledTemplates = Shuffle(QuestTemplates);

            for (int i = 0; i < shuffledTemplates.Length; i++)
            {
                var template = shuffledTemplates[i];
                var quest = new DailyQuest
                {
                    Id = $"quest_{DateTime.Now:yyyyMMdd}_{i}",
                    Description = template.Description,
                    QuestTag = template.Tag,
                    TargetProgress = template.TargetValue,
                    CurrentProgress = 0,
                    RewardCoins = template.RewardCoins,
                    RewardXP = template.RewardXP,
                    IsCompleted = false,
                    IsClaimed = false
                };

                _currentQuests[quest.Id] = quest;
            }

            SaveQuestsToData();
            Debug.Log($"[DailyQuests] Generated {_currentQuests.Count} new daily quests.");
        }

        public void ProgressQuests(IEnumerable<string> tags, int amount = 1)
        {
            if (tags == null) return;
            foreach (var tag in tags)
            {
                ProgressQuest(tag, amount);
            }
        }

        public TimeSpan GetTimeUntilRefresh()
        {
            var remaining = _nextRefreshTimeUtc - DateTime.UtcNow;

            // Если время вышло (наступила полночь)
            if (remaining <= TimeSpan.Zero)
            {
                string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_DATE, "");

                if (todayUtc != lastDate)
                {
                    CheckDailyReset(); // Генерируем новые квесты
                }

                // Пересчитываем время до следующей полночи
                _nextRefreshTimeUtc = DateTime.UtcNow.Date.AddDays(1);
                remaining = _nextRefreshTimeUtc - DateTime.UtcNow;
            }

            return remaining;
        }

        /// <summary>
        /// Обновить прогресс квеста по тегу
        /// </summary>
        public void ProgressQuest(string tag, int amount = 1)
        {
            if (_currentQuests == null) 
                return;

            bool changed = false;
            DailyQuest changedQuest;

            foreach (var quest in _currentQuests.Values)
            {
                if (quest.QuestTag == tag && !quest.IsCompleted && !quest.IsClaimed)
                {
                    quest.CurrentProgress += amount;
                    changedQuest = quest;

                    if (quest.CurrentProgress >= quest.TargetProgress)
                    {
                        quest.CurrentProgress = quest.TargetProgress;
                        quest.IsCompleted = true;

                        var reward = ClaimReward(quest.Id);
                        if (reward.HasValue)
                        {
                            _economyService.AddCoins(reward.Value.coins);
                            _playerService.AddXP(reward.Value.xp);
                        }

                        Debug.Log($"[DailyQuests] Quest completed: {quest.Description}");
                    }

                    changed = true;
                    OnQuestUpdated?.Invoke(changedQuest);
                }
            }

            if (changed)
                SaveQuestsToData();
        }

        /// <summary>
        /// Забрать награду за выполненный квест
        /// </summary>
        /// <returns>Награда (coins, XP) или null если нельзя забрать</returns>
        public (int coins, int xp)? ClaimReward(string questId)
        {
            if (_currentQuests == null || string.IsNullOrEmpty(questId))
                return null;

            var quest = _currentQuests[questId];

            if (!quest.IsCompleted || quest.IsClaimed)
            {
                Debug.LogWarning($"[DailyQuests] Cannot claim reward. Completed: {quest.IsCompleted}, Claimed: {quest.IsClaimed}");
                return null;
            }

            quest.IsClaimed = true;
            SaveQuestsToData();

            OnQuestUpdated?.Invoke(quest);
            OnQuestsUpdated?.Invoke(_currentQuests);

            Debug.Log($"[DailyQuests] Reward claimed: {quest.RewardCoins} coins, {quest.RewardXP} XP");
            return (quest.RewardCoins, quest.RewardXP);
        }

        /// <summary>
        /// Использовать только для ОТЛАДКИ!
        /// </summary>
        public void DeleteAllQuests()
        {
            if (File.Exists(SaveFilePath))
                File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Сохранить квесты в PlayerData
        /// </summary>
        private void SaveQuestsToData()
        {
            string json = JsonConvert.SerializeObject(_currentQuests);
            string tempPath = SaveFilePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            File.Move(tempPath, SaveFilePath); ;
        }

        /// <summary>
        /// Загрузить квесты из Сохранения
        /// </summary>
        private void LoadQuestsFromData()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _currentQuests = JsonConvert.DeserializeObject<Dictionary<string, DailyQuest>>(json);
                    Debug.Log($"[DailyQuests] Loaded {_currentQuests?.Count ?? 0} quests from save.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DailyQuests] Failed to load quests: {e.Message}. Generating new ones.");
                    GenerateNewQuests();
                }
            }
            else
                GenerateNewQuests();
        }

        /// <summary>
        /// Перемешать массив (Fisher-Yates shuffle)
        /// </summary>
        private T[] Shuffle<T>(T[] array)
        {
            T[] shuffled = (T[])array.Clone();
            var random = new System.Random();

            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            return shuffled;
        }
    }
}