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

## Quick Start

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

## Migration từ project hiện tại

Nếu project đang dùng `_Game.Scripts.Loading.LoadingSystems`:

1. Tạo `UIPanelLoadingAdapter` implement `ILoadingUI`, delegate tới `UIPanelLoading`
2. Thay `SceneFlowManager` (DDOLSingleton) bằng `Jis.LoadingSystems.SceneFlowManager`
3. Cập nhật `ISceneLifecycle` → `Jis.LoadingSystems.ISceneLifecycle`
4. Cập nhật `ScenePayload`, `LoadingContext` → dùng từ package hoặc kế thừa
5. `ControllerSceneLoadingPipeline` giữ trong project (phụ thuộc FactoryManager, TutorialManager...)

## License

MIT
