using Core.SO;
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

        private float GetTopY() => (_config.PegRows - 1) * _config.RowSpacing + _config.DropY;
    }
}