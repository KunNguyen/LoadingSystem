# LoadingSystem

Unity package cho scene loading flow, publish dưới dạng UPM package tại:
`https://github.com/KunNguyen/LoadingSystem`

## Tính năng chính

- Pipeline pattern: Boot -> InitSDK -> CheckAuth -> LoadLocal -> SyncCloud -> EnterGame
- Scene lifecycle với callback `OnSceneLoaded` khi scene load xong
- Tự hủy async task khi chuyển scene (cancellation token)
- Tách biệt UI qua interface `ILoadingUI` (dễ tích hợp với UI framework bất kỳ)

## Timeline thực thi

### App mở lần đầu (`StartGame(fromLogin: false)`)

```text
StartGame()
-> ShowLoadingUI
-> [Boot] LoadStartScene (0-10%, mặc định no-op)
-> [InitSDK] Load InitSdkScene (10-30%)
-> [CheckAuth] CheckAuthState()
-> [LoadLocalData] onLoadLocalDataAsync
-> [LoadCloudData] onSyncCloudDataAsync (chỉ khi IsLoggedIn == true)
-> [EnterGame] Load ControllerScene (70-100%)
-> Notify ISceneLifecycle.OnSceneLoaded(payload)
-> HideLoadingUI
```

### Đi từ login vào game (`StartGame(fromLogin: true)`)

```text
StartGame(fromLogin: true)
-> ShowLoadingUI
-> Bỏ qua Boot/InitSDK/CheckAuth/LoadData
-> EnterGame ngay: Load ControllerScene (0-100%)
-> OnSceneLoaded(payload)
-> HideLoadingUI
```

## Vai trò các scene

- `BootstrapScene`: chứa `SceneFlowManager` (DDOL) + `ILoadingUI`.
- `StartScene`: entry point, gọi `StartGame()`.
- `InitSdkScene`: init SDK (Firebase, Ads, IAP...), chỉ chạy một lần mỗi phiên.
- `ControllerScene`: scene điều phối; nhận payload và quyết định load scene tiếp theo.
- `GameplayScene`: scene nghiệp vụ chính, thường load từ `ControllerScene`.

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

### Checklist import nhanh

1. Thêm dependency trong `Packages/manifest.json` (block JSON bên dưới).
2. Tạo `BootstrapScene`, add `SceneFlowManager`.
3. **Nhanh:** Add `StubLoadingUI` vào GameObject, gán vào `loadingUIRaw`. Hoặc implement `ILoadingUI` đầy đủ.
4. Cấu hình `initSdkScene`, `controllerScene`.
5. Ở scene đầu, gọi `SceneFlowManager.Instance.StartGame().Forget();`
6. Trong `ControllerScene`, implement `ISceneLifecycle` để xử lý payload và điều hướng.
7. Add đủ scene vào Build Settings.

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

Ví dụ có xử lý payload trước khi vào gameplay:

```csharp
public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        if (payload is ScenePayload p && p.FromLogin)
            await ShowWelcomeBackPopup();

        if (payload is ScenePayload reloadPayload && reloadPayload.IsReload)
        {
            await ReloadGameplay();
            return;
        }

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

## Ý nghĩa tham số `StartGame`

- `reload`: đánh dấu reload dữ liệu/gameplay.
- `fromLogin`: `true` thì vào game ngay, bỏ qua các bước boot khác.
- `onLoadLocalDataAsync`: callback load dữ liệu local.
- `onSyncCloudDataAsync`: callback sync dữ liệu cloud.

**API gọn hơn:** `StartGame(StartGameOptions.FromLogin())` hoặc `StartGame(StartGameOptions.ReloadGame())`.

## Tối ưu đã áp dụng

- **StubLoadingUI**: Base class để test nhanh, chỉ cần show/hide GameObject.
- **LoadingUI timeout**: Không deadlock nếu quên gán; timeout 5s (config) rồi tiếp tục.
- **sceneLoadDelay**: Cấu hình delay trước load scene (mặc định 0.25s).
- **StartGameOptions**: Gọi `StartGame(options)` rõ ràng hơn.
- **Error handling**: `OnSceneLoaded` lỗi không làm crash cả flow; log và tiếp tục.
