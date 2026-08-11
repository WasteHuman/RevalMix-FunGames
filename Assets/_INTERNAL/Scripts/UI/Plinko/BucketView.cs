using System;
using UnityEngine;

namespace UI.Plinko
{
    public class BucketView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [SerializeField] private float _multiplier;

        public Action<float> OnBallEntered;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(collision.gameObject.TryGetComponent<PlayerBallView>(out var playerBall))
                HandleBallContact(playerBall);
        }

        public void Init(float multiplier, Sprite sprite)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = sprite;
            _multiplier = multiplier;
        }

        private void HandleBallContact(PlayerBallView playerBall)
        {
            OnBallEntered?.Invoke(_multiplier);
            Destroy(playerBall.gameObject);
        }
    }
}