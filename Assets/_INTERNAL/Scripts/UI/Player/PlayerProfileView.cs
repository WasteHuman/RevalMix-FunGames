using Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    public class PlayerProfileView : MonoBehaviour
    {
        [Header("Lables Setup")]
        [SerializeField] private TextMeshProUGUI _winRateLabel;
        [SerializeField] private TextMeshProUGUI _rankLabel;
        [SerializeField] private TextMeshProUGUI _playedHoursLabel;
        [SerializeField] private TextMeshProUGUI _playedGamesLabel;

        [Space(5), Header("Favorite Arcades Setup")]
        [SerializeField] private Image _firstPlace;
        [SerializeField] private Image _secondPlace;
        [SerializeField] private Image _thirdPlace;

        private void Awake()
        {
            _winRateLabel.text = $"{GameServices.PlayerService.GetWinRate()}%";
            _rankLabel.text = $"{GameServices.PlayerService.PlayerRank}";
            _playedHoursLabel.text = $"{GameServices.PlayerService.PlayerPlayedSeconds / 60 / 60}H";
            _playedGamesLabel.text = $"{GameServices.PlayerService.PlayerTotalGames}";
        }
    }
}