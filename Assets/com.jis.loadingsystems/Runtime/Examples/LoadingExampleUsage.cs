using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Example entry point demonstrating SDK-ready loading pipeline usage.
    /// </summary>
    public sealed class LoadingExampleUsage : MonoBehaviour
    {
        [SerializeField] private bool fromLogin;
        [SerializeField] private bool reload;

        private void Start()
        {
            Run().Forget();
        }

        private async UniTaskVoid Run()
        {
            await SceneFlowManager.Instance.StartGame(new StartGameOptions
            {
                FromLogin = fromLogin,
                Reload = reload,
                OnLoadLocalDataAsync = MockLoadLocalDataAsync,
                OnSyncCloudDataAsync = MockSyncCloudDataAsync
            });
        }

        private static async UniTask MockLoadLocalDataAsync()
        {
            await UniTask.Delay(200);
        }

        private static async UniTask MockSyncCloudDataAsync()
        {
            await UniTask.Delay(300);
        }
    }
}
