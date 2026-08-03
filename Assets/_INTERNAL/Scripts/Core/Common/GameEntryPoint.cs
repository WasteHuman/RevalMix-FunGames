using Core.Services;
using UnityEngine;

namespace Core.Common
{
    public abstract class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] protected GameController _controller;

        private void Start()
        {
            InvokeRepeating(nameof(UpdatePlayTime), 1f, 1f);
            _controller.Enter();
            _controller.Initialize();
        }

        private void UpdatePlayTime()
        {
            if (GameServices.SaveService.PlayerData != null)
                GameServices.SaveService.PlayerData.PlayTimeSeconds++;
        }

        private void OnDestroy()
        {
            _controller.Exit();
        }
    }
}