using Core.SO;
using DG.Tweening;
using System;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public sealed class BallAnimator
    {
        private readonly RectTransform _ball;
        private readonly PlinkoConfig _config;
        private readonly Vector3 _baseScale;

        public BallAnimator(RectTransform ball, PlinkoConfig config)
        {
            _ball = ball;
            _config = config;
            _baseScale = ball.localScale;
        }

        public Sequence Animate(
            PlinkoPath path,
            Action<int> onComplete,
            Action<PlinkoHop> onPegHit = null)
        {
            var seq = DOTween.Sequence();

            for (int i = 0; i < path.Hops.Length; i++)
            {
                var hop = path.Hops[i];
                var isFinal = hop.PegRow < 0;
                var duration = isFinal ? _config.FinalFallDuration : _config.SegmentDuration;

                // Хопы идут цепочкой Append → тайминги совпадают с визуалом структурно
                seq.Append(_ball
                    .DOPath(hop.Points, duration, PathType.CatmullRom)
                    .SetEase(Ease.Linear));

                if (isFinal) break;

                // Callback строго в момент удара
                var hit = hop;
                seq.AppendCallback(() => onPegHit?.Invoke(hit));

                if (!_config.SimplifiedAnimation)
                {
                    // Squash ОТНОСИТЕЛЬНО базового scale; 0.1s < 0.18s — не перекрывает следующий хоп
                    seq.Insert(seq.Duration(false), _ball
                        .DOScale(SquashScale(), 0.05f)
                        .SetLoops(2, LoopType.Yoyo));
                }
            }

            if (!_config.SimplifiedAnimation)
            {
                // Вращение только вокруг Z — безопасно для 2D-спрайта
                seq.Join(_ball
                    .DORotate(new Vector3(0f, 0f, 720f), seq.Duration(false), RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear));
            }

            // Мяч «оседает» в бакете — тоже относительный scale
            seq.Insert(seq.Duration(false) - 0.1f, _ball
                .DOScale(_baseScale * 0.85f, 0.1f)
                .SetEase(Ease.InQuad));

            // Finish строго после приземления
            seq.AppendCallback(() => onComplete(path.BucketIndex));
            return seq;
        }

        private Vector3 SquashScale() => new(_baseScale.x * 1.15f, _baseScale.y * 0.85f, _baseScale.z);

    }
}