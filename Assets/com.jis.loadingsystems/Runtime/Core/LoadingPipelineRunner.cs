using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    public sealed class LoadingPipelineRunner
    {
        private readonly ProgressSmoother _smoother;
        private float _realProgress;
        private bool _pipelineFinished;

        public LoadingPipelineRunner(float smoothingSpeed)
        {
            _smoother = new ProgressSmoother(smoothingSpeed);
        }

        public async UniTask Run(IReadOnlyList<ILoadingStep> steps, LoadingContext context)
        {
            if (steps == null || steps.Count == 0)
            {
                LoadingEvents.RaiseStart();
                LoadingEvents.RaiseProgress(1f);
                LoadingEvents.RaiseComplete();
                return;
            }

            _realProgress = 0f;
            _pipelineFinished = false;

            LoadingEvents.RaiseStart();
            var progressPump = PumpDisplayedProgress(context);

            try
            {
                var totalWeight = CalculateTotalWeight(steps);
                var accumulatedWeight = 0f;

                foreach (var step in steps)
                {
                    var stepWeight = Mathf.Max(step.Weight, 0.0001f);
                    var stepBase = accumulatedWeight / totalWeight;
                    var stepRange = stepWeight / totalWeight;

                    context.BindStepProgressReporter(p => { _realProgress = stepBase + (Mathf.Clamp01(p) * stepRange); });
                    await step.Execute(context);

                    accumulatedWeight += stepWeight;
                    _realProgress = Mathf.Clamp01(accumulatedWeight / totalWeight);
                }
            }
            finally
            {
                context.BindStepProgressReporter(null);
                _realProgress = 1f;
                _pipelineFinished = true;
                await progressPump;
                LoadingEvents.RaiseComplete();
            }
        }

        private static float CalculateTotalWeight(IReadOnlyList<ILoadingStep> steps)
        {
            var total = 0f;
            foreach (var step in steps)
            {
                total += Mathf.Max(step.Weight, 0.0001f);
            }

            return Mathf.Max(total, 0.0001f);
        }

        private async UniTask PumpDisplayedProgress(LoadingContext context)
        {
            var displayed = 0f;
            LoadingEvents.RaiseProgress(displayed);

            while (!_pipelineFinished || displayed < 0.999f)
            {
                var target = _pipelineFinished ? 1f : _realProgress;
                var deltaTime = Math.Max(Time.unscaledDeltaTime, 0.016f);
                displayed = _smoother.Next(displayed, target, (float)deltaTime);

                if (_pipelineFinished && target >= 1f && displayed > 0.999f)
                {
                    displayed = 1f;
                }

                LoadingEvents.RaiseProgress(displayed);
                await UniTask.Yield(PlayerLoopTiming.Update, context.CancellationToken);
            }
        }
    }
}
