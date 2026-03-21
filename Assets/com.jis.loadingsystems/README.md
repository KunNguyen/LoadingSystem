# JIS Loading System

Module loading per-scene tái sử dụng cho Unity, dùng Pipeline pattern, Scene Lifecycle và Cancellation support.

## Yêu cầu

- Unity 2022.3+
- [UniTask](https://github.com/Cysharp/UniTask) (com.cysharp.unitask)

## Cài đặt

### Cách 1: Local package (trong project)

1. Copy thư mục package vào `Packages/com.jis.loadingsystem` của project
2. Mở `Packages/manifest.json`, thêm:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "file:com.jis.loadingsystem",
    ...
  }
}
```

### Cách 2: Git URL

```json
"com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems"
```

## Kiến trúc

```
SceneFlowManager (MonoBehaviour, DDOL)
    │
    ├── ILoadingUI          ← Implement trong project (UIPanelLoading, v.v.)
    ├── LoadingPipeline    ← Boot: Boot → InitSDK → CheckAuth → LoadLocal → SyncCloud → EnterGame
    ├── SceneCancellationManager
    └── ISceneLifecycle    ← Implement trên controller trong mỗi scene
```

## Timeline thực thi (chi tiết)

### A) App mở lần đầu / Boot đầy đủ (`StartGame(fromLogin: false)`)

```text
T0: StartSceneController.Start()
    -> SceneFlowManager.Instance.StartGame(...)
    -> ShowLoadingUI(), progress = 0%

T1: [Boot]
    -> LoadStartScene(0% -> 10%) [mặc định no-op, để mở rộng nếu cần]

T2: [InitSDK]
    -> Load InitSdkScene (Single, 10% -> 30%)
    -> OnSceneLoaded(payload) của InitSdkScene (nếu có)
    -> _sdkInitialized = true

T3: [CheckAuth]
    -> CheckAuthState() (mặc định: IsLoggedIn = false, có thể override)

T4: [LoadLocalData]
    -> chạy onLoadLocalDataAsync delegate (nếu truyền vào)

T5: [LoadCloudData]
    -> nếu IsLoggedIn == true: chạy onSyncCloudDataAsync
    -> nếu false: skip, set progress thẳng lên 70%

T6: [EnterGame]
    -> Load ControllerScene (Single, 70% -> 100%)
    -> gọi OnSceneLoaded(payload) cho toàn bộ ISceneLifecycle trong scene
    -> HideLoadingUI()
```

### B) Đi từ Login sang game (`StartGame(fromLogin: true)`)

```text
T0: StartGame(fromLogin: true)
T1: Bỏ qua Boot/InitSDK/CheckAuth/LoadData
T2: EnterGame ngay:
    -> Load ControllerScene (0% -> 100%)
    -> OnSceneLoaded(payload)
    -> HideLoadingUI()
```

## Vai trò từng scene

- `BootstrapScene`
  - Chứa `SceneFlowManager` (DDOL) và đối tượng implement `ILoadingUI`.
  - Là scene gốc giữ manager sống xuyên suốt app.
- `StartScene`
  - Scene entry của app (logo/splash/menu khởi động).
  - Thường chỉ có nhiệm vụ gọi `StartGame()`.
- `InitSdkScene`
  - Nơi init SDK bên thứ 3 (Firebase, Ads, IAP, Analytics...).
  - Được load `Single`, chỉ chạy một lần trong phiên nhờ `_sdkInitialized`.
- `ControllerScene`
  - Scene điều phối sau boot, nhận `ScenePayload` qua `OnSceneLoaded`.
  - Từ đây có thể quyết định load `GameplayScene`, `HomeScene`, `TutorialScene`...
- `GameplayScene` (hoặc scene tính năng)
  - Scene nghiệp vụ chính; có thể load `Additive` hoặc `Single` tuỳ luồng game.

## Quick Start

### Checklist import vào project (khuyên dùng)

1. Mở `Packages/manifest.json`, thêm dependency:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

2. Tạo `BootstrapScene` và add `SceneFlowManager`.
3. Tạo loading UI adapter implement `ILoadingUI`, kéo vào `loadingUIRaw`.
4. Cấu hình `initSdkScene` + `controllerScene` trong Inspector.
5. Thêm `StartSceneController` ở scene đầu để gọi `StartGame()`.
6. Trong `ControllerScene`, implement `ISceneLifecycle` để điều hướng scene thực tế.
7. Đảm bảo các scene có trong Build Settings.

### 1. Tạo ILoadingUI adapter

UI loading hiện tại cần implement interface `ILoadingUI`:

```csharp
using Jis.LoadingSystems;
using UnityEngine.Events;

