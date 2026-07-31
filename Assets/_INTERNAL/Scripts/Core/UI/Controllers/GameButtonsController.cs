using UI.Other;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.UI.Controllers
{
    public class GameButtonsController : MonoBehaviour
    {
        [SerializeField] private ActionButton _reelsButton;
        [SerializeField] private ActionButton _vaultButton;
        [SerializeField] private ActionButton _neonWheelButton;

        private void Awake()
        {
            _reelsButton.OnButtonClick += HandleReelsButtonClick;
            _vaultButton.OnButtonClick += HandleVaultButtonClick;
            _neonWheelButton.OnButtonClick += HandleNeonWheelButtonClick;
        }

        private void OnDestroy()
        {
            _reelsButton.OnButtonClick -= HandleReelsButtonClick;
            _vaultButton.OnButtonClick -= HandleVaultButtonClick;
            _neonWheelButton.OnButtonClick -= HandleNeonWheelButtonClick;
        }

        private void HandleReelsButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_REELS);
        private void HandleNeonWheelButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_NEON_WHEEL);
        private void HandleVaultButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_VAULT);
    }
}