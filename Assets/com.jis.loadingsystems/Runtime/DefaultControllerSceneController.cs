using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Controller mặc định cho ControllerScene. Cấu hình scene cần load qua Inspector.
    /// Thay bằng script custom nếu cần logic phức tạp.
    /// </summary>
    public class DefaultControllerSceneController : MonoBehaviour, ISceneLifecycle
    {
        [Tooltip("Scene load sau ControllerScene (Additive). Để trống nếu load bằng code.")]
        [SerializeField] private string sceneToLoad = "GameplayScene";

        public async UniTask OnSceneLoaded(object payload)
        {
            if (string.IsNullOrEmpty(sceneToLoad)) return;
            await SceneFlowManager.Instance.LoadSceneByName(sceneToLoad, null, LoadSceneMode.Additive);
        }
    }
}
