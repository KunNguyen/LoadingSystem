using System;
using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Pipeline boot: Boot → InitSDK → CheckAuth → LoadLocal → SyncCloud → EnterGame.
    /// onLoadLocalDataAsync, onSyncCloudDataAsync optional (null = no-op).
    /// </summary>
    public class LoadingPipeline
    {
        private readonly SceneFlowManager _flow;
        private readonly LoadingContext _context;
        private readonly Func<UniTask> _onLoadLocalData;
        private readonly Func<UniTask> _onSyncCloudData;

        public LoadingPipeline(
            SceneFlowManager flow,
            LoadingContext context,
            Func<UniTask> onLoadLocalDataAsync = null,
            Func<UniTask> onSyncCloudDataAsync = null)
        {
            _flow = flow;
            _context = context;
            _onLoadLocalData = onLoadLocalDataAsync ?? (() => UniTask.CompletedTask);
            _onSyncCloudData = onSyncCloudDataAsync ?? (() => UniTask.CompletedTask);
        }

        public async UniTask Execute()
        {
            if (_context.FromLogin)
            {
                await Step(LoadingStep.EnterGame,
                    () => _flow.LoadControllerScene(new ScenePayload { IsReload = _context.IsReload, FromLogin = _context.FromLogin }, 0f, 1f), 1f);
                _flow.HideLoading();
                return;
            }

            await Step(LoadingStep.Boot, () => _flow.LoadStartScene(0f, 0.1f), 0.1f);
            await Step(LoadingStep.InitSDK, () => _flow.LoadInitSdkScene(0.1f, 0.3f), 0.3f);
            await Step(LoadingStep.CheckAuth, CheckAuthState, 0.4f);
            await Step(LoadingStep.LoadLocalData, _onLoadLocalData, 0.5f);

            if (_context.IsLoggedIn)
                await Step(LoadingStep.LoadCloudData, SyncCloudData, 0.7f);
            else
                _flow.UpdateLoadingBar(0.7f);

            await Step(LoadingStep.EnterGame,
                () => _flow.LoadControllerScene(new ScenePayload { IsReload = _context.IsReload, FromLogin = _context.FromLogin }, 0.7f, 1f), 1f);

            _flow.HideLoading();
        }

        private async UniTask Step(LoadingStep step, Func<UniTask> task, float targetProgress)
        {
            _flow.SetLoadingStep(step);
            await task();
            _flow.UpdateLoadingBar(targetProgress);
        }

        /// <summary>Override trong project để check auth (Firebase, etc.).</summary>
        protected virtual UniTask CheckAuthState()
        {
            Context.IsLoggedIn = false;
            return UniTask.CompletedTask;
        }

        protected LoadingContext Context => _context;

        private async UniTask SyncCloudData()
        {
            try
            {
                await _onSyncCloudData();
                _context.CloudDataAvailable = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[LoadingSystems] CloudSync error: {e.Message}");
                _context.CloudDataAvailable = false;
            }
        }
    }
}
