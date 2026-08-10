using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class PlinkoBoardView : MonoBehaviour
    {
        [SerializeField] private Sprite _pegNormal;
        [SerializeField] private Sprite _pegGlow;

        private readonly Dictionary<int, Image> _pegs = new();

        private void Awake()
        {
            foreach (var sr in GetComponentsInChildren<Image>(true))
            {
                var parts = sr.name.Split('_');
                if (parts.Length == 3 && parts[0] == "Peg"
                    && int.TryParse(parts[1], out var row)
                    && int.TryParse(parts[2], out var col))
                {
                    _pegs[row * 100 + col] = sr;
                }
            }
        }

        public void HighlightPeg(int row, int col)
        {
            if (!_pegs.TryGetValue(row * 100 + col, out Image sr)) 
                return;

            sr.sprite = _pegGlow;
            sr.color = Color.white;

            // Анимация glow с pulse-эффектом для имитации свечения от удара
            DOTween.Sequence()
                .Append(sr.DOColor(new Color(1f, 1f, 1f, 0.6f), 0.08f).SetEase(Ease.OutQuad))
                .AppendInterval(0.1f)
                .AppendCallback(() =>
                {
                    sr.sprite = _pegNormal;
                    sr.color = Color.white;
                });
        }
    }
}