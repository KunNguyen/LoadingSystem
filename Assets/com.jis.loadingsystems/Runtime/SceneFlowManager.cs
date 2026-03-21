using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Quản lý flow load scene, lifecycle và cancellation.
    /// Thêm vào GameObject trong bootstrap scene, gán ILoadingUI qua Inspector hoặc code.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        [Header("Loading UI")]
        [SerializeField] private MonoBehaviour loadingUIRaw;

        private ILoadingUI _loadingUI;
        private ILoadingUI LoadingUI => _loadingUI ??= loadingUIRaw as ILoadingUI;

        [Header("Scene Names")]
        [SerializeField] private string initSdkScene = "InitSdkScene";
        [SerializeField] private string controllerScene = "ControllerScene";

        [Header("Timing")]
        [Tooltip("Delay trước khi bắt đầu load scene (giây). Giảm xuống 0 để tắt.")]
        [SerializeField] [Min(0f)] private float sceneLoadDelay = 0.25f;
        [Tooltip("Timeout chờ LoadingUI (giây). 0 = chờ vô hạn.")]
        [SerializeField] [Min(0f)] private float loadingUIWaitTimeout = 5f;

        private bool _sdkInitialized;
        private readonly SceneCancellationManager _cancellation = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Entry point: App launch, Login, Logout, Switch account.</summary>
        public async UniTask StartGame(
            bool reload = false,
            bool fromLogin = false,
            Func<UniTask> onLoadLocalDataAsync = null,
            Func<UniTask> onSyncCloudDataAsync = null)
        {
            await StartGame(new StartGameOptions
            {
                Reload = reload,
                FromLogin = fromLogin,
                OnLoadLocalDataAsync = onLoadLocalDataAsync,
                OnSyncCloudDataAsync = onSyncCloudDataAsync
            });
        }

        /// <summary>Entry point với options struct. Ví dụ: StartGame(StartGameOptions.WhenFromLogin())</summary>
        public async UniTask StartGame(StartGameOptions options)
        {
            ShowLoading().Forget();
            var ctx = new LoadingContext
            {
                IsReload = options.Reload,
                FromLogin = options.FromLogin
            };
            var pipeline = CreatePipeline(ctx, options.OnLoadLocalDataAsync, options.OnSyncCloudDataAsync);
            await pipeline.Execute();
        }

        /// <summary>Override để dùng custom pipeline (ví dụ tích hợp Firebase Auth).</summary>
        protected virtual LoadingPipeline CreatePipeline(LoadingContext ctx, Func<UniTask> onLoad, Func<UniTask> onSync)
            => new LoadingPipeline(this, ctx, onLoad, onSync);

        /// <summary>Bước Boot (no-op). Override hoặc tạo pipeline riêng nếu cần load scene boot.</summary>
        public virtual UniTask LoadStartScene(float startProgress = 0f, float endProgress = 0.1f)
        {
            UpdateLoadingBar(endProgress);
            return UniTask.CompletedTask;
        }

        public async UniTask LoadInitSdkScene(float startProgress = 0.1f, float endProgress = 0.3f)
        {
            if (_sdkInitialized) { UpdateLoadingBar(endProgress); return; }
            await LoadScene(initSdkScene, null, LoadSceneMode.Single, startProgress, endProgress);
            _sdkInitialized = true;
        }

        public async UniTask LoadControllerScene(object payload = null, float startProgress = 0.7f, float endProgress = 1f)
        {
            await LoadScene(controllerScene, payload, LoadSceneMode.Single, startProgress, endProgress);
        }

        public async UniTask LoadSceneByName(string sceneName, object payload = null, LoadSceneMode mode = LoadSceneMode.Single, float startProgress = 0f, float endProgress = 1f)
        {
            await LoadScene(sceneName, payload, mode, startProgress, endProgress);
        }

        public CancellationToken GetSceneCancellationToken(string sceneName) => _cancellation.GetSceneToken(sceneName);
        public CancellationToken GetCurrentSceneCancellationToken() => _cancellation.GetCurrentSceneToken();

        public void SetLoadingUI(ILoadingUI ui) => _loadingUI = ui;

        private async UniTask LoadScene(string sceneName, object payload, LoadSceneMode mode, float startProgress, float endProgress)
        {
            _cancellation.RegisterSceneToken(sceneName, mode == LoadSceneMode.Single);
            LoadingUI?.OnChangeScene(0.15f, null);
            if (sceneLoadDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(sceneLoadDelay));

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            while (op != null && !op.isDone)
            {
                var p = Mathf.Lerp(startProgress, endProgress, Mathf.Clamp01(op.progress / 0.9f));
                UpdateLoadingBar(p);
                await UniTask.Yield();
            }
            UpdateLoadingBar(endProgress);
            await UniTask.NextFrame();
            await NotifySceneLoaded(sceneName, payload);
        }

        private async UniTask NotifySceneLoaded(string sceneName, object payload)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var lc in root.GetComponentsInChildren<ISceneLifecycle>(true))
                {
                    try
                    {
                        await lc.OnSceneLoaded(payload);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[LoadingSystem] OnSceneLoaded error ({lc.GetType().Name}): {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }

        public void SetLoadingStep(LoadingStep step) => LoadingUI?.SetStep(step.ToString());

        private async UniTask ShowLoading()
        {
            var startTime = Time.realtimeSinceStartup;
            while (LoadingUI == null)
            {
                if (loadingUIWaitTimeout > 0f && (Time.realtimeSinceStartup - startTime) >= loadingUIWaitTimeout)
                {
                    Debug.LogWarning("[LoadingSystem] LoadingUI chưa gán sau " + loadingUIWaitTimeout + "s. Dùng StubLoadingUI hoặc gán vào Inspector. Tiếp tục mà không có UI.");
                    break;
                }
                await UniTask.Yield();
            }
            LoadingUI?.ShowLoadingUI();
            UpdateLoadingBar(0f);
        }

        public void UpdateLoadingBar(float progress)
        {
            LoadingUI?.UpdateLoadingBar(progress);
            LoadingUI?.SetLoadingText(Mathf.FloorToInt(progress * 100f));
        }

        public void HideLoading() => LoadingUI?.CloseLoadingUI();
    }
}
