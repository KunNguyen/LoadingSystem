using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    public sealed class PostInitStep : ILoadingStep
    {
        private readonly string _sceneName;
        public float Weight { get; }

        public PostInitStep(string sceneName, float weight = 1f)
        {
            _sceneName = sceneName;
            Weight = Mathf.Max(0.0001f, weight);
        }

        public async UniTask Execute(LoadingContext context)
        {
            var targetSceneName = _sceneName;
            if (string.IsNullOrWhiteSpace(targetSceneName) && context.TryGet<string>("last_loaded_scene", out var loaded))
            {
                targetSceneName = loaded;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                context.ReportStepProgress(1f);
                return;
            }

            var scene = SceneManager.GetSceneByName(targetSceneName);
            if (!scene.IsValid())
            {
                context.ReportStepProgress(1f);
                return;
            }

            var roots = scene.GetRootGameObjects();
            var total = Math.Max(roots.Length, 1);

            for (var i = 0; i < roots.Length; i++)
            {
                foreach (var lifecycle in roots[i].GetComponentsInChildren<ISceneLifecycle>(true))
                {
                    await lifecycle.OnSceneLoaded(context.Payload);
                }

                context.ReportStepProgress((i + 1f) / total);
                await UniTask.Yield(PlayerLoopTiming.Update, context.CancellationToken);
            }

            context.ReportStepProgress(1f);
        }
    }
}
