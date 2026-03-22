using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    public sealed class LoadSceneStep : ILoadingStep
    {
        private readonly string _sceneName;
        private readonly LoadSceneMode _mode;
        private readonly bool _manualActivation;
        private readonly float _activationDelaySeconds;

        public float Weight { get; }

        public LoadSceneStep(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool manualActivation = true,
            float activationDelaySeconds = 0f,
            float weight = 1f)
        {
            _sceneName = sceneName;
            _mode = mode;
            _manualActivation = manualActivation;
            _activationDelaySeconds = Mathf.Max(0f, activationDelaySeconds);
            Weight = Mathf.Max(0.0001f, weight);
        }

        public async UniTask Execute(LoadingContext context)
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
                throw new InvalidOperationException("Scene name is required for LoadSceneStep.");

            var op = SceneManager.LoadSceneAsync(_sceneName, _mode);
            if (op == null)
                throw new InvalidOperationException($"Could not create AsyncOperation for scene '{_sceneName}'.");

            op.allowSceneActivation = !_manualActivation;

            while (!op.isDone)
            {
                var normalized = Mathf.Clamp01(op.progress / 0.9f);
                if (_manualActivation)
                {
                    context.ReportStepProgress(Mathf.Min(normalized, 0.9f));

                    if (op.progress >= 0.9f && !op.allowSceneActivation)
                    {
                        if (_activationDelaySeconds > 0f)
                            await UniTask.Delay(TimeSpan.FromSeconds(_activationDelaySeconds), cancellationToken: context.CancellationToken);

                        op.allowSceneActivation = true;
                    }
                }
                else
                {
                    context.ReportStepProgress(normalized);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, context.CancellationToken);
            }

            context.Set("last_loaded_scene", _sceneName);
            context.ReportStepProgress(1f);
        }
    }
}
