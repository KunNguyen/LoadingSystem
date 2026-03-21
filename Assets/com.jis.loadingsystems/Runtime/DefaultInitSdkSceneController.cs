using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Controller mặc định cho InitSdkScene. Override hoặc thay bằng script của bạn.
    /// </summary>
    public class DefaultInitSdkSceneController : MonoBehaviour, ISceneLifecycle
    {
        public async UniTask OnSceneLoaded(object payload)
        {
            // TODO: Init SDK (Ads, Firebase, IAP...)
            await UniTask.CompletedTask;
        }
    }
}
