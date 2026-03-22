using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    public sealed class DelegateStep : ILoadingStep
    {
        private readonly Func<LoadingContext, UniTask> _execute;
        public float Weight { get; }

        public DelegateStep(Func<LoadingContext, UniTask> execute, float weight = 1f)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            Weight = Mathf.Max(0.0001f, weight);
        }

        public async UniTask Execute(LoadingContext context)
        {
            await _execute(context);
            context.ReportStepProgress(1f);
        }
    }
}
