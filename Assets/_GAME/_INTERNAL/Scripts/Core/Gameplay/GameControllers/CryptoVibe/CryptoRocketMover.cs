using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public class CryptoRocketMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed = 1f; // Юнитов в секунду
        [SerializeField] private float _rotationAngleOffset = 0f; // Дополнительный угол поворота (спрайт уже наклонён)
        [SerializeField] private float _descentSpeedMultiplier = 3f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _explosionVFXPrefab;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private AudioSource _audioSource;

        private CryptoPath _currentPath;
        private Coroutine _moveCoroutine;
        private Transform _rocketTransform;

        private bool _isMoving;

        private Tween _rotationTween;
        private float _currentAngle;

        private void OnDestroy()
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _rotationTween?.Kill();
        }

        /// <summary>
        /// Инициализация компонента.
        /// </summary>
        public void Initialize(Transform rocketTransform)
        {
            _rocketTransform = rocketTransform;

            if (_rocketTransform != null)
                _currentAngle = _rocketTransform.rotation.eulerAngles.z;
        }

        /// <summary>
        /// Запускает движение ракеты по заданному пути.
        /// </summary>
        public void StartMove(CryptoPath path, float flightTime)
        {
            if (_rocketTransform == null)
            {
                Debug.LogError("[CryptoVibeRocketMover] Rocket transform is not initialized!");
                return;
            }

            _currentPath = path;
            _isMoving = true;

            float totalDistance = 0f;
            for (int i = 0; i < _currentPath.AscentPoints.Length - 1; i++)
                totalDistance += Vector3.Distance(_currentPath.AscentPoints[i], _currentPath.AscentPoints[i + 1]);

            float safeFlightTime = Mathf.Max(0.1f, flightTime);
            _movementSpeed = totalDistance / safeFlightTime;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        /// <summary>
        /// Останавливает текущее движение.
        /// </summary>
        public void StopMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _rotationTween?.Kill();
            _rotationTween = null;
            _isMoving = false;
        }

        /// <summary>
        /// Запускает фазу падения (после краша).
        /// </summary>
        public void StartDescent(System.Action onComplete = null)
        {
            if (_currentPath == null || _currentPath.DescentPoints == null || _currentPath.DescentPoints.Length == 0)
            {
                Debug.LogWarning("[CryptoVibeRocketMover] No descent path available!");
                onComplete?.Invoke();
                return;
            }

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _rocketTransform.rotation = new(0f, 0f, 0f, 0f);

            _moveCoroutine = StartCoroutine(PlayDescentAnimation(onComplete));
        }

        private Vector3 GetPointOnPathByProgress(Vector3[] points, float progress)
        {
            if (points == null || points.Length == 0) return Vector3.zero;
            if (points.Length == 1) return points[0];

            float totalLength = 0f;
            float[] segmentLengths = new float[points.Length - 1];

            for (int i = 0; i < points.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(points[i], points[i + 1]);
                totalLength += segmentLengths[i];
            }

            float targetDistance = progress * totalLength;
            float accumulated = 0f;

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (accumulated + segmentLengths[i] >= targetDistance)
                {
                    float localT = (targetDistance - accumulated) / segmentLengths[i];
                    return Vector3.Lerp(points[i], points[i + 1], localT);
                }
                accumulated += segmentLengths[i];
            }

            return points[^1];
        }

        private IEnumerator MoveAlongPath()
        {
            if (_currentPath == null || _currentPath.AscentPoints == null || _currentPath.AscentPoints.Length == 0)
                yield break;

            Vector3[] points = _currentPath.AscentPoints;

            _rocketTransform.position = points[0];
            _currentAngle = _rocketTransform.rotation.eulerAngles.z;

            for (int i = 0; i < points.Length - 1; i++)
            {
                if (!_isMoving)
                    yield break;

                Vector3 startPoint = points[i];
                Vector3 endPoint = points[i + 1];

                float distance = Vector3.Distance(startPoint, endPoint);
                float duration = distance / _movementSpeed;

                // Плавный поворот к цели сегмента (один раз в начале сегмента)
                RotateSmooth(endPoint - startPoint, duration, false);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    _rocketTransform.position = Vector3.Lerp(startPoint, endPoint, t);
                    yield return null;
                }

                _rocketTransform.position = endPoint;
            }

            _isMoving = false;
        }

        private IEnumerator PlayDescentAnimation(System.Action onComplete)
        {
            if (_currentPath == null || _currentPath.DescentPoints == null || _currentPath.DescentPoints.Length == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3[] points = _currentPath.DescentPoints;
            _currentAngle = _rocketTransform.rotation.eulerAngles.z;

            float descentSpeed = _movementSpeed * _descentSpeedMultiplier;

            // Вычисляем общую длину пути
            float totalDistance = 0f;
            for (int i = 0; i < points.Length - 1; i++)
                totalDistance += Vector3.Distance(points[i], points[i + 1]);

            PlayExplosionEffect();
            float totalDuration = totalDistance / descentSpeed;

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / totalDuration);

                // Общее ускорение на весь путь (easing применяется один раз)
                float easedT = t * t;

                Vector3 pos = GetPointOnPathByProgress(points, easedT);
                _rocketTransform.position = pos;

                yield return null;
            }

            _rocketTransform.position = points[^1];
            
            onComplete?.Invoke();
        }

        /// <summary>
        /// Поворачивает ракету в направлении движения.
        /// </summary>
        private void RotateSmooth(Vector3 direction, float segmentDuration, bool isFalling = false)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            // Целевой угол
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetAngle += _rotationAngleOffset;

            // Кратчайший путь от текущего угла к целевому (через DeltaAngle)
            float delta = Mathf.DeltaAngle(_currentAngle, targetAngle);
            float endAngle = _currentAngle + delta;

            // Убиваем предыдущий твин поворота
            _rotationTween?.Kill();

            // Длительность поворота: для взлёта плавнее, для падения резче
            float tweenDuration = isFalling
                ? Mathf.Clamp(segmentDuration * 0.5f, 0.1f, 0.25f)
                : Mathf.Clamp(segmentDuration * 1.5f, 0.3f, 0.6f);

            // Для взлёта мягкий Ease, для падения резкий
            Ease ease = isFalling ? Ease.OutExpo : Ease.OutSine;

            _rotationTween = DOTween.To(
                    () => _currentAngle,
                    x =>
                    {
                        _currentAngle = x;
                        if (_rocketTransform != null)
                            _rocketTransform.rotation = Quaternion.Euler(0f, 0f, x);
                    },
                    endAngle,
                    tweenDuration)
                .SetEase(ease)
                .SetTarget(this);
        }

        /// <summary>
        /// Воспроизводит эффект взрыва.
        /// </summary>
        private void PlayExplosionEffect()
        {
            // Null-check для эффекта взрыва
            if (_explosionVFXPrefab != null)
            {
                var vfx = Instantiate(_explosionVFXPrefab, _rocketTransform.position, Quaternion.identity, _rocketTransform);

                // Попытка получить ParticleSystem.MainModule для настройки duration
                var ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    float duration = main.duration + main.startLifetime.constant;
                    Destroy(vfx.gameObject, duration);
                }
                else
                {
                    Destroy(vfx.gameObject, 2f);
                }
            }

            // Null-check для звука
            if (_explosionSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_explosionSound);
            }
        }

        /// <summary>
        /// Устанавливает эффекты (вызывается из контроллера).
        /// </summary>
        public void SetEffects(ParticleSystem explosionVFX, AudioClip explosionSound, AudioSource audioSource)
        {
            _explosionVFXPrefab = explosionVFX;
            _explosionSound = explosionSound;
            _audioSource = audioSource;
        }

        /// <summary>
        /// Устанавливает скорость движения.
        /// </summary>
        public void SetMovementSpeed(float speed)
        {
            _movementSpeed = speed;
        }

        /// <summary>
        /// Устанавливает угол поворота ракеты (смещение).
        /// </summary>
        public void SetRotationAngleOffset(float angleOffset)
        {
            _rotationAngleOffset = angleOffset;
        }
    }
}