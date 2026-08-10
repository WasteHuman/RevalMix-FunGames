using UnityEngine;

namespace Core.SO
{
    [CreateAssetMenu(menuName = "Games/Plinko/Config")]
    public sealed class PlinkoConfig : ScriptableObject
    {
        [Header("Board")]
        public int PegRows = 12;
        public int PegsInFirstRow = 3;
        public float PegSpacing = 0.8f;
        public float RowSpacing = 0.9f;
        public float BucketSpacing = 1.25f;

        [Header("Drop")]
        public float DropX = 0f;
        public float BallStartOffsetY = 1.2f;

        [Header("Timing")]
        public float SegmentDuration = 0.18f;
        public float FinalFallDuration = 0.4f;

        [Header("Animation")]
        public bool SimplifiedAnimation = false;

        [Header("Randomness")]
        [Range(0f, 1f)] public float BiasToCenter = 0.55f; // шанс упасть "вниз-прямо"

        [Header("Buckets")]
        public PlinkoBucket[] Buckets; // массив слотов снизу с множителями

        private void OnValidate()
        {
            var expected = PegRows + 1;
            if (Buckets != null && Buckets.Length != expected)
                Debug.LogError($"[Plinko] Buckets.Length = {Buckets.Length}, expected {expected} (PegRows + 1)");
        }
    }

    [System.Serializable]
    public struct PlinkoBucket
    {
        public float Multiplier;     // 0.5x, 1x, 10x, 100x...
        public float Weight;         // для визуального баланса вероятностей
        public Sprite Sprite;
    }
}