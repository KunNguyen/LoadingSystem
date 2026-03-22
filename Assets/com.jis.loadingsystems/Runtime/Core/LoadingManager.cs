using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance { get; private set; }

        [SerializeField] [Min(0.1f)] private float progressSmoothingSpeed = 8f;

        private LoadingPipelineRunner _runner;
        private CancellationTokenSource _pipelineCts;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _runner = new LoadingPipelineRunner(progressSmoothingSpeed);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CancelCurrentPipeline();
        }

        public async UniTask RunPipeline(IReadOnlyList<ILoadingStep> steps, LoadingContext context)
        {
            CancelCurrentPipeline();
            _pipelineCts = context.CancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken)
                : new CancellationTokenSource();

            context.CancellationToken = _pipelineCts.Token;
            await _runner.Run(steps, context);
        }

        public void CancelCurrentPipeline()
        {
            if (_pipelineCts == null) return;
            _pipelineCts.Cancel();
            _pipelineCts.Dispose();
            _pipelineCts = null;
        }
    }
}
