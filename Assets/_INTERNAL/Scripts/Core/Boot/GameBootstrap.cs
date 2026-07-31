using Core.Services;
using Core.Services.AdsService;
using Core.Services.Analytics;
using Core.Services.Audio;
using Io.AppMetrica;
using System.Collections;
using System.Threading.Tasks;
using UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Boot
{
    public class GameBootstrap
    {
        private static GameBootstrap _instance;

        private AnalyticsService _analyticsService;
        private AdsService _adsService;
        private AudioService _audioService;

        private Coroutine _loadingCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static async Task AutoStart()
        {
            _instance = new();

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_EDITOR
            PlayerPrefs.DeleteAll();
#endif
            PlayerPrefs.DeleteKey("ShownLetsPlay");

            await InitializeExternalSDK();
            Run();
        }
        
        private static async Task InitializeExternalSDK()
        {
            CheckFirstLaunch();

            var analyticsServicePrefab = Resources.Load<AnalyticsService>("Prefabs/Services/[ANALYTICS_SERVICE]");
            var adsControllerPrefab = Resources.Load<AdsService>("Prefabs/Services/[ADS_CONTROLLER]");
            var audioControllerPrefab = Resources.Load<AudioService>("Prefabs/Services/[AUDIO_CONTROLLER]");

            if(analyticsServicePrefab == null || adsControllerPrefab == null || adsControllerPrefab == null)
            {
                Debug.LogError($"[Game Bootstrap] Analytics Service/Ads Service/Audio Service prefab is null!");
                return;
            }

            _instance._analyticsService = Object.Instantiate(analyticsServicePrefab);
            _instance._adsService = Object.Instantiate(adsControllerPrefab);
            _instance._audioService = Object.Instantiate(audioControllerPrefab);

            try
            {
                AppMetrica.Activate(new AppMetricaConfig("32e9a816-0394-4115-ac04-ecd60f9bebea")
                {
                    FirstActivationAsUpdate = !IsFirstLaunch()
                });
                Debug.Log($"[GlobalAction Bootstrap] AppMetrica initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GlobalAction Bootstrap] Failed to initialize AppMetrica: {ex.Message}");
            }
        }

        private static void Run()
        {
            _instance.LoadMainScene();
            GameServices.InitializeAll();
        }

        private static bool IsFirstLaunch()
        {
            // TODO: Сделать проверку не только по ключу PlayerPrefs, но и по другим критериям
            if (!PlayerPrefs.HasKey("First_Launch"))
                return false;

            return true;
        }

        private static void CheckFirstLaunch()
        {
            if (PlayerPrefs.HasKey("First_Launch"))
                PlayerPrefs.SetInt("First_Launch", 1);
        }

        private void LoadMainScene()
        {
            var loadingScreenViewPrefab = Resources.Load<UILoadingView>("Prefabs/UI/UILoadingView");
            var loadingScreenView = Object.Instantiate(loadingScreenViewPrefab);

            if (loadingScreenViewPrefab == null)
            {
                Debug.LogError($"[GlobalAction Bootstrap] Loading Screen View is null!");
                return;
            }

            var monoBehaviourHelper = new GameObject("[MONOBEHAVIOUR_HELPER]").AddComponent<MonoBehaviourHelper>();

            if(_loadingCoroutine != null)
                monoBehaviourHelper.StopCoroutine(_loadingCoroutine);

            _loadingCoroutine = monoBehaviourHelper.StartCoroutine(LoadMainSceneCoroutine(loadingScreenView));
        }

        private IEnumerator LoadMainSceneCoroutine(UILoadingView loadingScreenView)
        {
            Debug.Log($"[Game Bootstrap] Loading coroutine started");

            loadingScreenView.ResetProgress();

            float loadingDuration = 5f;
            float elapsedTime = 0f;

            SceneManager.LoadSceneAsync(GameConstants.MAIN_MENU);

            while (elapsedTime < loadingDuration)
            {
                elapsedTime += Time.deltaTime;
                loadingScreenView.SetLoadingProgress(Mathf.Clamp01(elapsedTime / loadingDuration));
                yield return null;
            }

            loadingScreenView.SetLoadingProgress(1f);

            _loadingCoroutine = null;
            loadingScreenView.ResetProgress();
            _analyticsService.ReportGameStart();

            Debug.Log($"[Game Bootstrap] Loading coroutine finished");
        }
    }

    public class MonoBehaviourHelper : MonoBehaviour 
    {
        private void Awake() => DontDestroyOnLoad(gameObject);

        

        private void OnApplicationQuit() => GameServices.SaveAll();
    }
}