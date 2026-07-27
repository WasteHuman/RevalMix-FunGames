using System;
using UnityEngine;

namespace Core.Gameplay
{
    public class EconomyController : MonoBehaviour
    {
        private static EconomyController _instance;

        [SerializeField] private float _initialBalance = 100000f;

        private float _currentCoinsBalance;
        private float _currentGemsBalance;
        private float _collectedCoins;

        public event Action<float> OnCoinsBalanceChanged;
        public event Action<float> OnGemsBalanceChanged;

        public static EconomyController Instance
        {
            get => _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _currentCoinsBalance = _initialBalance;

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Получить текущий баланс Coins
        /// </summary>
        public float GetCoinsBalance() => _currentCoinsBalance;

        /// <summary>
        /// Получить текущее количество собранных монет
        /// </summary>
        public float GetCollectedCoins() => _collectedCoins;

        /// <summary>
        /// Запросить текущий баланс Coins (invoke события)
        /// </summary>
        public void RequestCoinsBalance() => OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);

        /// <summary>
        /// Запросить текущий баланс Gems (invoke события)
        /// </summary>
        public void RequestGemsBanalce() => OnGemsBalanceChanged?.Invoke(_currentGemsBalance);

        /// <summary>
        /// Добавить средства (выигрыш, бонус)
        /// </summary>
        public void AddGems(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to add a negattive amount: {amount}. Use the SpendGems() method");
                return;
            }

            _currentGemsBalance += amount;
            OnGemsBalanceChanged?.Invoke(_currentGemsBalance);

            Debug.Log($"[Economy] Added gems: +{amount}. New balance: {_currentGemsBalance}");
        }

        /// <summary>
        /// Списать средства (ставка, проигрыш)
        /// </summary>
        public bool SpendGems(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to debit a negative amount: {amount}. Use the AddGems() method");
                return false;
            }

            if (!HasEnoughBalance(amount))
            {
                Debug.LogWarning($"Not enough gems! Balance: {_currentGemsBalance}, needed: {amount}");
                return false;
            }

            _currentGemsBalance -= amount;
            OnGemsBalanceChanged?.Invoke(_currentGemsBalance);

            Debug.Log($"[Economy] Debited: -{amount}. New gems balance: {_currentGemsBalance}");
            return true;
        }

        /// <summary>
        /// Добавить собранные монеты
        /// </summary>
        public void AddCollectedCoins(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to add a negattive amount: {amount}");
                return;
            }

            _collectedCoins += amount;

            Debug.Log($"[Economy] Added collected coins: +{amount}. New balance: {_collectedCoins}");
        }

        /// <summary>
        /// Добавить средства (выигрыш, бонус)
        /// </summary>
        public void AddCoins(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to add a negattive amount: {amount}. Use the SpendCoins() method");
                return;
            }

            _currentCoinsBalance += amount;
            OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);

            Debug.Log($"[Economy] Added coins: +{amount}. New balance: {_currentCoinsBalance}");
        }

        /// <summary>
        /// Списать средства (ставка, проигрыш)
        /// </summary>
        public bool SpendCoins(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to debit a negative amount: {amount}. Use the AddCoins() method");
                return false;
            }

            if (!HasEnoughBalance(amount))
            {
                Debug.LogWarning($"Not enough coins! Balance: {_currentCoinsBalance}, needed: {amount}");
                return false;
            }

            _currentCoinsBalance -= amount;
            OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);

            Debug.Log($"[Economy] Debited: -{amount}. New balance: {_currentCoinsBalance}");
            return true;
        }

        /// <summary>
        /// Проверить, достаточно ли средств
        /// </summary>
        public bool HasEnoughBalance(float amount) => _currentCoinsBalance >= amount;

        /// <summary>
        /// Установить баланс (для тестирования или загрузки из сохранений)
        /// </summary>
        public void SetBalance(float amount)
        {
            _currentCoinsBalance = Mathf.Max(0, amount);
            OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);
        }

        /// <summary>
        /// Сбросить собранные монеты
        /// </summary>
        public void ResetCollectedCoins() => _collectedCoins = 0f;

        /// <summary>
        /// Сбросить баланс на начальное значение
        /// </summary>
        public void ResetBalance()
        {
            _currentCoinsBalance = _initialBalance;
            OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);
        }
    }
}