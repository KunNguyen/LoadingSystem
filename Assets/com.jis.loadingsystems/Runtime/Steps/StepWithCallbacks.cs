using System;
using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Wraps any <see cref="ILoadingStep"/> with optional callbacks when the step starts and when it finishes
    /// (always invoked in <c>finally</c>, including when <see cref="ILoadingStep.Execute"/> throws).
    /// Use this when inserting a step in <see cref="SceneFlowManager.CustomizePipeline"/> instead of global <see cref="LoadingEvents.OnStepStarted"/>.
    /// </summary>
    public sealed class StepWithCallbacks : ILoadingStep
    {
        private readonly ILoadingStep _inner;
        private readonly Action<LoadingContext> _onStarted;
        private readonly Action<LoadingContext> _onCompleted;

        public float Weight => _inner.Weight;

        public StepWithCallbacks(
            ILoadingStep inner,
            Action<LoadingContext> onStarted = null,
            Action<LoadingContext> onCompleted = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onStarted = onStarted;
            _onCompleted = onCompleted;
        }

        public async UniTask Execute(LoadingContext context)
        {
            _onStarted?.Invoke(context);
            try
            {
                await _inner.Execute(context);
            }
            finally
            {
                _onCompleted?.Invoke(context);
            }
        }
    }
}
