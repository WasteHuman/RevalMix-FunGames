using Core.SO;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public sealed class PlinkoPathGenerator
    {
        private readonly PlinkoConfig _config;
        private readonly System.Random _rng;
        private readonly int _seed;

        public PlinkoPathGenerator(PlinkoConfig config, int seed = -1)
        {
            _config = config;
            _seed = seed < 0 ? System.Environment.TickCount : seed;
            _rng = new System.Random(_seed);
        }

        public int GetPegCountInRow(int row) => _config.PegsInFirstRow + row;

        public int GetBucketCount() => _config.PegRows + 1;

        public PlinkoPath GeneratePath(float dropX)
        {
            var hops = new List<PlinkoHop>();

            var col = GetStartColumn();
            var rights = 0;
            var currentPos = new Vector3(dropX, GetTopY(), 0f);

            for (int row = 0; row < _config.PegRows; row++)
            {
                var pegPos = GetPegPosition(row, col);
                hops.Add(new PlinkoHop(BuildArc(currentPos, pegPos), row, col));

                if (ChooseDirection() > 0) rights++;
                col = 1 + rights;
                currentPos = pegPos;
            }

            var bucketIndex = rights;
            hops.Add(new PlinkoHop(BuildArc(currentPos, GetBucketPosition(bucketIndex)), -1, bucketIndex));

            return new PlinkoPath(hops.ToArray(), bucketIndex, _seed);
        }

        /// <summary>Центральный колышек первого ряда.</summary>
        public int GetStartColumn() => (_config.PegsInFirstRow - 1) / 2;

        /// <summary>
        /// Позиция колышка. Формула центрирования сама даёт шахматное смещение:
        /// для нечётных рядов (pegsInRow - 1) / 2 даёт .5 → сдвиг на полшага.
        /// </summary>
        public Vector3 GetPegPosition(int row, int col)
        {
            var pegsInRow = GetPegCountInRow(row);

            if (col < 0 || col >= pegsInRow)
            {
                Debug.LogError($"[Plinko] Peg col {col} out of range [0..{pegsInRow - 1}] at row {row}");
                col = Mathf.Clamp(col, 0, pegsInRow - 1);
            }

            var x = (col - (pegsInRow - 1) / 2f) * _config.PegSpacing;
            var y = GetTopY() - row * _config.RowSpacing;
            return new Vector3(x, y, 0f);
        }

        /// <summary>Бакеты лежат во внутренних зазорах последнего ряда, шаг = PegSpacing.</summary>
        public Vector3 GetBucketPosition(int bucketIndex)
        {
            var count = GetBucketCount();
            if (bucketIndex < 0 || bucketIndex >= count)
            {
                Debug.LogError($"[Plinko] Bucket {bucketIndex} out of range [0..{count - 1}]");
                bucketIndex = Mathf.Clamp(bucketIndex, 0, count - 1);
            }

            var x = (bucketIndex - (count - 1) / 2f) * _config.BucketSpacing;
            var y = GetTopY() - _config.PegRows * _config.RowSpacing - _config.RowSpacing * 0.5f;
            return new Vector3(x, y, 0f);
        }

        private Vector3[] BuildArc(Vector3 from, Vector3 to, int samples = 4)
        {
            var points = new List<Vector3>();

            // Вертикальное падение (старт) — без дуги
            if (Mathf.Abs(to.x - from.x) < 0.001f)
            {
                for (int i = 1; i <= samples; i++)
                    points.Add(Vector3.Lerp(from, to, (float)i / samples));
                return points.ToArray();
            }

            var arcHeight = (from.y - to.y) * 0.35f;
            var mid = (from + to) / 2f + new Vector3(0, arcHeight, 0);

            for (int i = 1; i <= samples; i++)
            {
                var t = (float)i / samples;
                points.Add(Mathf.Pow(1 - t, 2) * from
                         + 2 * (1 - t) * t * mid
                         + Mathf.Pow(t, 2) * to);
            }
            return points.ToArray();
        }

        private float GetTopY() => (_config.PegRows - 1) * _config.RowSpacing + _config.BallStartOffsetY;

        private int ChooseDirection() => _rng.NextDouble() < 0.5 ? -1 : 1;
    }
}