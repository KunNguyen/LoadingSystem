using System;
using UnityEngine.Events;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Interface cho Loading UI. Implement trong project để tích hợp với UI framework của bạn.
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