public class UIPanelLoadingAdapter : MonoBehaviour, ILoadingUI
{
    [SerializeField] private UIPanelLoading _panel; // UI của bạn

    public void ShowLoadingUI() => _panel.ShowLoadingUI();
    public void CloseLoadingUI() => _panel.CloseLoadingUI();
    public void UpdateLoadingBar(float progress) => _panel.UpdateLoadingBar(progress);
    public void SetLoadingText(int percent) => _panel.SetLoadingText(percent);
    public void SetStep(string step) => _panel.SetStep(step);
    public void OnChangeScene(float delay, UnityAction action = null) => _panel.OnChangeScene(delay, action);
}
```

Hoặc implement trực tiếp trên `UIPanelLoading` nếu bạn sửa được.

### 2. Setup SceneFlowManager

1. Tạo GameObject trong bootstrap scene (ví dụ `Bootstrap`)
2. Add component `SceneFlowManager`
3. Gán `loadingUIRaw` = GameObject có component implement `ILoadingUI`
4. Cấu hình tên scene: `initSdkScene`, `controllerScene`

### 3. Entry point

Gọi từ scene đầu tiên (ví dụ StartSceneController):

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame().Forget();
    }
}
```

Với load/sync data:

```csharp
SceneFlowManager.Instance.StartGame(
    reload: false,
    fromLogin: false,
    onLoadLocalDataAsync: () => YourDataManager.LoadLocalAsync(),
    onSyncCloudDataAsync: () => YourCloudManager.SyncAsync()
).Forget();
```

### 4. Scene Lifecycle

Mỗi scene có controller implement `ISceneLifecycle`:

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        if (payload is ScenePayload p && p.IsReload)
            await ReloadGameplay();
        else
            await LoadGameplay();
    }
}
```

Ví dụ điều hướng scene theo payload:

```csharp
public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        if (payload is ScenePayload p)
        {
            if (p.FromLogin)
            {
                await ShowWelcomeBackPopup();
            }

            if (p.IsReload)
            {
                await ReloadGameplay();
                return;
            }
        }

        await SceneFlowManager.Instance.LoadSceneByName(
            "GameplayScene",
            null,
            UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }
}
```

### 5. Cancellation token

Dùng token khi chạy async trong scene để tự hủy khi chuyển scene:

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeAsyncWork(ct);
```

## Mở rộng

- **LoadingContext**: Thêm field cho pipeline
- **ScenePayload**: Thêm field cho payload truyền scene
- **LoadingPipeline**: Kế thừa và override `CheckAuthState()` để tích hợp Firebase/auth
- **LoadingStep**: Thêm enum mới nếu cần bước khác

## API chính

| API | Mô tả |
|-----|-------|
| `SceneFlowManager.Instance.StartGame(...)` | Entry point boot |
| `SceneFlowManager.Instance.LoadSceneByName(...)` | Load scene tùy ý |
| `SceneFlowManager.Instance.GetCurrentSceneCancellationToken()` | Token hủy khi đổi scene |
| `ISceneLifecycle.OnSceneLoaded(payload)` | Callback khi scene load xong |

## Giải thích nhanh tham số StartGame

- `reload`: đánh dấu đây là lượt reload dữ liệu/gameplay.
- `fromLogin`: nếu `true`, pipeline vào game ngay (bỏ qua các bước boot khác).
- `onLoadLocalDataAsync`: callback load dữ liệu local (cache, save local, db local...).
- `onSyncCloudDataAsync`: callback sync cloud (save cloud/profile/inventory...).

## Migration từ project hiện tại

Nếu project đang dùng `_Game.Scripts.Loading.LoadingSystems`:

1. Tạo `UIPanelLoadingAdapter` implement `ILoadingUI`, delegate tới `UIPanelLoading`
2. Thay `SceneFlowManager` (DDOLSingleton) bằng `Jis.LoadingSystems.SceneFlowManager`
3. Cập nhật `ISceneLifecycle` → `Jis.LoadingSystems.ISceneLifecycle`
4. Cập nhật `ScenePayload`, `LoadingContext` → dùng từ package hoặc kế thừa
5. `ControllerSceneLoadingPipeline` giữ trong project (phụ thuộc FactoryManager, TutorialManager...)

## License

MIT
