using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Subscribes to <see cref="LoadingEvents"/> and updates an <see cref="ILoadingUI"/> implementation.
    /// Keeps loading logic decoupled from concrete UI. See Documentation/TAI_LIEU_API.md.
    /// </summary>
    public sealed class LoadingUIPresenter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour loadingUIRaw;

        private ILoadingUI _loadingUI;
        private ILoadingUI LoadingUI => _loadingUI ??= loadingUIRaw as ILoadingUI;

        private void OnEnable()
        {
            LoadingEvents.OnStart += HandleStart;
            LoadingEvents.OnProgress += HandleProgress;
            LoadingEvents.OnComplete += HandleComplete;
        }

        private void OnDisable()
        {
            LoadingEvents.OnStart -= HandleStart;
            LoadingEvents.OnProgress -= HandleProgress;
            LoadingEvents.OnComplete -= HandleComplete;
        }

        public void SetLoadingUI(ILoadingUI ui)
        {
            _loadingUI = ui;
            loadingUIRaw = ui as MonoBehaviour;
        }

        private void HandleStart()
        {
            LoadingUI?.ShowLoadingUI();
            HandleProgress(0f);
        }

        private void HandleProgress(float progress)
        {
            if (LoadingUI == null) return;
            LoadingUI.UpdateLoadingBar(progress);
            LoadingUI.SetLoadingText(Mathf.FloorToInt(progress * 100f));
        }

        private void HandleComplete()
        {
            LoadingUI?.CloseLoadingUI();
        }
    }
}
