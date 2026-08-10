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
                var duration = isFinal? _config.FinalFallDuration : GetPhysicsLikeDuration(hop, i);

                // Хопы идут цепочкой Append → тайминги совпадают с визуалом структурно
                seq.Append(_ball
                    .DOPath(hop.Points, duration, PathType.CatmullRom)
                    .SetEase(isFinal ? Ease.InQuad : Ease.OutQuad));

                if (isFinal) 
                    break;

                // Callback строго в момент удара
                var hit = hop;
                seq.AppendCallback(() => onPegHit?.Invoke(hit));

                if (!_config.SimplifiedAnimation)
                {
                    // Выраженный squash/stretch при ударе — имитация деформации мяча
                    seq.Insert(seq.Duration(false), CreateSquashStretchSequence());

                    // Небольшое случайное вращение для каждого отскока
                    var randomRot = UnityEngine.Random.Range(45f, 90f) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
                    seq.Insert(seq.Duration(false), _ball
                        .DORotate(new Vector3(0f, 0f, _ball.eulerAngles.z + randomRot), 0.12f)
                        .SetEase(Ease.OutQuad));
                }
            }

            if (!_config.SimplifiedAnimation)
            {
                // Постоянное вращение во время падения — добавляет динамики
                seq.Join(_ball
                    .DORotate(new Vector3(0f, 0f, 1080f), seq.Duration(false), RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear));
            }

            // Мяч «оседает» в бакете — плавное затухание scale
            seq.Insert(seq.Duration(false) - 0.15f, _ball
                .DOScale(_baseScale * 0.75f, 0.15f)
                .SetEase(Ease.InBack));

            // Finish строго после приземления
            seq.AppendCallback(() => onComplete(path.BucketIndex));
            return seq;
        }

        /// <summary>
        /// Расчёт длительности хопа на основе "физики":
        /// - Чем больше вертикальное расстояние, тем быстрее (ускорение гравитации)
        /// - Чем больше горизонтальное смещение, тем дольше (инерция)
        /// - Ранние хопы быстрее, поздние медленнее (мяч теряет энергию)
        /// </summary>
        private float GetPhysicsLikeDuration(PlinkoHop hop, int hopIndex)
        {
            var verticalDist = Mathf.Abs(hop.Points[0].y - hop.Points[^1].y);
            var horizontalDist = Mathf.Abs(hop.Points[0].x - hop.Points[^1].x);

            // Базовая длительность от вертикального расстояния (гравитация)
            var baseDuration = Mathf.Sqrt(verticalDist / 20f) * 2f;

            // Горизонтальное движение добавляет время (инерция)
            var horizontalFactor = 1f + (horizontalDist / 10f);

            // Мяч немного теряет энергию с каждым отскоком
            var energyLoss = 1f - (hopIndex * 0.02f);
            energyLoss = Mathf.Clamp(energyLoss, 0.6f, 1f);

            var duration = baseDuration * horizontalFactor / energyLoss;

            // Ограничиваем разумными пределами
            return Mathf.Clamp(duration, 0.08f, 0.25f);
        }

        /// <summary>
        /// Создаёт последовательность squash/stretch для имитации деформации мяча при ударе.
        /// </summary>
        private Sequence CreateSquashStretchSequence()
        {
            var squashSeq = DOTween.Sequence();

            // Предварительное растяжение перед ударом (anticipation)
            squashSeq.Append(_ball
                .DOScale(new Vector3(_baseScale.x * 0.9f, _baseScale.y * 1.1f, _baseScale.z), 0.04f)
                .SetEase(Ease.OutQuad));

            // Резкий squash в момент удара
            squashSeq.Append(_ball
                .DOScale(SquashScale(), 0.06f)
                .SetEase(Ease.InOutQuad));

            // Возврат к норме с небольшим bounce
            squashSeq.Append(_ball
                .DOScale(_baseScale * 1.05f, 0.05f)
                .SetEase(Ease.OutQuad));

            squashSeq.Append(_ball
                .DOScale(_baseScale, 0.03f)
                .SetEase(Ease.Linear));

            return squashSeq;
        }

        private Vector3 SquashScale() => new(_baseScale.x * 1.25f, _baseScale.y * 0.75f, _baseScale.z);

    }
}