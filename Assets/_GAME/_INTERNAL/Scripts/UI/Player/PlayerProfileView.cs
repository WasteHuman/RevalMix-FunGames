using Core.Services;
using Core.SO;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    public class PlayerProfileView : MonoBehaviour
    {
        [Header("Labels Setup")]
        [SerializeField] private TextMeshProUGUI _winRateLabel;
        [SerializeField] private TextMeshProUGUI _rankLabel;
        [SerializeField] private TextMeshProUGUI _playedHoursLabel;
        [SerializeField] private TextMeshProUGUI _playedGamesLabel;

        [Space(5), Header("Favorite Arcades Setup")]
        [SerializeField] private List<Image> _places = new();
        [SerializeField] private FavoriteGamesSpritesConfig _favoriteGamesSpritesConfig;

        [Space(5), Header("Change Player Info Buttons")]
        [SerializeField] private ActionButton _changePhotoButton;
        [SerializeField] private ActionButton _changeNameButton;
        [SerializeField] private PlayerInfoView _playerInfoView;

        [Space(5), Header("Change Player Name Setup")]
        [SerializeField] private TMP_InputField _inputField;

        private void Awake()
        {
            _changeNameButton.OnButtonClick += HandleChangeNameButtonClick;
            _changePhotoButton.OnButtonClick += HandleChangePhotoButtonClick;
            _inputField.onEndEdit.AddListener(HandleEndNameInput);
        }

        private void Start()
        {
            _winRateLabel.text = $"{GameServices.PlayerService.GetWinRate():N0}%";
            _rankLabel.text = $"{GameServices.PlayerService.PlayerRank}";
            _playedHoursLabel.text = $"{GameServices.PlayerService.PlayerPlayedSeconds / 60 / 60:N0}H";
            _playedGamesLabel.text = $"{GameServices.PlayerService.PlayerTotalGames:N0}";

            SetupFavoriteGames();
        }

        private void OnDestroy()
        {
            _changeNameButton.OnButtonClick -= HandleChangeNameButtonClick;
            _changePhotoButton.OnButtonClick -= HandleChangePhotoButtonClick;
            _inputField.onEndEdit.RemoveListener(HandleEndNameInput);
        }

        private void SetupFavoriteGames()
        {
            var favoriteGames = GameServices.FavoriteGamesService.RequestTopFavoriteGames(3);
            int count = Mathf.Min(favoriteGames.Count, _places.Count);

            for (int i = 0; i < count; i++)
            {
                var gameEntry = favoriteGames[i];
                _places[i].sprite = _favoriteGamesSpritesConfig.GetSprite(gameEntry.GameId);
            }
        }

        private void HandleEndNameInput(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                Debug.LogWarning("Name is empty");
                return;
            }

            GameServices.PlayerService.SetName(raw);
            _inputField.gameObject.SetActive(false);
            _playerInfoView.ToggleNameLabel(true);
        }

        private void HandleChangePhotoButtonClick() => GameServices.AvatarService.RequestPermission();

        private void HandleChangeNameButtonClick()
        {
            _playerInfoView.ToggleNameLabel(false);
            _inputField.gameObject.SetActive(true);
            _inputField.Select();
            _inputField.ActivateInputField();
        }
    }
}