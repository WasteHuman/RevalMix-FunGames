using System.Collections.Generic;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public class CryptoVibeLineRenderController : MonoBehaviour
    {
        [Header("Настройки линии")]
        [SerializeField] private LineRenderer _lineRendererPrefab;
        [SerializeField] private float _lineWidth = 0.15f;
        [SerializeField] private float _zOffset = -0.5f;

        [Header("Палитра цветов (градиент пути)")]
        [Tooltip("Градиент цветов для линии пути ракеты")]
        [SerializeField]
        private Gradient _pathGradient = new();

        [Header("AAA Настройки сглаживания")]
        [Range(1, 10)][SerializeField] private int _segmentsPerConnection = 5;

        [Header("Canvas Reference")]
        [Tooltip("Canvas для корректного преобразования координат")]
        [SerializeField] private RectTransform _canvasRectTransform;
        [SerializeField] private RectTransform _rocketTransform;

        private LineRenderer _activeLine;
        private bool _isWorldSpaceCanvas;

        // Для постепенной отрисовки
        private List<Vector3> _allPathPositions = new();
        private int _currentDrawIndex;
        private bool _isDrawing;
        private RectTransform _graphContainer;

        public int SegmentsPerConnection => _segmentsPerConnection;

        private void Awake()
        {
            // Проверяем тип Canvas для корректной работы с координатами
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _isWorldSpaceCanvas = canvas.renderMode == RenderMode.WorldSpace ||
                                      canvas.renderMode == RenderMode.ScreenSpaceCamera;
            }

            // Инициализируем градиент по умолчанию, если не задан
            if (_pathGradient.colorKeys.Length == 0)
            {
                SetupDefaultGradient();
            }
        }

        private void SetupDefaultGradient()
        {
            GradientColorKey[] colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.1f, 0.8f, 1f, 1f), 0f),      // Голубой (старт)
                new GradientColorKey(new Color(0.8f, 0.2f, 1f, 1f), 0.5f),   // Фиолетовый (середина)
                new GradientColorKey(new Color(1f, 0.2f, 0.2f, 1f), 1f)      // Красный (краш)
            };

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };

            _pathGradient.SetKeys(colorKeys, alphaKeys);
        }

        /// <summary>
        /// Инициализирует отрисовку пути ракеты. Вызывать при старте полёта.
        /// </summary>
        public void InitPath(CryptoPath path, RectTransform graphContainer)
        {
            ClearLine();

            if (path == null || path.AscentPoints == null || path.AscentPoints.Length == 0)
                return;

            _graphContainer = graphContainer;
            _allPathPositions.Clear();

            Vector3 worldOffsetToCenter = Vector3.zero;
            if (_rocketTransform != null)
            {
                Vector3 centerWorld = _rocketTransform.TransformPoint(_rocketTransform.rect.center);
                worldOffsetToCenter = centerWorld - _rocketTransform.position;
            }

            // Добавляем точки взлёта
            foreach (Vector3 point in path.AscentPoints)
            {
                Vector3 adjustedPoint = ConvertPositionForCanvas(point, graphContainer);
                adjustedPoint.z += _zOffset;

                if (_rocketTransform != null)
                {
                    adjustedPoint += worldOffsetToCenter;
                }

                _allPathPositions.Add(adjustedPoint);
            }

            // Добавляем точки падения (для визуализации полного пути)
            if (path.DescentPoints != null && path.DescentPoints.Length > 0)
            {
                foreach (Vector3 point in path.DescentPoints)
                {
                    Vector3 adjustedPoint = ConvertPositionForCanvas(point, graphContainer);
                    adjustedPoint.z += _zOffset;

                    // И для спада тоже применяем смещение
                    if (_rocketTransform != null)
                    {
                        adjustedPoint += worldOffsetToCenter;
                    }

                    _allPathPositions.Add(adjustedPoint);
                }
            }

            if (_allPathPositions.Count < 2)
                return;

            // Сглаживаем линию
            List<Vector3> subDividedPositions = SubdivideLine(_allPathPositions, _segmentsPerConnection);
            _allPathPositions = subDividedPositions;

            // Создаём LineRenderer с нулевым количеством точек
            _activeLine = Instantiate(_lineRendererPrefab, transform);
            _activeLine.useWorldSpace = true;
            _activeLine.positionCount = 0;
            _activeLine.startWidth = _lineWidth;
            _activeLine.endWidth = _lineWidth;

            _currentDrawIndex = 0;
            _isDrawing = true;
        }

        /// <summary>
        /// Обновляет отрисовку линии. Вызывать каждый кадр во время полёта ракеты.
        /// Передаётся текущий индекс прогресса (сколько точек уже должно быть видно).
        /// </summary>
        /// <param name="progressIndex">Текущий индекс точки пути (от 0 до Count-1)</param>
        public void UpdateLine(int progressIndex)
        {
            if (!_isDrawing || _activeLine == null || _allPathPositions.Count == 0)
                return;

            // Ограничиваем индекс максимумом
            int targetIndex = Mathf.Min(progressIndex, _allPathPositions.Count - 1);

            // Если достигли конца пути
            if (targetIndex >= _allPathPositions.Count - 1)
            {
                _isDrawing = false;
                targetIndex = _allPathPositions.Count - 1;
            }

            // Увеличиваем количество видимых точек
            if (targetIndex + 1 > _currentDrawIndex)
            {
                _currentDrawIndex = targetIndex + 1;
                _activeLine.positionCount = _currentDrawIndex;

                // Устанавливаем позиции для всех видимых точек
                Vector3[] visiblePositions = new Vector3[_currentDrawIndex];
                for (int i = 0; i < _currentDrawIndex; i++)
                {
                    visiblePositions[i] = _allPathPositions[i];
                }

                _activeLine.SetPositions(visiblePositions);

                // Применяем градиент к видимой части
                ApplyGradientToLine(_activeLine, _currentDrawIndex, _allPathPositions.Count);
            }
        }

        /// <summary>
        /// Отрисовывает весь путь сразу (для отладки или превью).
        /// </summary>
        public void DrawFullpath(CryptoPath path, RectTransform graphContainer)
        {
            InitPath(path, graphContainer);
            UpdateLine(_allPathPositions.Count - 1);
            _isDrawing = false;
        }

        /// <summary>
        /// Преобразует позицию в зависимости от типа Canvas.
        /// Для ScreenSpaceCamera конвертирует из локальных координат сетки в мировые.
        /// </summary>
        private Vector3 ConvertPositionForCanvas(Vector3 localPosition, RectTransform graphContainer)
        {
            if (graphContainer == null)
                return localPosition;

            // Если Canvas в режиме WorldSpace или ScreenSpaceCamera, используем мировые координаты
            if (_isWorldSpaceCanvas && _canvasRectTransform != null)
            {
                // TransformPoint учитывает масштаб, поворот и позицию родителя
                return graphContainer.TransformPoint(localPosition);
            }

            // Для Overlay используем экранные координаты
            return localPosition;
        }

        /// <summary>
        /// Применяет градиент к линии на основе прогресса до краша.
        /// </summary>
        private void ApplyGradientToLine(LineRenderer line, int visibleCount, int totalCount)
        {
            if (line == null || visibleCount < 2)
                return;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                _pathGradient.colorKeys,
                _pathGradient.alphaKeys
            );

            // Устанавливаем цвета для каждой видимой позиции вдоль линии
            Color[] colors = new Color[visibleCount];

            for (int i = 0; i < visibleCount; i++)
            {
                float t = (float)i / Mathf.Max(1, visibleCount - 1);
                colors[i] = gradient.Evaluate(t);
            }

            line.startColor = colors[0];
            line.endColor = colors[^1];
        }

        /// <summary>
        /// Разбивает линию на сегменты для сглаживания.
        /// </summary>
        private List<Vector3> SubdivideLine(List<Vector3> originalPoints, int subDivisions)
        {
            List<Vector3> newPoints = new List<Vector3>();

            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                Vector3 startNode = originalPoints[i];
                Vector3 endNode = originalPoints[i + 1];

                newPoints.Add(startNode);

                for (int j = 1; j < subDivisions; j++)
                {
                    float t = (float)j / subDivisions;
                    Vector3 intermediatePoint = Vector3.Lerp(startNode, endNode, t);
                    newPoints.Add(intermediatePoint);
                }
            }

            newPoints.Add(originalPoints[^1]);
            return newPoints;
        }

        /// <summary>
        /// Очищает отрисованную линию.
        /// </summary>
        public void ClearLine()
        {
            if (_activeLine != null)
            {
                Destroy(_activeLine.gameObject);
                _activeLine = null;
            }
        }

        /// <summary>
        /// Устанавливает префаб LineRenderer.
        /// </summary>
        public void SetLineRendererPrefab(LineRenderer prefab)
        {
            _lineRendererPrefab = prefab;
        }

        /// <summary>
        /// Устанавливает ширину линии.
        /// </summary>
        public void SetLineWidth(float width)
        {
            _lineWidth = width;
            if (_activeLine != null)
            {
                _activeLine.startWidth = width;
                _activeLine.endWidth = width;
            }
        }
    }
}