using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Định nghĩa một bước trong pipeline. Dùng để cấu hình pipeline linh hoạt.
    /// </summary>
    public struct PipelineStepDefinition
    {
        public LoadingStep Step;
        public Func<UniTask> Task;
        public float EndProgress;

        public PipelineStepDefinition(LoadingStep step, Func<UniTask> task, float endProgress)
        {
            Step = step;
            Task = task;
            EndProgress = Mathf.Clamp01(endProgress);
        }
    }
}
