using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    public interface ILoadingStep
    {
        float Weight { get; }
        UniTask Execute(LoadingContext context);
    }
}
