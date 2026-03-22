using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Ready-to-use example for pipeline customization via CustomizePipeline().
    /// Replace SceneFlowManager in BootstrapRoot with this component to test quickly.
    /// </summary>
    public class MySceneFlowManagerExample : SceneFlowManager
    {
        [Header("Example Options")]
        [SerializeField] private bool disableCloudSyncInDebug = true;
        [SerializeField] [Min(0f)] private float fakeDelayOverrideSeconds = 0.6f;

        protected override void CustomizePipeline(
            LoadingPipeline pipeline,
            LoadingContext context,
            StartGameOptions options)
        {
            pipeline.InsertBefore(
                StepKeyLoadLocalData,
                new FetchRemoteConfigStepExample(),
                key: "fetch-remote-config");

            pipeline.ReplaceStep(
                StepKeyFakeDelay,
                new DelayStep(fakeDelayOverrideSeconds, 0.08f));

            if (disableCloudSyncInDebug && Debug.isDebugBuild)
                pipeline.RemoveStep(StepKeySyncCloudData);

            pipeline.InsertAfter(
                StepKeyLoadControllerScene,
                new PostWarmupStepExample(),
                key: "post-warmup");
        }
    }

    public sealed class FetchRemoteConfigStepExample : ILoadingStep
    {
        public float Weight => 0.15f;

        public async UniTask Execute(LoadingContext context)
        {
            for (var i = 0; i <= 10; i++)
            {
                context.ReportStepProgress(i / 10f);
                await UniTask.DelayFrame(1, cancellationToken: context.CancellationToken);
            }

            context.Set("remote_config_ready", true);
        }
    }

    public sealed class PostWarmupStepExample : ILoadingStep
    {
        public float Weight => 0.05f;

        public async UniTask Execute(LoadingContext context)
        {
            // Example post-load warmup hook.
            await UniTask.Delay(100, cancellationToken: context.CancellationToken);
            context.Set("post_warmup_done", true);
            context.ReportStepProgress(1f);
        }
    }
}
