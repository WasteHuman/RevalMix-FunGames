using System;
using UI.Animations.GameScreen;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Other
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(RectTransform))]
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private ButtonAnimations _animations;
        [SerializeField] private RectTransform _rectTransform;

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
            _rectTransform = GetComponent<RectTransform>();

            _animations.Init(_rectTransform);
        }

        private void Start() => _button.onClick.AddListener(HandleButtonClick);

        private void OnDestroy()
        {
            _animations.StopAnimations();
            _button.onClick.RemoveListener(HandleButtonClick);
        }

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