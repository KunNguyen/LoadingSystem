using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Lifecycle callback khi scene được load xong.
    /// Implement trên MonoBehaviour trong scene để nhận payload và khởi tạo.
    /// </summary>
    public interface ISceneLifecycle
    {
        UniTask OnSceneLoaded(object payload);
    }
}
