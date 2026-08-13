using Core.Services.Quests;
using System;
using System.Collections;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestsUIView : MonoBehaviour
    {
        [SerializeField] private DailyQuestViewsHolder _viewsHolder;
        [SerializeField] private TextMeshProUGUI _refreshTimerLabel;

        private DailyQuestsService _dailyQuestsService;
        private Coroutine _timerCoroutine;

        public void Init(DailyQuestsService dailyQuestsService)
        {
            _dailyQuestsService = dailyQuestsService;
            _viewsHolder.Init(_dailyQuestsService);
        }

        public void SetupViewsHolder() => _viewsHolder.SetupQuestViews();

        public void Dispose()
        {
            _viewsHolder.Dispose();
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);
        }

        private void StartTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(UpdateTimerRoutine());
        }

        private IEnumerator UpdateTimerRoutine()
        {
            while (true)
            {
                // Опрос API сервиса
                var timeLeft = _dailyQuestsService.GetTimeUntilRefresh();

                if (_refreshTimerLabel != null)
                    _refreshTimerLabel.text = FormatTime(timeLeft);

                yield return new WaitForSeconds(1f);
            }
        }

        private string FormatTime(TimeSpan ts)
        {
            // Если по какой-то причине время отрицательное, показываем нули
            if (ts <= TimeSpan.Zero) 
                return "00:00:00";
            return $"Refresh in: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}