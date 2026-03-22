using System;

namespace Jis.LoadingSystems
{
    public static class LoadingEvents
    {
        public static Action OnStart;
        public static Action<float> OnProgress;
        public static Action OnComplete;

        internal static void RaiseStart() => OnStart?.Invoke();
        internal static void RaiseProgress(float progress) => OnProgress?.Invoke(progress);
        internal static void RaiseComplete() => OnComplete?.Invoke();
    }
}
