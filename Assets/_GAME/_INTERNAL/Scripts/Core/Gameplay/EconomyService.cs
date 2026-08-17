using System;
using UnityEngine;

namespace Core.Gameplay
{
    public class EconomyService
    {
        private float _currentCoinsBalance;
        private bool _isDailyFreeBonusAvailable;

        private DateTime _dailyFreeBonusNextRefreshTimeUtc;

        public event Action<float> OnCoinsBalanceChanged;

        public void Init(float initialCoinsBalance)
        {
            _currentCoinsBalance = initialCoinsBalance;
            _dailyFreeBonusNextRefreshTimeUtc.AddDays(1);

            CheckDailyFreeBonus();
        }

        /// <summary>
        /// Получить текущий баланс Coins
        /// </summary>
        public float GetCoinsBalance() => _currentCoinsBalance;

        /// <summary>
        /// Запросить текущий баланс Coins (invoke события)
        /// </summary>
        public void RequestCoinsBalance() => OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);

        /// <summary>
        /// Запросить актуальность ежедневного бонуса
        /// </summary>
        public bool RequestDailyFreeBonusAvailable() => _isDailyFreeBonusAvailable;

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

        private void CheckDailyFreeBonus()
        {
            string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_BONUS_CLAIM, "");

            if (todayUtc != lastDate)
                _isDailyFreeBonusAvailable = true;
            else
            {
                _isDailyFreeBonusAvailable = false;
                Debug.LogWarning($"[Economy Service] Daily Free Bonus is already claimed!");
            }
        }
    }
}