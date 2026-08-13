using Core.Data;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace Core.Services.SaveSystem
{
    public class SaveService
    {
        private const string KEY_SETTINGS = GameConstants.KEY_SETTINGS;

        private PlayerData _playerData;
        private SettingsData _settingsData;
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "player_data.sav");

        public PlayerData PlayerData => _playerData;
        public SettingsData Settings => _settingsData;

        public void Init()
        {
            LoadPlayerData();
            LoadSettings();
        }

        private void LoadPlayerData()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json);
                }
                catch
                {
                    CreateNewPlayerData();
                }
            }
            else
                CreateNewPlayerData();
        }

        private void CreateNewPlayerData()
        {
            _playerData = new PlayerData();
            SavePlayerData();
            Debug.Log("[SaveService] New player data created.");
        }

        public void SavePlayerData()
        {
            if (_playerData == null) 
                return;

            try
            {
                _playerData.CurrentAvatar = null;
                string json = JsonConvert.SerializeObject(_playerData);

                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(SaveFilePath))
                    File.Delete(SaveFilePath);

                File.Move(tempPath, SaveFilePath);

                Debug.Log("[SaveService] File saved successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] File save failed: {e.Message}");
            }
        }

        public void DeleteAllSaves()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
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