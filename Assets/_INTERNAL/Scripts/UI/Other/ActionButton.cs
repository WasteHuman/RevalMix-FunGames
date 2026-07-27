using System;
using UI.Animations.GameScreen;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Other
{
    [RequireComponent(typeof(ButtonAnimations))]
    [RequireComponent(typeof(Button))]
    public class ActionButton : MonoBehaviour
    {
        private ButtonAnimations _animations;
        private Button _button;

        public bool Interactable
        {
            get
            {
                return _button.interactable;
            }
            set
            {
                _button.interactable = value;
            }
        }
        public ButtonAnimations Animations => _animations;

        public event Action OnButtonClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _animations = GetComponent<ButtonAnimations>();
        }

        private void Start() => _button.onClick.AddListener(HandleButtonClick);

        private void OnDestroy() => _button.onClick.RemoveListener(HandleButtonClick);

        private void HandleButtonClick()
        {
            Interactable = false;

            _animations.ButtonClickAnimation(() =>
            {
                Interactable = true;
                OnButtonClick?.Invoke();
            });
        }
    }
}