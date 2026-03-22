using UnityEngine.Events;

namespace Jis.LoadingSystems
{
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
