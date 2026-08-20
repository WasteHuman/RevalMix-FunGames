using System.Collections.Generic;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public class CryptoPathGenerator
    {
        private readonly System.Random _rng;
        private readonly int _seed;

        // Настройки генерации
        private readonly float _maxMultiplier;
        private readonly RectTransform _gridContainer;
        private readonly Vector2 _fallTargetPosition;
        private readonly float _ascentDeviation;
        private readonly float _descentDeviation;
        private readonly bool _isScreenSpaceCamera;

        /// <summary>
        /// Конструктор генератора пути.
        /// </summary>
        /// <param name="maxMultiplier">Максимальный множитель (35x).</param>
        /// <param name="gridContainer">Сетка UI, внутри которой строится путь.</param>
        /// <param name="fallTargetPosition">Целевая точка падения (в локальных координатах сетки).</param>
        /// <param name="ascentDeviation">Максимальное отклонение при взлёте.</param>
        /// <param name="descentDeviation">Максимальное отклонение при падении.</param>
        /// <param name="seed">Seed для воспроизводимости (-1 для случайного).</param>
        public CryptoPathGenerator(
            float maxMultiplier,
            RectTransform gridContainer,
            Vector2 fallTargetPosition,
            float ascentDeviation = 0.5f,
            float descentDeviation = 1f,
            int seed = -1)
        {
            _maxMultiplier = maxMultiplier;
            _gridContainer = gridContainer;
            _fallTargetPosition = fallTargetPosition;
            _ascentDeviation = ascentDeviation;
            _descentDeviation = descentDeviation;

            _seed = seed < 0 ? System.Environment.TickCount : seed;
            _rng = new System.Random(_seed);

            _isScreenSpaceCamera = IsScreenSpaceCamera(gridContainer);
        }

        /// <summary>
        /// Генерирует полный путь ракеты на основе множителя краша.
        /// </summary>
        /// <param name="crashMultiplier">Множитель, на котором произойдёт краш.</param>
        /// <param name="startPosition">Стартовая позиция ракеты (в локальных координатах сетки).</param>
        /// <returns>CryptoVibePath с точками взлёта и падения.</returns>
        public CryptoPath GeneratePath(float crashMultiplier, Vector2 startPosition)
        {
            float clampedMultiplier = Mathf.Clamp(crashMultiplier, 1f, _maxMultiplier);
            Rect gridRect = GetGridRect();

            Vector2 fallTargetWorld = new(gridRect.xMax, gridRect.yMin);

            Vector3[] ascentPoints = GenerateAscentPath(clampedMultiplier, gridRect);
            Vector3 crashPoint = ascentPoints[^1];

            Vector3[] descentPoints = GenerateDescentPath(crashPoint, fallTargetWorld);
            int crashPointIndex = ascentPoints.Length - 1;

            return new CryptoPath(
                ascentPoints,
                descentPoints,
                crashPointIndex,
                clampedMultiplier,
                _seed
            );
        }

        /// <summary>
        /// Проверяет, использует ли Canvas режим ScreenSpaceCamera.
        /// </summary>
        private bool IsScreenSpaceCamera(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return false;

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            return canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay == false
                   && canvas.renderMode == RenderMode.ScreenSpaceCamera;
        }

        /// <summary>
        /// Генерирует путь взлёта с небольшими отклонениями.
        /// Путь строится линейно вправо-вверх с учётом множителя.
        /// </summary>
        private Vector3[] GenerateAscentPath(float crashMultiplier, Rect gridRect)
        {
            int pointCount = Mathf.Max(5, Mathf.FloorToInt(crashMultiplier * 1.5f));
            var points = new List<Vector3>();

            Vector3 startPoint = new(gridRect.xMin, gridRect.yMin, 0f);
            points.Add(startPoint);

            float normalizedProgress = (crashMultiplier - 1f) / (_maxMultiplier - 1f);

            float targetX = Mathf.Lerp(gridRect.xMin + 0.5f, gridRect.xMax - 0.5f, normalizedProgress);
            float targetY = Mathf.Lerp(gridRect.yMin + 0.5f, gridRect.yMax - 0.5f, normalizedProgress);

            targetX = Mathf.Clamp(targetX, gridRect.xMin + 0.1f, gridRect.xMax - 0.1f);
            targetY = Mathf.Clamp(targetY, gridRect.yMin + 0.1f, gridRect.yMax - 0.1f);

            float scaleModifier = _isScreenSpaceCamera ? 0.3f : 1f;
            float adjustedAscentDeviation = _ascentDeviation * scaleModifier;

            for (int i = 1; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);

                float baseX = Mathf.Lerp(startPoint.x, targetX, t);
                float baseY = Mathf.Lerp(startPoint.y, targetY, t);

                float deviationX = ((float)_rng.NextDouble() - 0.5f) * 2f * adjustedAscentDeviation;
                float deviationY = ((float)_rng.NextDouble() - 0.5f) * 2f * adjustedAscentDeviation;

                float finalX = Mathf.Clamp(baseX + deviationX, gridRect.xMin, gridRect.xMax);
                float finalY = Mathf.Clamp(baseY + deviationY, gridRect.yMin, gridRect.yMax);

                points.Add(new Vector3(finalX, finalY, 0f));
            }

            points.Add(new Vector3(targetX, targetY, 0f));

            return points.ToArray();
        }

        /// <summary>
        /// Генерирует путь падения от точки краша до целевой точки.
        /// Путь имеет небольшие отклонения для реалистичности.
        /// </summary>
        private Vector3[] GenerateDescentPath(Vector3 startPoint, Vector2 endPoint)
        {
            var points = new List<Vector3>
            {
                startPoint
            };

            // Количество точек падения
            int pointCount = 8;

            float scaleModifier = _isScreenSpaceCamera ? 0.3f : 1f;
            float adjustedDescentDeviation = _descentDeviation * scaleModifier;

            for (int i = 1; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);

                // Базовая позиция на линии от краша к цели
                float baseX = Mathf.Lerp(startPoint.x, endPoint.x, t);
                float baseY = Mathf.Lerp(startPoint.y, endPoint.y, t);

                // Добавляем отклонения (уменьшаются по мере приближения к цели)
                float deviationFactor = 1f - t; // Отклонения уменьшаются к концу
                float deviationX = ((float)_rng.NextDouble() - 0.5f) * 2f * adjustedDescentDeviation * deviationFactor;
                float deviationY = ((float)_rng.NextDouble() - 0.5f) * 2f * adjustedDescentDeviation * deviationFactor;

                points.Add(new Vector3(baseX + deviationX, baseY + deviationY, 0f));
            }

            // Добавляем финальную точку
            points.Add(new Vector3(endPoint.x, endPoint.y, 0f));

            return points.ToArray();
        }

        /// <summary>
        /// Возвращает прямоугольник сетки в мировых координатах.
        /// </summary>
        private Rect GetGridRect()
        {
            if (_gridContainer == null)
            {
                Debug.LogWarning("[CryptoVibePathGenerator] Grid container is null!");
                return new Rect(-5f, -5f, 10f, 10f);
            }

            // Получаем угловые точки сетки в мировых координатах
            Vector3[] corners = new Vector3[4];
            _gridContainer.GetWorldCorners(corners);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var corner in corners)
            {
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Преобразует локальные координаты сетки в мировые.
        /// </summary>
        private Vector2 LocalToGridWorld(Vector2 localPosition)
        {
            if (_gridContainer == null)
                return Vector2.zero;

            // TransformPoint автоматически учитывает позицию, поворот и масштаб родителя
            return _gridContainer.TransformPoint(localPosition);
        }
    }
}