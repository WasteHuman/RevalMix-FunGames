using Core.SO;
using System.Collections;
using System.Collections.Generic;
using UI.Plinko;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class MathBallMover : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _hopDuration = 0.15f;
        [SerializeField] private float _finalFallDuration = 0.3f;
        [SerializeField] private ParticleSystem _hitVFXPrefab;
        [SerializeField] private AudioClip[] _hitSounds;
        [SerializeField] private AudioSource _audioSource;

        private PlinkoPath _path;
        private PlinkoConfig _config;
        private Coroutine _moveCoroutine;
        private int _currentHopIndex;

        private readonly List<PegView> _allPegs = new();
        private readonly List<BucketView> _allBuckets = new();

        public void SetInitialPosition(Vector3 position) => transform.position = position;

        /// <summary>
        /// Запускает анимацию движения мяча по заданному пути.
        /// </summary>
        public void StartMove(PlinkoPath path, PlinkoConfig config, List<PegView> allPegs, List<BucketView> allBuckets)
        {
            _path = path;
            _config = config;
            _currentHopIndex = 0;

            _allPegs.Clear();
            _allPegs.AddRange(allPegs);

            _allBuckets.Clear();
            _allBuckets.AddRange(allBuckets);

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        /// <summary>
        /// Останавливает анимацию движения.
        /// </summary>
        public void StopMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
        }

        private IEnumerator MoveAlongPath()
        {
            for (int i = 0; i < _path.Hops.Length; i++)
            {
                var hop = _path.Hops[i];
                bool isFinalHop = (i == _path.Hops.Length - 1);

                // Анимация текущего хопа
                yield return StartCoroutine(AnimateHop(hop, isFinalHop));

                _currentHopIndex++;
            }

            // Движение завершено, сообщаем о результате
            OnMoveCompleted();
        }

        private IEnumerator AnimateHop(PlinkoHop hop, bool isFinalHop)
        {
            if (hop.Points == null || hop.Points.Length < 2)
            {
                yield break;
            }

            float duration = isFinalHop ? _finalFallDuration : _hopDuration;

            // Анимация между точками хопа
            for (int i = 0; i < hop.Points.Length - 1; i++)
            {
                Vector3 startPoint = hop.Points[i];
                Vector3 endPoint = hop.Points[i + 1];

                float elapsed = 0f;
                float segmentDuration = duration / (hop.Points.Length - 1);

                while (elapsed < segmentDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / segmentDuration);

                    // Интерполяция позиции
                    transform.position = Vector3.Lerp(startPoint, endPoint, t);

                    yield return null;
                }

                transform.position = endPoint;

                // Если это не финальный хоп и мы достигли точки удара о пег
                if (!isFinalHop && hop.PegRow >= 0 && i == hop.Points.Length - 2)
                {
                    Debug.Log($"[MathBallMover] Triggering peg glow at row={hop.PegRow}, col={hop.PegCol}");

                    // Вызываем эффект на пеге
                    TriggerPegGlow(hop.PegRow, hop.PegCol);

                    // Воспроизводим остальные эффекты (VFX, звук)
                    PlayHitEffects(endPoint);
                }
            }
        }

        /// <summary>
        /// Находит пег по ряду и колонке и вызывает у него эффект свечения.
        /// </summary>
        private void TriggerPegGlow(int pegRow, int pegCol)
        {
            if (_allPegs == null || _allPegs.Count == 0)
            {
                Debug.LogWarning("[MathBallMover] No pegs found in scene!");
                return;
            }

            // Вычисляем ожидаемую позицию пега
            Vector3 expectedPos = GetExpectedPegPosition(pegRow, pegCol);
            Debug.Log($"[MathBallMover] Looking for peg at row={pegRow}, col={pegCol}, expectedPos={expectedPos}");

            // Ищем пег с соответствующими координатами
            PegView targetPeg = null;
            float minDistance = float.MaxValue;

            foreach (var peg in _allPegs)
            {
                if (peg == null) 
                    continue;

                Vector3 pegPos = peg.transform.position;
                float distance = Vector3.Distance(pegPos, expectedPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetPeg = peg;
                }
            }

            if (targetPeg != null && minDistance < 0.5f)
                targetPeg.TriggerHit();
            else
                Debug.LogWarning($"[MathBallMover] Peg not found! Closest distance={minDistance:F3}, threshold=0.5");
        }

        /// <summary>
        /// Вычисляет ожидаемую позицию пега по ряду и колонке на основе конфига.
        /// </summary>
        private Vector3 GetExpectedPegPosition(int row, int col)
        {
            int pegsInRow = _config.PegsInFirstRow + row;

            if (col < 0 || col >= pegsInRow)
                return Vector3.zero;

            float x = (col - (pegsInRow - 1) / 2f) * _config.PegSpacing;
            float y = ((_config.PegRows - 1) * _config.RowSpacing + _config.SpawnPoint.y) - row * _config.RowSpacing;

            return new Vector3(x, y, 0f);
        }

        private void PlayHitEffects(Vector3 position)
        {
            // VFX
            if (_hitVFXPrefab != null)
            {
                var vfx = Instantiate(_hitVFXPrefab, position, Quaternion.identity);
                Destroy(vfx.gameObject, 2f);
            }

            // Sound
            if (_audioSource != null && _hitSounds != null && _hitSounds.Length > 0)
            {
                AudioClip sound = _hitSounds[Random.Range(0, _hitSounds.Length)];
                _audioSource.PlayOneShot(sound);
            }
        }

        private void OnMoveCompleted()
        {
            if (_path.BucketIndex >= 0 && _path.BucketIndex < _allBuckets.Count)
            {
                BucketView targetBucket = _allBuckets[_path.BucketIndex];
                targetBucket.InvokeBallEntered();
            }

            // Уничтожаем мяч после завершения
            Destroy(gameObject);
        }

        /// <summary>
        /// Передаёт ссылки на эффекты и аудио из контроллера.
        /// </summary>
        public void SetEffects(ParticleSystem hitVFX, AudioClip[] hitSounds, AudioSource audioSource)
        {
            _hitVFXPrefab = hitVFX;
            _hitSounds = hitSounds;
            _audioSource = audioSource;
        }
    }
}