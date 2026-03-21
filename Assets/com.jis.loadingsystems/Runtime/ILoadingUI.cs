using UnityEngine.Events;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Interface cho Loading UI. Implement trong project để tích hợp với UI framework của bạn.
    /// Dùng <see cref="StubLoadingUI"/> làm base nếu chỉ cần override một vài method.
    /// </summary>
    public interface ILoadingUI
    {
        void ShowLoadingUI();
        void CloseLoadingUI();
        void UpdateLoadingBar(float progress);
        void SetLoadingText(int percent);
        void SetStep(string step);
        void OnChangeScene(float delay, UnityAction action = null);
    }
}
