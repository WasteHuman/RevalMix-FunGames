using System;
using UnityEngine;

namespace Core.WheelOfLuck
{
    [Serializable]
    public class WheelReward
    {
        public enum RewardType
        {
            FreeSpin,
            Coins,
            Nothing,
            Multiplier
        }

        [Tooltip("Тип награды")]
        public RewardType Type = RewardType.Nothing;

        [Tooltip("Количество (монет или спинов)")]
        public float Amount = 0;

        [Tooltip("Вес при случайном выборе (чем больше — тем чаще выпадет)")]
        public float Weight = 1f;

        public override string ToString() => $"{Type} x{Amount}";
    }
}