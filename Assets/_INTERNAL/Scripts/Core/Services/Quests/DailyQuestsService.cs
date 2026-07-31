using Core.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services.Quests
{
    public class DailyQuestsService
    {
        private PlayerData _data;
        private List<DailyQuest> _currentQuests;
        private DateTime _lastGeneratedDate;

        public IReadOnlyList<DailyQuest> CurrentQuests => _currentQuests.AsReadOnly();
        public event Action<List<DailyQuest>> OnQuestsUpdated;

        // Шаблоны квестов для генерации
        private static readonly QuestTemplate[] QuestTemplates = new QuestTemplate[]
        {
            new("play_every_arcade", "Play Every Arcade", GameConstants.TAG_PLAY_EVERY_ARCADE, 1, 500, 200),
            new("spin_10_reels", "Spin 10 Reels", GameConstants.TAG_SPIN_10_REELS, 10, 100, 30),
            new("collect_5_diamonds", "Collect 5 Diamonds", GameConstants.TAG_COLLECT_5_DIAMONDS, 5, 100, 100),
            new("trigger_turbo_mode", "Trigger Turbo Mode", GameConstants.TAG_TRIGGER_TURBO_BOOST, 1, 150, 50),
            new("reach_10x_multiplier", "Reach a x10 Multiplier", GameConstants.TAG_REACH_10X_MULTIPLIER, 1, 200, 50),
            new("claim_free_energy", "Claim Free Energy", GameConstants.TAG_CLAIM_FREE_ENERGY, 1, 120, 35),
            new("open_the_vault", "Open the Vault", GameConstants.TAG_OPEN_THE_VAULT, 1, 180, 45),
            new("hit_21", "Hit 21 Exactly", GameConstants.TAG_HIT_21, 1, 500, 1000),
            new("launch_3_rockets", "Launch 3 Rockets", GameConstants.TAG_LAUNCH_3_ROCKETS, 3, 500, 150),
            new("drop_10_plinko_balls", "Drop 10 Plinko Balls", GameConstants.TAG_DROP_10_PLINKO_BALLS, 10, 200, 500),
            new("spin_lucky_wheel", "Spin the Lucky Wheel", GameConstants.TAG_SPIN_LUCKY_WHEEL, 1, 100, 25),
            new("roll_double_dice", "Roll Double Dice", GameConstants.TAG_ROLL_DOUBLE_DICE, 1, 100, 75),
            new("earn_2500_coins", "Earn 2,500 R-Coins", GameConstants.TAG_EARN_2500_RCOINS, 2500, 150, 50),
            new("complete_5_combos", "Complete 5 Combos", GameConstants.TAG_COMPLETE_5_COMBOS, 5, 175, 500),
            new("upgrade_level", "Upgrade Your Level", GameConstants.TAG_UPGRADE_YOUR_LEVEL, 1, 100, 25),
            new("win_3_games", "Win 3 Games", GameConstants.TAG_WIN_3_GAMES, 3, 250, 250),
        };

        public void Init(PlayerData data)
        {
            _data = data;
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
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_DATE, "");

            if (today != lastDate)
            {
                Debug.Log($"[DailyQuests] New day detected! Resetting quests. {lastDate} -> {today}");
                PlayerPrefs.SetString(GameConstants.KEY_LAST_DAILY_DATE, today);
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
            _currentQuests = new List<DailyQuest>();
            var random = new System.Random();

            var shuffledTemplates = Shuffle(QuestTemplates);

            for (int i = 0; i < shuffledTemplates.Length; i++)
            {
                var template = shuffledTemplates[i];
                var quest = new DailyQuest
                {
                    Id = $"quest_{DateTime.Now:yyyyMMdd}_{i}",
                    Description = template.Description,
                    QuestTag = template.Tag,
                    TargetValue = template.TargetValue,
                    CurrentProgress = 0,
                    RewardCoins = template.RewardCoins,
                    RewardXP = template.RewardXP,
                    IsCompleted = false,
                    IsClaimed = false
                };

                _currentQuests.Add(quest);
            }

            SaveQuestsToData();
            _lastGeneratedDate = DateTime.Now;
            OnQuestsUpdated?.Invoke(_currentQuests);

            Debug.Log($"[DailyQuests] Generated {_currentQuests.Count} new daily quests.");
        }

        /// <summary>
        /// Обновить прогресс квеста по тегу
        /// </summary>
        public void ProgressQuest(string tag, int amount = 1)
        {
            if (_currentQuests == null) return;

            bool changed = false;

            foreach (var quest in _currentQuests)
            {
                if (quest.QuestTag == tag && !quest.IsCompleted && !quest.IsClaimed)
                {
                    quest.CurrentProgress += amount;

                    if (quest.CurrentProgress >= quest.TargetValue)
                    {
                        quest.CurrentProgress = quest.TargetValue;
                        quest.IsCompleted = true;
                        Debug.Log($"[DailyQuests] Quest completed: {quest.Description}");
                    }

                    changed = true;
                }
            }

            if (changed)
            {
                SaveQuestsToData();
                OnQuestsUpdated?.Invoke(_currentQuests);
            }
        }

        /// <summary>
        /// Забрать награду за выполненный квест
        /// </summary>
        /// <returns>Награда (coins, XP) или null если нельзя забрать</returns>
        public (int coins, int xp)? ClaimReward(int questIndex)
        {
            if (_currentQuests == null || questIndex < 0 || questIndex >= _currentQuests.Count)
                return null;

            var quest = _currentQuests[questIndex];

            if (!quest.IsCompleted || quest.IsClaimed)
            {
                Debug.LogWarning($"[DailyQuests] Cannot claim reward. Completed: {quest.IsCompleted}, Claimed: {quest.IsClaimed}");
                return null;
            }

            quest.IsClaimed = true;
            SaveQuestsToData();
            OnQuestsUpdated?.Invoke(_currentQuests);

            Debug.Log($"[DailyQuests] Reward claimed: {quest.RewardCoins} coins, {quest.RewardXP} XP");
            return (quest.RewardCoins, quest.RewardXP);
        }

        /// <summary>
        /// Сохранить квесты в PlayerData
        /// </summary>
        private void SaveQuestsToData()
        {
            // Сериализуем квесты в JSON и сохраняем через PlayerPrefs
            // Для простоты используем отдельный ключ
            string json = JsonConvert.SerializeObject(_currentQuests);
            PlayerPrefs.SetString("Daily_Quests_JSON", json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загрузить квесты из PlayerData
        /// </summary>
        private void LoadQuestsFromData()
        {
            if (PlayerPrefs.HasKey("Daily_Quests_JSON"))
            {
                try
                {
                    string json = PlayerPrefs.GetString("Daily_Quests_JSON");
                    _currentQuests = JsonConvert.DeserializeObject<List<DailyQuest>>(json);
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