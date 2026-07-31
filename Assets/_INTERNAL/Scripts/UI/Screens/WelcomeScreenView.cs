using TMPro;
using Screen = UI.Other.Screen;
using UnityEngine;
using UI.Other;
using System;
using Core.Services;

namespace UI.Screens
{
    public class WelcomeScreenView : Screen
    {
        [Header("Text Setup")]
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private TextMeshProUGUI _nameLabel;

        [Space(5), Header("Action Buttons Setup")]
        [SerializeField] private ActionButton _saveButton;
        [SerializeField] private ActionButton _choosePlayerPhotoButton;

        private bool _isNameFieldEmpty = true;

        public event Action OnPlayerReady;

        private void Awake()
        {
            _saveButton.OnButtonClick += HandleSaveButtonClick;
            _saveButton.OnButtonClick += HandleChoosePhotoButtonClick;

            _nameInputField.onEndEdit.AddListener(HandleNameInput);
        }

        private void OnDestroy()
        {
            _saveButton.OnButtonClick -= HandleSaveButtonClick;
            _saveButton.OnButtonClick -= HandleChoosePhotoButtonClick;

            _nameInputField.onEndEdit.RemoveListener(HandleNameInput);
        }

        private void HandleSaveButtonClick()
        {
            if (_isNameFieldEmpty)
            {
                Debug.LogWarning($"[Welcome Screen] Name is null!");
                return;
            }

            GameServices.SaveService.SetProfileCreated(true);
            OnPlayerReady?.Invoke();
        }

        private void HandleChoosePhotoButtonClick()
        {
            // TODO: Выбор фото на аватарку
        }

        private void HandleNameInput(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return;

            _isNameFieldEmpty = false;
            _nameLabel.text = raw;
            GameServices.SaveService.PlayerData.Name = raw;
        }
    }
}