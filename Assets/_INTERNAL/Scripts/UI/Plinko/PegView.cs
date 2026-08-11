using UnityEngine;

namespace UI.Plinko
{
    public class PegView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Sprite _hitSprite;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent<PlayerBallView>(out var player))
                _spriteRenderer.sprite = _hitSprite;
        }

        private void OnCollisionExit2D(Collision2D collision) => _spriteRenderer.sprite = _defaultSprite;
    }
}