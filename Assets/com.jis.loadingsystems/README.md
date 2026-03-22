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

### Setup 1 bước (khuyên dùng)

Sau khi thêm package, vào menu:

- **Tools > JIS Loading System > Setup Template (1-click)**  
  Hoặc  
- **Tools > JIS Loading System > Setup Template** (chọn thư mục)  
- Hoặc **Right-click trong Project > Create > JIS Loading System > Setup Template**

Template sẽ tạo sẵn: scenes (BootstrapScene, InitSdkScene, ControllerScene, GameplayScene), scripts mẫu, Build Settings. Mở `BootstrapScene` và Play để test.

### Checklist import thủ công

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

**Cách nhanh:** Thêm component `StubLoadingUI` vào GameObject (chỉ show/hide), gán vào `loadingUIRaw`. Đủ để test flow.

**Tùy chỉnh:** Implement `ILoadingUI` hoặc kế thừa `StubLoadingUI` và override method cần dùng:

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

Gọi từ scene đầu tiên:

```csharp
SceneFlowManager.Instance.StartGame().Forget();
```

Với options rõ ràng hơn:

```csharp
// Vào game từ login (bỏ qua boot)
SceneFlowManager.Instance.StartGame(StartGameOptions.WhenFromLogin()).Forget();

// Reload game
SceneFlowManager.Instance.StartGame(StartGameOptions.WhenReload()).Forget();

// Load/sync data
SceneFlowManager.Instance.StartGame(new StartGameOptions
{
    OnLoadLocalDataAsync = () => YourDataManager.LoadLocalAsync(),
    OnSyncCloudDataAsync = () => YourCloudManager.SyncAsync()
}).Forget();
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
- **LoadingPipeline**: Override `GetSteps()` để tùy chỉnh steps theo game
- **LoadingStep**: Thêm enum mới nếu cần bước khác
- **CurrentProgress, CurrentStep**: Đọc từ `SceneFlowManager.Instance` để hiển thị trên LoadingUI

### Tùy chỉnh pipeline steps

Override `GetSteps()` trong pipeline kế thừa:

```csharp
public class MyLoadingPipeline : LoadingPipeline
{
    public MyLoadingPipeline(SceneFlowManager flow, LoadingContext ctx, Func<UniTask> onLoad, Func<UniTask> onSync)
        : base(flow, ctx, onLoad, onSync) { }

    protected override List<PipelineStepDefinition> GetSteps()
    {
        var steps = new List<PipelineStepDefinition>
        {
            new(LoadingStep.Boot, () => Flow.LoadStartScene(0f, 0.1f), 0.1f),
            new(LoadingStep.InitSDK, () => Flow.LoadInitSdkScene(0.1f, 0.3f), 0.3f),
            new(LoadingStep.FetchRemoteConfig, FetchRemoteConfigAsync, 0.35f),  // thêm step mới
            new(LoadingStep.CheckAuth, CheckAuthState, 0.4f),
            new(LoadingStep.LoadLocalData, () => /* ... */ UniTask.CompletedTask, 0.5f)
        };
        if (Context.IsLoggedIn)
            steps.Add(new PipelineStepDefinition(LoadingStep.LoadCloudData, SyncCloudData, 0.7f));
        else
            Flow.UpdateLoadingBar(0.7f);
        steps.Add(new PipelineStepDefinition(LoadingStep.EnterGame, () => Flow.LoadControllerScene(...), 1f));
        return steps;
    }

    private async UniTask FetchRemoteConfigAsync() => await MyRemoteConfig.FetchAsync();
}
```

### Đọc progress hiện tại (cho LoadingUI)

```csharp
// Trong ILoadingUI hoặc logic khác
var progress = SceneFlowManager.Instance.CurrentProgress;  // 0–1
var step = SceneFlowManager.Instance.CurrentStep;          // LoadingStep enum
```

## API chính

| API | Mô tả |
|-----|-------|
| `SceneFlowManager.Instance.StartGame(...)` | Entry point boot |
| `SceneFlowManager.Instance.CurrentProgress` | Progress hiện tại (0–1) |
| `SceneFlowManager.Instance.CurrentStep` | Bước đang chạy (LoadingStep) |
| `SceneFlowManager.Instance.LoadSceneByName(...)` | Load scene tùy ý |
| `SceneFlowManager.Instance.GetCurrentSceneCancellationToken()` | Token hủy khi đổi scene |
| `ISceneLifecycle.OnSceneLoaded(payload)` | Callback khi scene load xong |
| `LoadingPipeline.GetSteps()` | Override để tùy chỉnh pipeline steps |

## Tối ưu & cấu hình (SceneFlowManager Inspector)

| Field | Mô tả | Default |
|-------|-------|---------|
| `sceneLoadDelay` | Delay trước khi load scene (giây). 0 = tắt | 0.25 |
| `loadingUIWaitTimeout` | Timeout chờ LoadingUI (giây). 0 = chờ vô hạn | 5 |

Nếu không gán LoadingUI, sau `loadingUIWaitTimeout` giây sẽ log warning và tiếp tục (tránh deadlock).

## StubLoadingUI và vòng đời object khi đổi scene

`StubLoadingUI` **có thể bị mất** khi chuyển scene, tùy cách đặt object:

- `SceneFlowManager` gọi `DontDestroyOnLoad(gameObject)`, nên object chứa manager (và child của nó) sẽ được giữ lại.
- `StubLoadingUI` không tự DDOL; nếu đặt trong scene thường, không nằm dưới root DDOL, nó sẽ bị destroy khi load scene `Single`.
- Nếu đặt `StubLoadingUI` dưới cùng root bootstrap với `SceneFlowManager`, nó sẽ sống xuyên scene.
- Nếu load `Additive`, object chỉ mất khi scene chứa nó bị unload.

### Setup khuyến nghị (an toàn)

1. Tạo root `BootstrapRoot` trong bootstrap scene.
2. Gắn `SceneFlowManager` vào `BootstrapRoot`.
3. Tạo `LoadingUI` (gắn `StubLoadingUI` hoặc UI adapter thật) làm child của `BootstrapRoot`.
4. Gán component vào `loadingUIRaw` trong Inspector.
5. Không tạo thêm bản sao `SceneFlowManager` ở các scene khác (tránh duplicate rồi bị destroy).

## Giải thích tham số StartGame

- `reload`: đánh dấu reload dữ liệu/gameplay.
- `fromLogin`: `true` → vào game ngay (bỏ qua boot).
- `onLoadLocalDataAsync`: load dữ liệu local.
- `onSyncCloudDataAsync`: sync cloud.

## Migration từ project hiện tại

Nếu project đang dùng `_Game.Scripts.Loading.LoadingSystems`:

1. Tạo `UIPanelLoadingAdapter` implement `ILoadingUI`, delegate tới `UIPanelLoading`
2. Thay `SceneFlowManager` (DDOLSingleton) bằng `Jis.LoadingSystems.SceneFlowManager`
3. Cập nhật `ISceneLifecycle` → `Jis.LoadingSystems.ISceneLifecycle`
4. Cập nhật `ScenePayload`, `LoadingContext` → dùng từ package hoặc kế thừa
5. `ControllerSceneLoadingPipeline` giữ trong project (phụ thuộc FactoryManager, TutorialManager...)

## License

MIT
