using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Pipeline boot với steps có thể cấu hình. Override GetSteps() để tùy chỉnh theo từng game.
    /// Mặc định: Boot → InitSDK → CheckAuth → LoadLocal → SyncCloud (nếu login) → EnterGame.
    /// </summary>
    public class LoadingPipeline
    {
        protected readonly SceneFlowManager Flow;
        protected readonly LoadingContext Context;
        private readonly Func<UniTask> _onLoadLocalData;
        private readonly Func<UniTask> _onSyncCloudData;

        public LoadingPipeline(
            SceneFlowManager flow,
            LoadingContext context,
            Func<UniTask> onLoadLocalDataAsync = null,
            Func<UniTask> onSyncCloudDataAsync = null)
        {
            Flow = flow;
            Context = context;
            _onLoadLocalData = onLoadLocalDataAsync ?? (() => UniTask.CompletedTask);
            _onSyncCloudData = onSyncCloudDataAsync ?? (() => UniTask.CompletedTask);
        }

        public async UniTask Execute()
        {
            var steps = GetSteps();
            foreach (var def in steps)
            {
                await RunStep(def);
            }
            Flow.HideLoading();
        }

        /// <summary>
        /// Override để tùy chỉnh steps theo từng game. Thêm, bớt, đổi thứ tự steps.
        /// </summary>
        protected virtual List<PipelineStepDefinition> GetSteps()
        {
            if (Context.FromLogin)
            {
                return new List<PipelineStepDefinition>
                {
                    new(LoadingStep.EnterGame,
                        () => Flow.LoadControllerScene(
                            new ScenePayload { IsReload = Context.IsReload, FromLogin = Context.FromLogin }, 0f, 1f),
                        1f)
                };
            }

            var steps = new List<PipelineStepDefinition>
            {
                new(LoadingStep.Boot, () => Flow.LoadStartScene(0f, 0.1f), 0.1f),
                new(LoadingStep.InitSDK, () => Flow.LoadInitSdkScene(0.1f, 0.3f), 0.3f),
                new(LoadingStep.CheckAuth, CheckAuthState, 0.4f),
                new(LoadingStep.LoadLocalData, _onLoadLocalData, 0.5f)
            };

            if (Context.IsLoggedIn)
                steps.Add(new PipelineStepDefinition(LoadingStep.LoadCloudData, SyncCloudData, 0.7f));
            else
                Flow.UpdateLoadingBar(0.7f);

            steps.Add(new PipelineStepDefinition(LoadingStep.EnterGame,
                () => Flow.LoadControllerScene(
                    new ScenePayload { IsReload = Context.IsReload, FromLogin = Context.FromLogin }, 0.7f, 1f),
                1f));

            return steps;
        }

        /// <summary>Chạy một step. Protected để subclass gọi khi thêm logic tùy chỉnh.</summary>
        protected async UniTask RunStep(PipelineStepDefinition def)
        {
            Flow.SetLoadingStep(def.Step);
            await def.Task();
            Flow.UpdateLoadingBar(def.EndProgress);
        }

        /// <summary>Override trong project để check auth (Firebase, etc.).</summary>
        protected virtual UniTask CheckAuthState()
        {
            Context.IsLoggedIn = false;
            return UniTask.CompletedTask;
        }

        protected virtual async UniTask SyncCloudData()
        {
            try
            {
                await _onSyncCloudData();
                Context.CloudDataAvailable = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadingSystems] CloudSync error: {e.Message}");
                Context.CloudDataAvailable = false;
            }
        }
    }
}
