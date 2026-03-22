namespace Jis.LoadingSystems
{
    /// <summary>
    /// Metadata for a pipeline step when raising step lifecycle events.
    /// </summary>
    public readonly struct LoadingStepInfo
    {
        public LoadingStepInfo(int index, int total, string stepTypeName)
        {
            Index = index;
            Total = total;
            StepTypeName = stepTypeName ?? string.Empty;
        }

        /// <summary>0-based index in the current pipeline.</summary>
        public int Index { get; }

        /// <summary>Total number of steps in this run.</summary>
        public int Total { get; }

        /// <summary><see cref="ILoadingStep"/> concrete type name (for logging / UI).</summary>
        public string StepTypeName { get; }
    }
}
