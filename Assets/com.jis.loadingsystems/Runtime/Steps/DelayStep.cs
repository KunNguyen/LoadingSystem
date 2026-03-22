using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    public sealed class DelayStep : ILoadingStep
    {
        private readonly float _durationSeconds;
        public float Weight { get; }

        public DelayStep(float durationSeconds, float weight = 1f)
        {
            _durationSeconds = Mathf.Max(0f, durationSeconds);
            Weight = Mathf.Max(0.0001f, weight);
        }

        public async UniTask Execute(LoadingContext context)
        {
            if (_durationSeconds <= 0f)
            {
                context.ReportStepProgress(1f);
                return;
            }

            var elapsed = 0f;
            while (elapsed < _durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                context.ReportStepProgress(elapsed / _durationSeconds);
                await UniTask.Yield(PlayerLoopTiming.Update, context.CancellationToken);
            }

            context.ReportStepProgress(1f);
        }
    }
}
