using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Shared runtime context for all loading steps.
    /// </summary>
    public sealed class LoadingContext
    {
        private readonly Dictionary<string, object> _data = new();
        private Action<float> _stepProgressReporter;

        public bool IsLoggedIn;
        public bool IsReload;
        public bool CloudDataAvailable;
        public bool FromLogin;
        public object Payload;
        public CancellationToken CancellationToken;

        public void Set<T>(string key, T value) => _data[key] = value;

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var raw) && raw is T casted)
            {
                value = casted;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Report progress inside current step. Range: 0..1.
        /// </summary>
        public void ReportStepProgress(float progress)
        {
            _stepProgressReporter?.Invoke(Mathf.Clamp01(progress));
        }

        internal void BindStepProgressReporter(Action<float> reporter) => _stepProgressReporter = reporter;
    }
}
