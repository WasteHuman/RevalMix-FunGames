using Core.Services;
using UnityEngine;

namespace Core.Common
{
    public abstract class GameEntryPoint : MonoBehaviour
    {
        private void Start() => InvokeRepeating(nameof(UpdatePlayTime), 1f, 1f);

        private void UpdatePlayTime()
        {
            if (GameServices.SaveService.PlayerData != null)
                GameServices.SaveService.PlayerData.PlayTimeSeconds++;
        }
    }
}