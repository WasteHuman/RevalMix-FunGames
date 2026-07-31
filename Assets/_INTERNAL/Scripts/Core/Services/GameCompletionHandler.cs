using Core.Data;
using Core.Gameplay;
using Core.Services.Player;
using Core.Services.Quests;
using System;
using UnityEngine;

namespace Core.Services
{
    public class GameCompletionHandler
    {
        private readonly EconomyService _economy;
        private readonly PlayerService _player;
        private readonly DailyQuestsService _quests;
        private readonly Action _onGameDataChanged;

        public GameCompletionHandler(
            EconomyService economy,
            PlayerService player,
            DailyQuestsService quests,
            Action onGameDataChanged)
        {
            _economy = economy;
            _player = player;
            _quests = quests;
            _onGameDataChanged = onGameDataChanged;
        }

        /// <summary>
        /// Обработать результат завершённой мини-игры
        /// </summary>
        /// <param name="result">Результат игры</param>
        public void HandleGameResult(GameResult result)
        {
            Debug.Log($"[GameCompletion] Handling result: Win={result.IsWin}, Coins={result.RewardCoins}, XP={result.RewardXP}, Tag={result.QuestTag}");

            // 1. Обновляем общую статистику игр
            _player.RecordGameResult(result.IsWin);

            // 2. Начисляем награды (монеты + XP)
            if (result.RewardCoins > 0)
                _economy.AddCoins(result.RewardCoins);

            if (result.RewardXP > 0)
                _player.AddXP(result.RewardXP);

            // 3. Обновляем прогресс квеста по тегу
            if (!string.IsNullOrEmpty(result.QuestTag))
                _quests.ProgressQuest(result.QuestTag, 1);

            // 4. Триггерим сохранение
            _onGameDataChanged?.Invoke();

            Debug.Log($"[GameCompletion] Result handled. Total games: {_player.PlayerTotalGames}, WithdrawalAmount: {_player.PlayerTotalWins}");
        }
    }
}