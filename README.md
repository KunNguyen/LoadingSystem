# LoadingSystem

Unity package cho scene loading flow, publish dưới dạng UPM package tại:
`https://github.com/KunNguyen/LoadingSystem`

## Tính năng chính

- Pipeline pattern: Boot -> InitSDK -> CheckAuth -> LoadLocal -> SyncCloud -> EnterGame
- Scene lifecycle với callback `OnSceneLoaded` khi scene load xong
- Tự hủy async task khi chuyển scene (cancellation token)
- Tách biệt UI qua interface `ILoadingUI` (dễ tích hợp với UI framework bất kỳ)

## Cài đặt (UPM từ GitHub)

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Hướng dẫn tích hợp

### 1) Implement `ILoadingUI`

```csharp
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MyLoadingUIAdapter : MonoBehaviour, ILoadingUI
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI textPercent;
    [SerializeField] private TMPro.TextMeshProUGUI textStep;

    public void ShowLoadingUI() => gameObject.SetActive(true);
    public void CloseLoadingUI() => gameObject.SetActive(false);
    public void UpdateLoadingBar(float progress) => progressBar.value = progress;
    public void SetLoadingText(int percent) => textPercent.text = $"{percent}%";
    public void SetStep(string step) => textStep.text = step;
    public void OnChangeScene(float delay, UnityAction action = null) => action?.Invoke();
}
```

### 2) Tạo Bootstrap scene

1. Tạo scene `BootstrapScene`
2. Tạo GameObject `SceneFlowManager`
3. Add component `Jis.LoadingSystems.SceneFlowManager`
4. Tạo GameObject `LoadingUI` có component implement `ILoadingUI`
5. Gán `LoadingUI` vào field `Loading UIRaw` của `SceneFlowManager`
6. Cấu hình tên scene:
   - `Init Sdk Scene`: scene khởi tạo SDK (Ads, IAP, Firebase...)
   - `Controller Scene`: scene chính sau boot

### 3) Tạo entry point

```csharp
using Jis.LoadingSystems;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame().Forget();
    }
}
```

### 4) Dùng lifecycle trong Controller scene

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        await SceneFlowManager.Instance.LoadSceneByName(
            "GameplayScene", null, LoadSceneMode.Additive);
    }
}
```

## Tuỳ chỉnh nâng cao

### Truyền delegate load/sync data

```csharp
await SceneFlowManager.Instance.StartGame(
    reload: false,
    fromLogin: false,
    onLoadLocalDataAsync: async () =>
    {
        await PlayerPrefsManager.LoadAsync();
        await LocalDatabase.LoadAsync();
    },
    onSyncCloudDataAsync: async () =>
    {
        await CloudSaveManager.SyncAsync();
    }
);
```

### Override pipeline để tích hợp auth (ví dụ Firebase)

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;

public class MySceneFlowManager : SceneFlowManager
{
    protected override LoadingPipeline CreatePipeline(
        LoadingContext ctx,
        System.Func<UniTask> onLoad,
        System.Func<UniTask> onSync)
    {
        return new MyLoadingPipeline(this, ctx, onLoad, onSync);
    }
}

public class MyLoadingPipeline : LoadingPipeline
{
    public MyLoadingPipeline(
        SceneFlowManager flow,
        LoadingContext ctx,
        System.Func<UniTask> onLoad,
        System.Func<UniTask> onSync) : base(flow, ctx, onLoad, onSync) { }

    protected override UniTask CheckAuthState()
    {
#if FIREBASE_AUTH
        Context.IsLoggedIn = FirebaseManager.Instance.Auth.IsSignedIn;
#endif
        return UniTask.CompletedTask;
    }
}
```

### Cancellation token theo scene

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await LoadHeavyResourceAsync(ct);
```

## Gợi ý cấu trúc thư mục

```text
Assets/
  Scenes/
    BootstrapScene.unity
    StartScene.unity
    InitSdkScene.unity
    ControllerScene.unity
    GameplayScene.unity
  Scripts/
    Loading/
      MyLoadingUIAdapter.cs
      MyLoadingPipeline.cs
    Scenes/
      StartSceneController.cs
      InitSdkSceneController.cs
      ControllerSceneController.cs
```

## Lưu ý

- `SceneFlowManager` dùng `DontDestroyOnLoad`, nên đặt ở bootstrap scene và không load lại scene này.
- `ILoadingUI` có thể null; flow vẫn chạy nhưng không cập nhật UI.
- Có thể tạo nhiều pipeline khác nhau (boot, login, reload) bằng cách kế thừa `LoadingPipeline`.
