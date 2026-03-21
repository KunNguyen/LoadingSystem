using UnityEngine;
using UnityEngine.Events;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Base class implement ILoadingUI với default no-op. Kế thừa và override chỉ method cần dùng.
    /// Dùng trực tiếp để test flow (chỉ show/hide GameObject) khi chưa có UI thật.
    /// </summary>
    public class StubLoadingUI : MonoBehaviour, ILoadingUI
    {
        [SerializeField] [Tooltip("Nếu null, dùng chính gameObject này")]
        private GameObject targetToShowHide;

        private GameObject Target => targetToShowHide != null ? targetToShowHide : gameObject;

        public virtual void ShowLoadingUI() => Target.SetActive(true);
        public virtual void CloseLoadingUI() => Target.SetActive(false);
        public virtual void UpdateLoadingBar(float progress) { }
        public virtual void SetLoadingText(int percent) { }
        public virtual void SetStep(string step) { }
        public virtual void OnChangeScene(float delay, UnityAction action = null) => action?.Invoke();
    }
}
