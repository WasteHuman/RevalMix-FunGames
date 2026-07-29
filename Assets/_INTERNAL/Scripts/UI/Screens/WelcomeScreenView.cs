using TMPro;
using Screen = UI.Other.Screen;
using UnityEngine;
using UI.Other;
using System;

namespace UI.Screens
{
    public class WelcomeScreenView : Screen
    {
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private ActionButton _saveButton;

        public event Action OnPlayerReady;

        private void Awake()
        {
            _saveButton.OnButtonClick += HandleSaveButtonClick;

            _nameInputField.onEndEdit.AddListener(HandleNameInput);
        }

        private void OnDestroy()
        {
            _saveButton.OnButtonClick -= HandleSaveButtonClick;

            _nameInputField.onEndEdit.RemoveListener(HandleNameInput);
        }

        private void HandleSaveButtonClick() => OnPlayerReady?.Invoke();

        private void HandleNameInput(string raw)
        {

        }
    }
}