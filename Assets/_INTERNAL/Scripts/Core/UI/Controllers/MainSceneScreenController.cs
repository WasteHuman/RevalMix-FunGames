using Core.Services;
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

        private void Start()
        {
            if (GameServices.SaveService.HasProfile())
                HandlePlayerReady();
        }

        private void OnDestroy()
        {
            _welcomeScreenView.OnPlayerReady -= HandlePlayerReady;
        }

        private void HandlePlayerReady()
        {
            Debug.Log($"[Main Scene Screen Controller] Player is ready.");
            GameServices.AvatarService.LoadSavedAvatar();

            _welcomeScreenView.Close();
            _mainMenuScreenView.Open();
        }
    }
}