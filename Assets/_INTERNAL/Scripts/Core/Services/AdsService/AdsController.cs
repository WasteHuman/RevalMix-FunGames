using StartApp;
using System;
using UnityEngine;

namespace Core.Services.AdsService
{
    public class AdsController : MonoBehaviour
    {
        private static AdsController _instance;

        private InterstitialAd _rewardedAd;
        private Action _onRewardCallback;

        public static AdsController Instance
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
            DontDestroyOnLoad(gameObject);

            InitializeAds();
        }

        private void OnDestroy()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.RaiseAdLoaded -= OnAdLoaded;
                _rewardedAd.RaiseAdVideoCompleted -= OnVideoCompleted;
                _rewardedAd.RaiseAdLoadingFailed -= OnAdLoadingFailed;
                _rewardedAd.RaiseAdClosed -= OnAdClosed;
            }
        }

        private void InitializeAds()
        {
            return;

            Debug.Log("[Ads] Initialize Start.io...");

            _rewardedAd = AdSdk.Instance.CreateInterstitial();

            _rewardedAd.RaiseAdLoaded += OnAdLoaded;
            _rewardedAd.RaiseAdVideoCompleted += OnVideoCompleted;
            _rewardedAd.RaiseAdLoadingFailed += OnAdLoadingFailed;
            _rewardedAd.RaiseAdClosed += OnAdClosed;

            PreloadRewardedAd();
        }

        public void PreloadRewardedAd()
        {
            return;

            if (_rewardedAd == null)
            {
                Debug.LogWarning("[Ads] Rewarded ad is not initialized!");
                return;
            }

            if (_rewardedAd.IsReady())
            {
                Debug.Log("[Ads] Rewarded is already loaded");
                return;
            }

            Debug.Log("[Ads] Start loading rewarded ads...");
            _rewardedAd.LoadAd(InterstitialAd.AdType.Rewarded);
        }

        public void ShowRewardedAd(Action onRewardCallback)
        {
            return;

            if (_rewardedAd == null)
            {
                Debug.LogWarning("[Ads] Rewarded ad is not initialized!");
                return;
            }

            if (!_rewardedAd.IsReady())
            {
                Debug.LogWarning("[Ads] The rewarded ad has not been uploaded yet!");
                return;
            }

            _onRewardCallback = onRewardCallback;

            Debug.Log("[Ads] Show rewarded ad...");
            _rewardedAd.ShowAd();
        }

        public bool IsRewardedAdLoaded() => _rewardedAd != null && _rewardedAd.IsReady();

        private void OnAdLoaded(object sender, EventArgs e)
        {
            Debug.Log("[Ads] The rewarded ad has been uploaded and is ready to be displayed");
        }

        private void OnVideoCompleted(object sender, EventArgs e)
        {
            Debug.Log("[Ads] The user has finished viewing the ad");

            _onRewardCallback?.Invoke();
            _onRewardCallback = null;
        }

        private void OnAdLoadingFailed(object sender, MessageArgs e)
        {
            Debug.LogWarning($"[Ads] Error loading a rewarded ad: {e.Message}");
        }

        private void OnAdClosed(object sender, EventArgs e)
        {
            Debug.Log("[Ads] Rewarded ad is closed");

            PreloadRewardedAd();
        }
    }
}