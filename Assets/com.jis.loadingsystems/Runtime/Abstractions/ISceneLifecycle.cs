using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    public interface ISceneLifecycle
    {
        UniTask OnSceneLoaded(object payload);
    }
}
