using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// One unit of work in the loading pipeline. See Documentation/TAI_LIEU_API.md for <see cref="Weight"/> semantics.
    /// </summary>
    public interface ILoadingStep
    {
        /// <summary>
        /// Relative share of the global 0→1 progress bar for this step: segment length = Weight / sum(all step weights).
        /// Use <see cref="LoadingContext.ReportStepProgress"/> for fine-grained progress inside the step (0..1).
        /// </summary>
        float Weight { get; }

        UniTask Execute(LoadingContext context);
    }
}
