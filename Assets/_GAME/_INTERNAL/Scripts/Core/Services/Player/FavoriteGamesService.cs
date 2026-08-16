using Core.Data;
using System.Collections.Generic;

namespace Core.Services.Player
{
    public class FavoriteGamesService
    {
        private readonly PlayerData _playerData;

        private readonly List<FavoriteGameData> _topFavoriteGames = new();

        public FavoriteGamesService(PlayerData data) => _playerData = data;

        public List<FavoriteGameData> RequestTopFavoriteGames(int count)
        {
            if (_playerData.FavoriteGames.Count == 0 && _playerData.FavoriteGames.Count < count)
                return null;

            _topFavoriteGames.Clear();
            _topFavoriteGames.AddRange(_playerData.FavoriteGames.GetRange(0, count));
            _topFavoriteGames.Sort((a, b) => b.TotalPlayed.CompareTo(a));

            return _topFavoriteGames;
        }
    }
}