using UnityEngine;

namespace Core.Data.Cyber21
{
    [System.Serializable]
    public struct CardData
    {
        public Sprite CardSprite;
        public int CardValue;
        public bool IsAce;

        public CardData(Sprite cardSprite, int cardValue, bool isAce)
        {
            CardSprite = cardSprite;
            CardValue = cardValue;
            IsAce = isAce;
        }
    }
}