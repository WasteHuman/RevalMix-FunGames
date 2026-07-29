using UI.Screens;
using UnityEngine;

namespace Core.UI.Controllers
{
    public class MainSceneScreenController : MonoBehaviour
    {
        [SerializeField] private WelcomeScreenView _welcomeScreenView;
        [SerializeField] private MainMenuScreenView _mainMenuScreenView;

        private void Awake()
        {
            _welcomeScreenView.OnPlayerReady += HandlePlayerReady;
        }

        private void OnDestroy()
        {
            _welcomeScreenView.OnPlayerReady -= HandlePlayerReady;
        }

        private void HandlePlayerReady()
        {
            _welcomeScreenView.Close();
            _mainMenuScreenView.Open();
        }
    }
}