using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    public sealed class InitSDKStep : ILoadingStep
    {
        public float Weight { get; }
        private readonly float _simulatedDurationSeconds;

        public InitSDKStep(float weight = 1f, float simulatedDurationSeconds = 0.2f)
        {
            Weight = Mathf.Max(0.0001f, weight);
            _simulatedDurationSeconds = Mathf.Max(0f, simulatedDurationSeconds);
        }

        public async UniTask Execute(LoadingContext context)
        {
            // Mock initialization hook for Ads/IAP/Tracking SDKs.
            if (_simulatedDurationSeconds > 0f)
            {
                var elapsed = 0f;
                while (elapsed < _simulatedDurationSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    context.ReportStepProgress(elapsed / _simulatedDurationSeconds);
                    await UniTask.Yield(PlayerLoopTiming.Update, context.CancellationToken);
                }
            }

            context.Set("sdk_initialized", true);
            context.ReportStepProgress(1f);
        }
    }
}
