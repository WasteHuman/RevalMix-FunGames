using Core.Data;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Core.Services.SaveSystem
{
    public class SaveService
    {
        private const string KEY_PLAYER = GameConstants.KEY_PLAYER_DATA;
        private const string KEY_SETTINGS = GameConstants.KEY_SETTINGS;

        private PlayerData _playerData;
        private SettingsData _settingsData;

        public PlayerData PlayerData => _playerData;
        public SettingsData Settings => _settingsData;

        public async UniTask Init()
        {
            await LoadPlayerData();
            LoadSettings();
        }

        private async UniTask LoadPlayerData()
        {
            if (PlayerPrefs.HasKey(KEY_PLAYER))
            {
                try
                {
                    string json = PlayerPrefs.GetString(KEY_PLAYER);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json) ?? throw new Exception("Null data");
                    Debug.Log("[SaveService] Player data loaded.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveService] Corrupted save data, creating new. Error: {e.Message}");
                    await CreateNewPlayerData();
                }
            }
            else
            {
                await CreateNewPlayerData();
            }
        }

        private async UniTask CreateNewPlayerData()
        {
            _playerData = new PlayerData();
            await SavePlayerData();
            Debug.Log("[SaveService] New player data created.");
        }

        public async UniTask SavePlayerData()
        {
            if (_playerData == null) 
                return;

            try
            {
                string json = JsonConvert.SerializeObject(_playerData);
                PlayerPrefs.SetString(KEY_PLAYER, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveService] Player data saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to save player  {e.Message}");
            }
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey(KEY_SETTINGS))
            {
                try
                {
                    string json = PlayerPrefs.GetString(KEY_SETTINGS);
                    _settingsData = JsonConvert.DeserializeObject<SettingsData>(json) ?? throw new Exception("Null settings");
                }
                catch
                {
                    _settingsData = new SettingsData();
                }
            }
            else
            {
                _settingsData = new SettingsData();
            }
        }

        public void SaveSettings()
        {
            if (_settingsData == null) 
                return;

            string json = JsonConvert.SerializeObject(_settingsData);
            PlayerPrefs.SetString(KEY_SETTINGS, json);
            PlayerPrefs.Save();
        }

        public bool HasProfile()
        {
            return PlayerPrefs.HasKey(GameConstants.KEY_HAS_PROFILE);
        }

        public void SetProfileCreated(bool value)
        {
            PlayerPrefs.SetInt(GameConstants.KEY_HAS_PROFILE, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}