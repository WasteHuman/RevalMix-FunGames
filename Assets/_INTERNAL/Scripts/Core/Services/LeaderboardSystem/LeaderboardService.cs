using Core.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services.LeaderboardSystem
{
    public class LeaderboardService
    {
        private PlayerData _playerData;
        private List<LeaderboardEntry> _leaderboard;

        public List<LeaderboardEntry> Leaderboard => _leaderboard;

        public void Init(PlayerData data)
        {
            _playerData = data;
            GenerateMockLeaderboard();
        }

        /// <summary>
        /// Сгенерировать мок-таблицу лидеров с позицией игрока
        /// </summary>
        private void GenerateMockLeaderboard()
        {
            _leaderboard = new List<LeaderboardEntry>();

            // Генерируем 20 фейковых игроков с разным уровнем и XP
            var random = new System.Random();
            string[] names = { "Alex", "Maria", "John", "Emma", "David", "Sophie", "Michael", "Olivia",
                              "James", "Isabella", "Robert", "Mia", "William", "Charlotte", "Daniel",
                              "Amelia", "Matthew", "Harper", "Andrew", "Evelyn" };

            for (int i = 0; i < names.Length; i++)
            {
                int level = random.Next(5, 30);
                int xp = random.Next(level * 100, (level + 1) * 100);
                float withdrawalAmount = random.Next(50, 5000);

                _leaderboard.Add(new LeaderboardEntry
                {
                    Rank = i + 1,
                    Name = names[i],
                    WithdrawalAmount = withdrawalAmount,
                    IsCurrentPlayer = false
                });
            }

            // Добавляем текущего игрока
            _leaderboard.Add(new LeaderboardEntry
            {
                Rank = 0, // Будет пересчитан после сортировки
                Name = _playerData.Name != "" ? _playerData.Name : "Player",
                WithdrawalAmount = _playerData.WithdrawalAmount,
                IsCurrentPlayer = true
            });

            // Сортируем по XP (убывание)
            _leaderboard.Sort((a, b) => b.WithdrawalAmount.CompareTo(a.WithdrawalAmount));

            // Пересчитываем ранги
            for (int i = 0; i < _leaderboard.Count; i++)
                _leaderboard[i].Rank = i + 1;

            UnityEngine.Debug.Log($"[Leaderboard] Generated {_leaderboard.Count} entries. Player position: {GetPlayerPosition()}");
        }

        /// <summary>
        /// Получить позицию текущего игрока
        /// </summary>
        public int GetPlayerPosition()
        {
            for (int i = 0; i < _leaderboard.Count; i++)
            {
                if (_leaderboard[i].IsCurrentPlayer)
                    return i + 1;
            }
            return _leaderboard.Count;
        }

        /// <summary>
        /// Обновить таблицу после изменения XP игрока
        /// </summary>
        public void RefreshLeaderboard()
        {
            // Находим запись игрока и обновляем её
            for (int i = 0; i < _leaderboard.Count; i++)
            {
                if (_leaderboard[i].IsCurrentPlayer)
                    _leaderboard[i].WithdrawalAmount = _playerData.TotalWins;
            }

            // Пересортировываем
            _leaderboard.Sort((a, b) => b.WithdrawalAmount.CompareTo(a.WithdrawalAmount));

            // Пересчитываем ранги
            for (int i = 0; i < _leaderboard.Count; i++)
                _leaderboard[i].Rank = i + 1;

            UnityEngine.Debug.Log($"[Leaderboard] Refreshed. Player position: {GetPlayerPosition()}");
        }

        /// <summary>
        /// Получить топ-N игроков
        /// </summary>
        public List<LeaderboardEntry> GetTop(int count)
        {
            return _leaderboard.GetRange(0, Mathf.Min(count, _leaderboard.Count));
        }
    }
}