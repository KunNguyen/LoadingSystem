using System;

namespace Jis.LoadingSystems
{
    public static class LoadingEvents
    {
        public static Action OnStart;
        public static Action<float> OnProgress;
        public static Action OnComplete;

        /// <summary>Fired immediately before <see cref="ILoadingStep.Execute"/> for each step.</summary>
        public static Action<LoadingStepInfo> OnStepStarted;

        /// <summary>Fired after <see cref="ILoadingStep.Execute"/> returns, or in <c>finally</c> if the step throws.</summary>
        public static Action<LoadingStepInfo> OnStepCompleted;

        internal static void RaiseStart() => OnStart?.Invoke();
        internal static void RaiseProgress(float progress) => OnProgress?.Invoke(progress);
        internal static void RaiseComplete() => OnComplete?.Invoke();

        internal static void RaiseStepStarted(LoadingStepInfo info) => OnStepStarted?.Invoke(info);
        internal static void RaiseStepCompleted(LoadingStepInfo info) => OnStepCompleted?.Invoke(info);
    }
}
