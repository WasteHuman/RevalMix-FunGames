using Core.Services.LeaderboardSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardPlaceHolder : MonoBehaviour
    {
        [Header("Place View Prefab")]
        [SerializeField] private List<LeaderboardPlaceView> _top3Prefabs = new();
        [SerializeField] private LeaderboardPlaceView _placePrefab;
        [SerializeField] private RectTransform _top3Container;
        [SerializeField] private RectTransform _otherPlacesContainer;

        [Space(5), Header("Player Place View")]
        [SerializeField] private LeaderboardPlaceView _playerPlace;

        private readonly List<LeaderboardPlaceView> _top3Places = new();
        private readonly List<LeaderboardPlaceView> _otherPlaces = new();

        private LeaderboardService _service;

        public void Init(LeaderboardService leaderboardService)
        {
            _service = leaderboardService;

            InitPlayerPlace();
            InitTop3Places();
            InitOtherPlaces();
        }

        private void InitPlayerPlace()
        {
            var playerEntry = _service.Leaderboard.FirstOrDefault(player => player.IsCurrentPlayer);
            if(playerEntry == null)
            {
                Debug.LogError($"[Leaderboard Place Holder] Player entry not found!");
                return;
            }

            _playerPlace.Init(playerEntry);
        }

        private void InitTop3Places()
        {
            var top3Places = _service.GetTop(3);

            for (int i = 0; i < top3Places.Count; i++)
            {
                var entry = top3Places[i];
                var view = Instantiate(_top3Prefabs[i], _top3Container);
                view.Init(entry);
                _top3Places.Add(view);
            }
        }

        private void InitOtherPlaces()
        {
            _service.UpdatePlayerAvatar();
            var otherPlaces = _service.GetOtherPlayers();

            for (int i = 0; i < otherPlaces.Count; i++)
            {
                var entry = otherPlaces[i];
                var view = Instantiate(_placePrefab, _otherPlacesContainer);
                view.Init(entry);
                _otherPlaces.Add(view);
            }
        }
    }
}