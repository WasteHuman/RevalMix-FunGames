using Core.Gameplay.GameControllers.Plinko;
using Core.SO;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Plinko
{
    public class PlayerBallView : MonoBehaviour
    {
        private MathBallMover _mathMover;

        /// <summary>
        /// Инициализирует мяч для движения по математическому пути.
        /// </summary>
        public void InitForMathMovement(
            PlinkoPath path,
            PlinkoConfig config,
            List<PegView> allPegs,
            List<BucketView> allBuckets,
            ParticleSystem hitVFX = null,
            AudioClip[] hitSounds = null,
            AudioSource audioSource = null)
        {
            // Добавляем компонент математического движения если его нет
            if (_mathMover == null)
                _mathMover = gameObject.GetComponent<MathBallMover>();

            if (_mathMover == null)
                _mathMover = gameObject.AddComponent<MathBallMover>();

            // Передаём эффекты и аудио
            if (hitVFX != null || hitSounds != null || audioSource != null)
                _mathMover.SetEffects(hitVFX, hitSounds, audioSource);

            // Настраиваем и запускаем движение
            _mathMover.StartMove(path, config, allPegs, allBuckets);
        }
    }
}