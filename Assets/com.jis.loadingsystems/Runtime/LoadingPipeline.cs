using System;
using System.Collections.Generic;

namespace Jis.LoadingSystems
{
    public sealed class LoadingPipeline
    {
        private sealed class PipelineItem
        {
            public string Key;
            public ILoadingStep Step;
        }

        private readonly List<PipelineItem> _items = new();

        public LoadingPipeline AddStep(ILoadingStep step, string key = null)
        {
            ValidateStep(step);
            ValidateUniqueKey(key);
            _items.Add(new PipelineItem
            {
                Key = key,
                Step = step
            });
            return this;
        }

        public LoadingPipeline InsertStep(int index, ILoadingStep step, string key = null)
        {
            ValidateStep(step);
            ValidateUniqueKey(key);

            if (index < 0 || index > _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of pipeline range.");

            _items.Insert(index, new PipelineItem
            {
                Key = key,
                Step = step
            });
            return this;
        }

        public LoadingPipeline InsertBefore(string anchorKey, ILoadingStep step, string key = null)
        {
            var index = FindIndexByKey(anchorKey);
            if (index < 0)
                throw new InvalidOperationException($"Anchor step key '{anchorKey}' was not found.");

            return InsertStep(index, step, key);
        }

        public LoadingPipeline InsertAfter(string anchorKey, ILoadingStep step, string key = null)
        {
            var index = FindIndexByKey(anchorKey);
            if (index < 0)
                throw new InvalidOperationException($"Anchor step key '{anchorKey}' was not found.");

            return InsertStep(index + 1, step, key);
        }

        public LoadingPipeline ReplaceStep(string key, ILoadingStep newStep, string newKey = null)
        {
            ValidateStep(newStep);
            var index = FindIndexByKey(key);
            if (index < 0)
                throw new InvalidOperationException($"Step key '{key}' was not found.");

            var finalKey = string.IsNullOrWhiteSpace(newKey) ? key : newKey;
            if (!string.Equals(finalKey, key, StringComparison.Ordinal))
                ValidateUniqueKey(finalKey);

            _items[index] = new PipelineItem
            {
                Key = finalKey,
                Step = newStep
            };
            return this;
        }

        public LoadingPipeline RemoveStep(string key)
        {
            var index = FindIndexByKey(key);
            if (index >= 0)
                _items.RemoveAt(index);
            return this;
        }

        public bool ContainsStep(string key) => FindIndexByKey(key) >= 0;

        public IReadOnlyList<ILoadingStep> Steps
        {
            get
            {
                var steps = new List<ILoadingStep>(_items.Count);
                for (var i = 0; i < _items.Count; i++)
                    steps.Add(_items[i].Step);
                return steps;
            }
        }

        private int FindIndexByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return -1;
            for (var i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void ValidateUniqueKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (ContainsStep(key))
                throw new InvalidOperationException($"Step key '{key}' already exists.");
        }

        private static void ValidateStep(ILoadingStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
        }
    }
}
