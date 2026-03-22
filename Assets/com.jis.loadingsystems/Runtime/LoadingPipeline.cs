using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    public sealed class LoadingPipeline
    {
        private readonly List<ILoadingStep> _steps = new();

        public LoadingPipeline AddStep(ILoadingStep step)
        {
            _steps.Add(step);
            return this;
        }

        public IReadOnlyList<ILoadingStep> Steps => _steps;
    }
}
