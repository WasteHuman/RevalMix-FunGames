using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Other
{
    public class HoldButton : MonoBehaviour
    {
        [SerializeField] private float _scaleChangeAnimationDuration = 0.1f; // Duration for the scale change animation

        public bool IsHeld { get; private set; }

        private Tween _scaleTween;

        public void OnPointerDown(PointerEventData eventData)
        {
            _scaleTween?.Kill(); // Kill any existing tween to avoid conflicts

            IsHeld = true;
            _scaleTween = transform.DOScale(Vector3.one * 0.9f, _scaleChangeAnimationDuration); // Scale down the button when pressed
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _scaleTween?.Kill(); // Kill any existing tween to avoid conflicts

            IsHeld = false;
            _scaleTween = transform.DOScale(Vector3.one, _scaleChangeAnimationDuration); // Reset the button scale when released
        }
    }
}