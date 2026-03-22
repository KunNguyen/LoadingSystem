using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    public class SceneFlowManager : LoadingManager
    {
        public static SceneFlowManager Instance { get; private set; }

        [Header("UI (Presentation Layer)")]
        [SerializeField] private MonoBehaviour loadingUIRaw;
        [SerializeField] private LoadingUIPresenter loadingUIPresenter;

        [Header("Pipeline Scenes")]
        [SerializeField] private string initSdkScene = "InitSdkScene";
        [SerializeField] private string controllerScene = "ControllerScene";

        [Header("Scene Loading")]
        [SerializeField] private LoadSceneMode controllerSceneMode = LoadSceneMode.Single;
        [SerializeField] private bool manualSceneActivation = true;
        [SerializeField] [Min(0f)] private float activationDelaySeconds = 0.15f;
        [SerializeField] [Min(0f)] private float fakeDelaySeconds = 0.2f;

        private readonly SceneCancellationManager _cancellation = new();

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
            TryBindPresenter();
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

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

        public async UniTask StartGame(StartGameOptions options)
        {
            var ctx = new LoadingContext
            {
                IsReload = options.Reload,
                FromLogin = options.FromLogin,
                Payload = new ScenePayload
                {
                    IsReload = options.Reload,
                    FromLogin = options.FromLogin
                }
            };

            var pipeline = BuildPipeline(ctx, options);
            await RunPipeline(pipeline.Steps, ctx);
        }

        protected virtual LoadingPipeline BuildPipeline(LoadingContext context, StartGameOptions options)
        {
            var pipeline = new LoadingPipeline();

            if (!options.FromLogin)
            {
                pipeline
                    .AddStep(new InitSDKStep(weight: 0.2f))
                    .AddStep(new LoadSceneStep(
                        initSdkScene,
                        mode: LoadSceneMode.Single,
                        manualActivation: manualSceneActivation,
                        activationDelaySeconds: activationDelaySeconds,
                        weight: 0.2f))
                    .AddStep(new DelegateStep(async ctx =>
                    {
                        if (options.OnLoadLocalDataAsync != null)
                            await options.OnLoadLocalDataAsync();

                        ctx.Set("local_data_loaded", true);
                    }, weight: 0.2f))
                    .AddStep(new DelegateStep(async ctx =>
                    {
                        if (options.OnSyncCloudDataAsync == null)
                        {
                            ctx.CloudDataAvailable = false;
                            return;
                        }

                        await options.OnSyncCloudDataAsync();
                        ctx.CloudDataAvailable = true;
                    }, weight: 0.2f));
            }

            pipeline
                .AddStep(new DelayStep(fakeDelaySeconds, weight: 0.05f))
                .AddStep(new LoadSceneStep(
                    controllerScene,
                    mode: controllerSceneMode,
                    manualActivation: manualSceneActivation,
                    activationDelaySeconds: activationDelaySeconds,
                    weight: 0.35f))
                .AddStep(new PostInitStep(controllerScene, weight: 0.05f));

            return pipeline;
        }

        public async UniTask LoadControllerScene(object payload = null)
        {
            var context = new LoadingContext { Payload = payload };
            var pipeline = new LoadingPipeline()
                .AddStep(new LoadSceneStep(controllerScene, controllerSceneMode, manualSceneActivation, activationDelaySeconds, 0.9f))
                .AddStep(new PostInitStep(controllerScene, 0.1f));

            await RunPipeline(pipeline.Steps, context);
        }

        public async UniTask LoadSceneByName(
            string sceneName,
            object payload = null,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool useManualActivation = true,
            float activateDelay = 0f)
        {
            _cancellation.RegisterSceneToken(sceneName, mode == LoadSceneMode.Single);

            var context = new LoadingContext
            {
                Payload = payload,
                CancellationToken = _cancellation.GetSceneToken(sceneName)
            };

            var pipeline = new LoadingPipeline()
                .AddStep(new LoadSceneStep(sceneName, mode, useManualActivation, activateDelay, 0.9f))
                .AddStep(new PostInitStep(sceneName, 0.1f));

            await RunPipeline(pipeline.Steps, context);
        }

        public UniTask LoadSceneByName(
            string sceneName,
            object payload,
            LoadSceneMode mode,
            float startProgress,
            float endProgress)
        {
            return LoadSceneByName(sceneName, payload, mode, useManualActivation: true, activateDelay: 0f);
        }

        public CancellationToken GetSceneCancellationToken(string sceneName) => _cancellation.GetSceneToken(sceneName);
        public CancellationToken GetCurrentSceneCancellationToken() => _cancellation.GetCurrentSceneToken();

        public void SetLoadingUI(ILoadingUI ui)
        {
            if (loadingUIPresenter == null)
            {
                loadingUIPresenter = GetComponent<LoadingUIPresenter>();
                if (loadingUIPresenter == null)
                    loadingUIPresenter = gameObject.AddComponent<LoadingUIPresenter>();
            }

            loadingUIPresenter.SetLoadingUI(ui);
        }

        private void TryBindPresenter()
        {
            if (loadingUIPresenter == null)
                loadingUIPresenter = GetComponent<LoadingUIPresenter>();

            if (loadingUIPresenter == null)
                loadingUIPresenter = gameObject.AddComponent<LoadingUIPresenter>();

            if (loadingUIRaw is ILoadingUI ui)
            {
                loadingUIPresenter.SetLoadingUI(ui);
            }
        }
    }
}
