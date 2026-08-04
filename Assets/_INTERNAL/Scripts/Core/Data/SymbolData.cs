using System;
using UnityEngine;

namespace Core.Data
{
    [Serializable]
    public struct SymbolData
    {
        public Sprite Sprite;
        public SymbolType Type;
        public int BaseReward;
    }
}