using Core.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers.Plinko
{
    /// <summary>
    /// Строит визуальную доску в редакторе из того же конфига,
    /// который использует PlinkoPathGenerator. Расхождение визуала и логики невозможно.
    /// </summary>
    [ExecuteAlways]
    public sealed class PlinkoBoardBuilder : MonoBehaviour
    {
        [SerializeField] private PlinkoConfig _config;
        [SerializeField] private RectTransform _pegPrefab;
        [SerializeField] private RectTransform _bucketPrefab;
        [SerializeField] private RectTransform _pegsRoot;
        [SerializeField] private RectTransform _bucketsRoot;

        private void OnEnable() => Rebuild();
        private void OnValidate() => Rebuild();

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            if (_config == null || Application.isPlaying)
                return;

            Clear(_pegsRoot);
            Clear(_bucketsRoot);

            var generator = new PlinkoPathGenerator(_config);

            // Пирамида колышков 3 → 10
            for (int row = 0; row < _config.PegRows; row++)
            {
                var pegsInRow = generator.GetPegCountInRow(row);
                for (int col = 0; col < pegsInRow; col++)
                {
                    var peg = Instantiate(_pegPrefab, _pegsRoot);
                    peg.position = generator.GetPegPosition(row, col);
                    peg.name = $"Peg_{row}_{col}";
                }
            }

            // 9 бакетов с множителями из конфига
            for (int i = 0; i < generator.GetBucketCount(); i++)
            {
                var bucket = Instantiate(_bucketPrefab, _bucketsRoot);
                bucket.position = generator.GetBucketPosition(i);
                bucket.name = $"Bucket_{i}";

                var bucketData = _config.Buckets[i];
                var spriteRenderer = bucket.GetComponent<Image>();
                spriteRenderer.sprite = bucketData.Sprite;
            }
        }

        private static void Clear(Transform root)
        {
            if (root == null) 
                return;
            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}