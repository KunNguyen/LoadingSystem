using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Gọi StartGame khi BootstrapScene load. Dùng trong scene có SceneFlowManager.
    /// </summary>
    public class BootstrapController : MonoBehaviour
    {
        private void Start()
        {
            SceneFlowManager.Instance.StartGame().Forget();
        }
    }
}
