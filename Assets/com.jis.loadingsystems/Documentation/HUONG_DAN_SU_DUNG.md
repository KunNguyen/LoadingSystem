# Hướng dẫn sử dụng JIS Loading System

## 1. Giới thiệu

**JIS Loading Systems** là module Unity tái sử dụng cho hệ thống loading theo từng scene, hỗ trợ:

- **Pipeline pattern**: Boot → InitSDK → CheckAuth → LoadLocal → SyncCloud → EnterGame
- **Scene Lifecycle**: Callback `OnSceneLoaded` khi mỗi scene load xong
- **Cancellation**: Tự hủy async task khi chuyển scene
- **Tách biệt UI**: Interface `ILoadingUI` cho phép dùng bất kỳ UI framework nào

## 2. Cài đặt

### Bước 1: Thêm package

**Option A – Local (trong cùng repo):**

Đảm bảo thư mục `Packages/com.jis.loadingsystem` tồn tại. Trong `Packages/manifest.json`:

```json
"com.jis.loadingsystem": "file:com.jis.loadingsystem"
```

**Option B – Từ project khác:**

Copy toàn bộ thư mục package vào `Packages/com.jis.loadingsystem` của project mới, rồi thêm dependency như trên.

### Bước 2: Kiểm tra UniTask

Package phụ thuộc UniTask. Nếu chưa có:

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

## 3. Tích hợp vào project

### 3.1 Implement ILoadingUI

Tạo adapter cho UI loading hiện tại:

```csharp
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.Events;

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

### 3.2 Tạo Bootstrap scene

1. Tạo scene mới `BootstrapScene`
2. Tạo GameObject `SceneFlowManager`
3. Add component `Jis.LoadingSystems.SceneFlowManager`
4. Tạo GameObject `LoadingUI` với component implement `ILoadingUI`
5. Kéo `LoadingUI` vào field `Loading UIRaw` của SceneFlowManager
6. Cấu hình tên scene:
   - **Init Sdk Scene**: Scene khởi tạo SDK (Ads, IAP, Firebase...)
   - **Controller Scene**: Scene chính sau khi boot

### 3.3 Entry point

Tạo script gọi boot từ scene đầu (ví dụ StartScene):

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    void Start()
    {
        SceneFlowManager.Instance.StartGame().Forget();
    }
}
```

### 3.4 Scene controller với Lifecycle

Trong Controller Scene, tạo controller implement `ISceneLifecycle`:

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        // Load gameplay scene additive, init UI, v.v.
        await SceneFlowManager.Instance.LoadSceneByName(
            "GameplayScene", null, 
            UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }
}
```

## 4. Load/Sync data tùy chỉnh

Truyền delegate khi gọi `StartGame`:

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

## 5. Tích hợp Firebase Auth

**Cách 1 – Override CreatePipeline trong SceneFlowManager:**

Tạo class kế thừa `SceneFlowManager`:

```csharp
using Jis.LoadingSystems;
using Cysharp.Threading.Tasks;

public class MySceneFlowManager : SceneFlowManager
{
    protected override LoadingPipeline CreatePipeline(LoadingContext ctx,
        System.Func<UniTask> onLoad, System.Func<UniTask> onSync)
    {
        return new MyLoadingPipeline(this, ctx, onLoad, onSync);
    }
}

public class MyLoadingPipeline : LoadingPipeline
{
    public MyLoadingPipeline(SceneFlowManager flow, LoadingContext ctx,
        System.Func<UniTask> onLoad, System.Func<UniTask> onSync)
        : base(flow, ctx, onLoad, onSync) { }

    protected override UniTask CheckAuthState()
    {
#if FIREBASE_AUTH
        Context.IsLoggedIn = FirebaseManager.Instance.Auth.IsSignedIn;
#endif
        return UniTask.CompletedTask;
    }
}
```

Dùng `MySceneFlowManager` thay cho `SceneFlowManager` trên GameObject.

## 6. Cancellation token

Dùng token để hủy task khi chuyển scene:

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await LoadHeavyResourceAsync(ct);
```

## 7. Mở rộng LoadingContext và ScenePayload

**LoadingContext** – thêm field cho pipeline:

```csharp
// Trong project, tạo class kế thừa
public class MyLoadingContext : LoadingContext
{
    public string UserId;
    public int LastLevel;
}
```

**ScenePayload** – thêm field cho payload truyền giữa các scene:

```csharp
public class MyScenePayload : ScenePayload
{
    public int LevelToLoad;
    public bool FromMainMenu;
}
```

## 8. Cấu trúc thư mục gợi ý

```
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
      MyLoadingPipeline.cs (nếu cần override)
    Scenes/
      StartSceneController.cs
      InitSdkSceneController.cs
      ControllerSceneController.cs
Packages/
  com.jis.loadingsystem/
```

## 9. Lưu ý

- `SceneFlowManager` dùng `DontDestroyOnLoad`, nên đặt trong scene bootstrap và không load lại scene đó.
- `ILoadingUI` có thể null; SceneFlowManager vẫn chạy nhưng không cập nhật UI.
- Có thể dùng nhiều pipeline khác nhau (boot, login, reload) bằng cách tạo các class kế thừa `LoadingPipeline`.
