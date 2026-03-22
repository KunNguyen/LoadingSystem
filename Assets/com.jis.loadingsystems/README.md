# JIS Loading System (Tiếng Việt)

Hệ thống loading dạng SDK cho Unity, hỗ trợ pipeline theo step, UI tách rời bằng event, và tiến độ mượt.

English version: `README.en.md`

## Tùy chỉnh pipeline nhanh nhất

Không cần viết lại toàn bộ flow. Chỉ cần kế thừa `SceneFlowManager` và override `CustomizePipeline(...)`.

### Key mặc định của các step

- `SceneFlowManager.StepKeyInitSdk`
- `SceneFlowManager.StepKeyLoadInitScene`
- `SceneFlowManager.StepKeyLoadLocalData`
- `SceneFlowManager.StepKeySyncCloudData`
- `SceneFlowManager.StepKeyFakeDelay`
- `SceneFlowManager.StepKeyLoadControllerScene`
- `SceneFlowManager.StepKeyPostInit`

### API chỉnh pipeline

- `AddStep(step, key)`
- `InsertBefore(anchorKey, step, key)`
- `InsertAfter(anchorKey, step, key)`
- `ReplaceStep(key, newStep, newKey)`
- `RemoveStep(key)`

### Ví dụ dùng ngay

File mẫu có sẵn trong package:

- `Runtime/Examples/MySceneFlowManagerExample.cs`

Trong `BootstrapScene`, thay component `SceneFlowManager` bằng `MySceneFlowManagerExample`.

```csharp
using Jis.LoadingSystems;
using UnityEngine;

public class MySceneFlowManager : SceneFlowManager
{
    protected override void CustomizePipeline(
        LoadingPipeline pipeline,
        LoadingContext context,
        StartGameOptions options)
    {
        pipeline.InsertBefore(
            StepKeyLoadLocalData,
            new FetchRemoteConfigStepExample(),
            key: "fetch-remote-config");

        pipeline.ReplaceStep(
            StepKeyFakeDelay,
            new DelayStep(0.6f, 0.08f));

        if (Debug.isDebugBuild)
            pipeline.RemoveStep(StepKeySyncCloudData);

        pipeline.InsertAfter(
            StepKeyLoadControllerScene,
            new PostWarmupStepExample(),
            key: "post-warmup");
    }
}
```
# JIS Loading System (Tiếng Việt)

Hệ thống loading dạng SDK cho Unity, hỗ trợ pipeline theo step, UI tách rời bằng event, và tiến độ mượt.

English version: `README.en.md`

## 1) Yêu cầu

- Unity `2022.3+`
- [UniTask](https://github.com/Cysharp/UniTask)

## 2) Cài đặt qua UPM

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## 3) Kiến trúc Runtime

```text
Abstractions/
  ILoadingStep
  ILoadingUI
  ISceneLifecycle

Core/
  LoadingManager
  LoadingPipelineRunner
  LoadingContext
  LoadingEvents

Steps/
  InitSDKStep
  DelayStep
  LoadSceneStep
  PostInitStep
  DelegateStep

UI/
  LoadingUIPresenter
  StubLoadingUI

Utils/
  ProgressSmoother
```

## 4) Setup nhanh bằng Template (khuyên dùng)

Menu:

- `Tools > JIS Loading System > Setup Template (1-click)`
- hoặc `Tools > JIS Loading System > Setup Template`
- hoặc `Assets > Create > JIS Loading System > Setup Template`

Template sẽ tạo:

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` gồm:
  - `SceneFlowManager`
  - `BootstrapController`
  - child `LoadingUI` + `StubLoadingUI` (đã bind `loadingUIRaw`)
- script mẫu:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`

## 5) Tích hợp thủ công

1. Trong `BootstrapScene`, tạo `BootstrapRoot`.
2. Add `SceneFlowManager` + `BootstrapController`.
3. Tạo child `LoadingUI`, add `StubLoadingUI` hoặc adapter UI của bạn.
4. Gán component đó vào `SceneFlowManager.loadingUIRaw`.
5. Đảm bảo `InitSdkScene`, `ControllerScene` có trong Build Settings.

## 6) Cách chạy flow cơ bản

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame(new StartGameOptions
        {
            Reload = false,
            FromLogin = false,
            OnLoadLocalDataAsync = LoadLocalDataAsync,
            OnSyncCloudDataAsync = SyncCloudDataAsync
        }).Forget();
    }

    private static async UniTask LoadLocalDataAsync() => await UniTask.Delay(300);
    private static async UniTask SyncCloudDataAsync() => await UniTask.Delay(500);
}
```

## 7) Tùy chỉnh Pipeline dễ nhất (khuyến nghị)

Bạn **không cần viết lại toàn bộ pipeline**.  
Chỉ cần kế thừa `SceneFlowManager` và override `CustomizePipeline(...)`.

SDK đã gắn sẵn key cho các step mặc định:

- `SceneFlowManager.StepKeyInitSdk`
- `SceneFlowManager.StepKeyLoadInitScene`
- `SceneFlowManager.StepKeyLoadLocalData`
- `SceneFlowManager.StepKeySyncCloudData`
- `SceneFlowManager.StepKeyFakeDelay`
- `SceneFlowManager.StepKeyLoadControllerScene`
- `SceneFlowManager.StepKeyPostInit`

`LoadingPipeline` hỗ trợ sẵn:

- `AddStep(step, key)`
- `InsertBefore(anchorKey, step, key)`
- `InsertAfter(anchorKey, step, key)`
- `ReplaceStep(key, newStep, newKey)`
- `RemoveStep(key)`

### Có sẵn ví dụ dùng ngay

Bạn có thể dùng trực tiếp:

- `Runtime/Examples/MySceneFlowManagerExample.cs`

File này đã gồm:

- `MySceneFlowManagerExample` (override `CustomizePipeline`)
- `FetchRemoteConfigStepExample`
- `PostWarmupStepExample`

Trong scene, thay `SceneFlowManager` bằng `MySceneFlowManagerExample` để test ngay.

Ví dụ:

```csharp
using Jis.LoadingSystems;
using UnityEngine;

public class MySceneFlowManager : SceneFlowManager
{
    protected override void CustomizePipeline(
        LoadingPipeline pipeline,
        LoadingContext context,
        StartGameOptions options)
    {
        pipeline.InsertBefore(
            StepKeyLoadLocalData,
            new FetchRemoteConfigStepExample(),
            key: "fetch-remote-config");

        pipeline.ReplaceStep(
            StepKeyFakeDelay,
            new DelayStep(durationSeconds: 0.6f, weight: 0.08f));

        if (Debug.isDebugBuild)
            pipeline.RemoveStep(StepKeySyncCloudData);

        pipeline.InsertAfter(
            StepKeyLoadControllerScene,
            new PostWarmupStepExample(),
            key: "post-warmup");
    }
}
```

## 8) Ví dụ tạo step mới

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;

public sealed class FetchRemoteConfigStep : ILoadingStep
{
    public float Weight => 0.2f;

    public async UniTask Execute(LoadingContext context)
    {
        for (int i = 0; i <= 10; i++)
        {
            context.ReportStepProgress(i / 10f);
            await UniTask.DelayFrame(1, cancellationToken: context.CancellationToken);
        }

        context.Set("remote_config_ready", true);
    }
}
```

## 9) Load scene nâng cao

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: null,
    mode: LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

`LoadSceneStep` hỗ trợ:

- async loading
- `allowSceneActivation = false`
- `Single` và `Additive`

## 10) Event UI

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

`LoadingUIPresenter` subscribe event để cập nhật `ILoadingUI`.

## 11) Lưu ý `StubLoadingUI`

`StubLoadingUI` có thể bị mất nếu không nằm dưới root DDOL.

Setup an toàn:

1. `SceneFlowManager` trên `BootstrapRoot`.
2. `LoadingUI` là child của `BootstrapRoot`.
3. Không đặt thêm `SceneFlowManager` ở scene khác.

## 12) Cancellation token

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## License

MIT
# JIS Loading System (Tiếng Việt)

Hệ thống loading dạng SDK cho Unity, hỗ trợ pipeline theo step, UI tách rời bằng event, và tiến độ mượt.

English version: `README.en.md`

## 1) Yêu cầu

- Unity `2022.3+`
- [UniTask](https://github.com/Cysharp/UniTask)

## 2) Cài đặt qua UPM

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## 3) Kiến trúc Runtime

```text
Abstractions/
  ILoadingStep
  ILoadingUI
  ISceneLifecycle

Core/
  LoadingManager
  LoadingPipelineRunner
  LoadingContext
  LoadingEvents

Steps/
  InitSDKStep
  DelayStep
  LoadSceneStep
  PostInitStep
  DelegateStep

UI/
  LoadingUIPresenter
  StubLoadingUI

Utils/
  ProgressSmoother
```

## 4) Setup nhanh bằng Template (khuyên dùng)

Menu:

- `Tools > JIS Loading System > Setup Template (1-click)`
- hoặc `Tools > JIS Loading System > Setup Template`
- hoặc `Assets > Create > JIS Loading System > Setup Template`

Template sẽ tạo:

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` gồm:
  - `SceneFlowManager`
  - `BootstrapController`
  - child `LoadingUI` + `StubLoadingUI` (đã bind `loadingUIRaw`)
- script mẫu:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`

## 5) Tích hợp thủ công

1. Trong `BootstrapScene`, tạo `BootstrapRoot`.
2. Add `SceneFlowManager` + `BootstrapController`.
3. Tạo child `LoadingUI`, add `StubLoadingUI` hoặc adapter UI của bạn.
4. Gán component đó vào `SceneFlowManager.loadingUIRaw`.
5. Đảm bảo `InitSdkScene`, `ControllerScene` có trong Build Settings.

## 6) Cách chạy flow cơ bản

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame(new StartGameOptions
        {
            Reload = false,
            FromLogin = false,
            OnLoadLocalDataAsync = LoadLocalDataAsync,
            OnSyncCloudDataAsync = SyncCloudDataAsync
        }).Forget();
    }

    private static async UniTask LoadLocalDataAsync() => await UniTask.Delay(300);
    private static async UniTask SyncCloudDataAsync() => await UniTask.Delay(500);
}
```

## 7) Tùy chỉnh Pipeline dễ nhất (khuyến nghị)

Bạn **không cần viết lại toàn bộ pipeline**.  
Chỉ cần kế thừa `SceneFlowManager` và override `CustomizePipeline(...)`.

SDK đã gắn sẵn key cho các step mặc định:

- `SceneFlowManager.StepKeyInitSdk`
- `SceneFlowManager.StepKeyLoadInitScene`
- `SceneFlowManager.StepKeyLoadLocalData`
- `SceneFlowManager.StepKeySyncCloudData`
- `SceneFlowManager.StepKeyFakeDelay`
- `SceneFlowManager.StepKeyLoadControllerScene`
- `SceneFlowManager.StepKeyPostInit`

`LoadingPipeline` hỗ trợ sẵn:

- `AddStep(step, key)`
- `InsertBefore(anchorKey, step, key)`
- `InsertAfter(anchorKey, step, key)`
- `ReplaceStep(key, newStep, newKey)`
- `RemoveStep(key)`

Ví dụ:

```csharp
using Jis.LoadingSystems;
using UnityEngine.SceneManagement;

public class MySceneFlowManager : SceneFlowManager
{
    protected override void CustomizePipeline(
        LoadingPipeline pipeline,
        LoadingContext context,
        StartGameOptions options)
    {
        // Chèn step fetch config trước load data local
        pipeline.InsertBefore(
            StepKeyLoadLocalData,
            new FetchRemoteConfigStep(),
            key: "fetch-remote-config");

        // Thay fake delay mặc định bằng delay dài hơn
        pipeline.ReplaceStep(
            StepKeyFakeDelay,
            new DelayStep(durationSeconds: 0.6f, weight: 0.08f));

        // Bỏ cloud sync ở môi trường dev
        if (Debug.isDebugBuild)
            pipeline.RemoveStep(StepKeySyncCloudData);

        // Chèn step hậu xử lý sau khi load ControllerScene
        pipeline.InsertAfter(
            StepKeyLoadControllerScene,
            new PostWarmupStep(),
            key: "post-warmup");
    }
}
```

> Trong scene, thay component `SceneFlowManager` bằng `MySceneFlowManager`.

## 8) Ví dụ tạo step mới

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;

public sealed class FetchRemoteConfigStep : ILoadingStep
{
    public float Weight => 0.2f;

    public async UniTask Execute(LoadingContext context)
    {
        for (int i = 0; i <= 10; i++)
        {
            context.ReportStepProgress(i / 10f);
            await UniTask.DelayFrame(1, cancellationToken: context.CancellationToken);
        }

        context.Set("remote_config_ready", true);
    }
}
```

## 9) Load scene nâng cao

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: null,
    mode: LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

`LoadSceneStep` hỗ trợ:

- async loading
- `allowSceneActivation = false`
- `Single` và `Additive`

## 10) Event UI

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

`LoadingUIPresenter` subscribe event để cập nhật `ILoadingUI`.

## 11) Lưu ý `StubLoadingUI`

`StubLoadingUI` có thể bị mất nếu không nằm dưới root DDOL.

Setup an toàn:

1. `SceneFlowManager` trên `BootstrapRoot`.
2. `LoadingUI` là child của `BootstrapRoot`.
3. Không đặt thêm `SceneFlowManager` ở scene khác.

## 12) Cancellation token

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## License

MIT
# JIS Loading System (Tiếng Việt)

Hệ thống loading theo kiến trúc SDK-ready cho Unity, hỗ trợ pipeline theo step, UI tách rời bằng event, và progress mượt.

English version: `README.en.md`

## 1) Yêu cầu

- Unity `2022.3+`
- [UniTask](https://github.com/Cysharp/UniTask)

## 2) Cài đặt qua UPM

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## 3) Kiến trúc runtime

```text
Abstractions/
  ILoadingStep
  ILoadingUI
  ISceneLifecycle

Core/
  LoadingManager           -> lõi chạy pipeline (DDOL)
  LoadingPipelineRunner    -> chạy step + tính progress theo trọng số
  LoadingContext           -> dữ liệu dùng chung + cancellation + report progress
  LoadingEvents            -> OnStart / OnProgress / OnComplete

Steps/
  InitSDKStep              -> step khởi tạo SDK (mock)
  DelayStep                -> fake loading
  LoadSceneStep            -> load scene async (hỗ trợ manual activation)
  PostInitStep             -> gọi ISceneLifecycle sau khi scene load xong
  DelegateStep             -> step tùy biến bằng callback

UI/
  LoadingUIPresenter       -> lắng nghe event và cập nhật ILoadingUI
  StubLoadingUI            -> UI stub mặc định để test nhanh

Utils/
  ProgressSmoother         -> làm mượt tiến độ hiển thị
```

## 4) Tích hợp nhanh bằng Template (khuyên dùng)

Mở menu:

- `Tools > JIS Loading System > Setup Template (1-click)`
- hoặc `Tools > JIS Loading System > Setup Template`
- hoặc `Assets > Create > JIS Loading System > Setup Template`

Template sẽ tự tạo:

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` gồm:
  - `SceneFlowManager`
  - `BootstrapController`
  - `LoadingUI` (child) + `StubLoadingUI` (đã gán vào `loadingUIRaw`)
- script mẫu:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`
- Build Settings entries

### Vì sao setup này an toàn?

- `SceneFlowManager` dùng `DontDestroyOnLoad`.
- `LoadingUI` là child của `BootstrapRoot` nên không bị mất khi `LoadSceneMode.Single`.
- Logic loading không gọi UI trực tiếp, mọi cập nhật đi qua `LoadingEvents` + `LoadingUIPresenter`.

## 5) Tích hợp thủ công từng bước

### Bước 1: Tạo BootstrapRoot

Trong `BootstrapScene`:

1. Tạo GameObject `BootstrapRoot`.
2. Add `SceneFlowManager`.
3. Add `BootstrapController` (entry point gọi `StartGame()`).
4. Tạo child `LoadingUI` và add `StubLoadingUI` (hoặc adapter UI của bạn).
5. Kéo component này vào `SceneFlowManager.loadingUIRaw`.

### Bước 2: Tạo UI adapter (khuyến nghị)

```csharp
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MyLoadingUIAdapter : MonoBehaviour, ILoadingUI
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI progressText;
    [SerializeField] private CanvasGroup canvasGroup;

    public void ShowLoadingUI() => canvasGroup.alpha = 1f;
    public void CloseLoadingUI() => canvasGroup.alpha = 0f;
    public void UpdateLoadingBar(float progress) => progressBar.value = progress;
    public void SetLoadingText(int percent) => progressText.text = $"{percent}%";
    public void SetStep(string step) { }
    public void OnChangeScene(float delay, UnityAction action = null) => action?.Invoke();
}
```

### Bước 3: Chạy flow

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame(new StartGameOptions
        {
            Reload = false,
            FromLogin = false,
            OnLoadLocalDataAsync = LoadLocalDataAsync,
            OnSyncCloudDataAsync = SyncCloudDataAsync
        }).Forget();
    }

    private static async UniTask LoadLocalDataAsync()
    {
        await UniTask.Delay(300);
    }

    private static async UniTask SyncCloudDataAsync()
    {
        await UniTask.Delay(500);
    }
}
```

### Bước 4: Xử lý lifecycle khi scene đã load

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        if (payload is ScenePayload p && p.FromLogin)
        {
            // xử lý logic riêng nếu vào từ login
        }

        await SceneFlowManager.Instance.LoadSceneByName(
            "GameplayScene",
            payload: null,
            mode: LoadSceneMode.Additive,
            useManualActivation: true,
            activateDelay: 0.15f);
    }
}
```

## 6) Pipeline và step mặc định

`SceneFlowManager` build pipeline bằng cách ghép `ILoadingStep`.

Chuỗi step mặc định:

1. `InitSDKStep`
2. `LoadSceneStep(InitSdkScene)`
3. `DelegateStep` (load local data)
4. `DelegateStep` (sync cloud data)
5. `DelayStep` (fake/smoothing)
6. `LoadSceneStep(ControllerScene)`
7. `PostInitStep`

## 7) Ví dụ thêm step tùy chỉnh

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;

public sealed class FetchRemoteConfigStep : ILoadingStep
{
    public float Weight => 0.2f;

    public async UniTask Execute(LoadingContext context)
    {
        for (var i = 0; i <= 10; i++)
        {
            context.ReportStepProgress(i / 10f);
            await UniTask.DelayFrame(1, cancellationToken: context.CancellationToken);
        }

        context.Set("remote_config_ready", true);
    }
}
```

## 8) Mô hình progress

- `real progress`: tính theo mức hoàn thành từng step và trọng số (`Weight`).
- `displayed progress`: được làm mượt bằng `ProgressSmoother`.
- `fake progress`: thêm `DelayStep` hoặc report tiến độ nội bộ qua `context.ReportStepProgress(...)`.

## 9) Scene loading nâng cao

`LoadSceneStep` hỗ trợ:

- async loading
- `allowSceneActivation = false` (manual activation)
- `activateDelay`
- cả `LoadSceneMode.Single` và `LoadSceneMode.Additive`

Ví dụ gọi trực tiếp:

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: new ScenePayload { FromLogin = false, IsReload = false },
    mode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

## 10) Event hệ thống

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

`LoadingUIPresenter` subscribe các event này để cập nhật UI.

## 11) Lưu ý quan trọng về `StubLoadingUI`

`StubLoadingUI` có thể bị mất nếu không nằm dưới root DDOL.

Setup an toàn:

1. `BootstrapRoot` chứa `SceneFlowManager` (DDOL).
2. `LoadingUI` là child của `BootstrapRoot`.
3. Không đặt thêm bản sao `SceneFlowManager` ở scene khác.

## 12) Cancellation token theo scene

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## 13) FAQ

### Có thể bỏ qua init SDK khi đi từ màn login không?

Có. Gọi `StartGame(StartGameOptions.WhenFromLogin())` hoặc set `FromLogin = true`.

### Muốn thêm Ads/IAP/Tracking sau này thì làm sao?

Tạo các step riêng implement `ILoadingStep`, sau đó chèn vào pipeline.

### Vì sao progress không nhảy ngay?

Vì progress hiển thị được làm mượt để UX tốt hơn.

## License

MIT
# JIS Loading System

EN: SDK-ready loading system for Unity with modular pipeline steps, event-driven UI, and smooth progress.  
VI: He thong loading san sang cho mo hinh SDK trong Unity, gom cac step mo-rong duoc, UI tach roi bang event, va progress muot.

## Requirements / Yeu cau

- Unity 2022.3+
- [UniTask](https://github.com/Cysharp/UniTask)

## Install / Cai dat

EN: Add to `Packages/manifest.json`.  
VI: Them vao `Packages/manifest.json`.

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Runtime Architecture / Kien truc Runtime

```text
Abstractions/
  ILoadingStep
  ILoadingUI
  ISceneLifecycle

Core/
  LoadingManager           -> pipeline executor root (DDOL)
  LoadingPipelineRunner    -> step runner + weighted progress
  LoadingContext           -> shared data + cancellation + step progress
  LoadingEvents            -> OnStart / OnProgress / OnComplete

Steps/
  InitSDKStep              -> mock SDK init step
  DelayStep                -> fake progress step
  LoadSceneStep            -> async scene load (manual activation supported)
  PostInitStep             -> invoke ISceneLifecycle in loaded scene
  DelegateStep             -> custom inline step

UI/
  LoadingUIPresenter       -> subscribe events, update ILoadingUI
  StubLoadingUI            -> fallback UI implementation

Utils/
  ProgressSmoother         -> displayed progress smoothing
```

EN: High-level modules and responsibilities are shown below.  
VI: Cac module chinh va trach nhiem tuong ung nhu ben duoi.

## Quick Integration (Template) / Tich hop nhanh bang Template

### 1-click setup (recommended) / Cai dat 1 lan nhan (khuyen dung)

EN: Use one of these menus.  
VI: Dung mot trong cac menu sau.

- `Tools > JIS Loading System > Setup Template (1-click)`
- or `Tools > JIS Loading System > Setup Template`
- or `Assets > Create > JIS Loading System > Setup Template`

EN: Template automatically creates.  
VI: Template se tu dong tao.

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` with:
  - `SceneFlowManager`
  - `BootstrapController`
  - child `LoadingUI` + `StubLoadingUI` (already wired to `loadingUIRaw`)
- Example scripts:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`
- Build Settings entries

### Why this template is safe / Vi sao template nay an toan

EN:
- `SceneFlowManager` is `DontDestroyOnLoad`.
- `LoadingUI` is child of `BootstrapRoot`, so it survives `LoadSceneMode.Single`.
- UI is event-driven through `LoadingUIPresenter`; loading logic does not call UI directly.

VI:
- `SceneFlowManager` duoc giu bang `DontDestroyOnLoad`.
- `LoadingUI` la con cua `BootstrapRoot`, nen khong bi mat khi `LoadSceneMode.Single`.
- UI cap nhat qua `LoadingUIPresenter` va event, logic loading khong goi truc tiep UI.

## Manual Integration (Step-by-step) / Tich hop thu cong tung buoc

### 1) Create bootstrap root / Tao bootstrap root

EN: In `BootstrapScene`.  
VI: Trong `BootstrapScene`.

1. Create `BootstrapRoot` GameObject.
2. Add `SceneFlowManager`.
3. Add `BootstrapController` (calls `StartGame()` in `Start`).
4. Create child `LoadingUI` object and add `StubLoadingUI` (or your own UI adapter).
5. Assign that component to `SceneFlowManager.loadingUIRaw`.

### 2) Implement UI adapter (optional but recommended) / Tao UI adapter (tuy chon, nhung nen dung)

```csharp
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MyLoadingUIAdapter : MonoBehaviour, ILoadingUI
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI progressText;
    [SerializeField] private CanvasGroup canvasGroup;

    public void ShowLoadingUI() => canvasGroup.alpha = 1f;
    public void CloseLoadingUI() => canvasGroup.alpha = 0f;
    public void UpdateLoadingBar(float progress) => progressBar.value = progress;
    public void SetLoadingText(int percent) => progressText.text = $"{percent}%";
    public void SetStep(string step) { }
    public void OnChangeScene(float delay, UnityAction action = null) => action?.Invoke();
}
```

### 3) Run flow / Chay flow

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneFlowManager.Instance.StartGame(new StartGameOptions
        {
            Reload = false,
            FromLogin = false,
            OnLoadLocalDataAsync = LoadLocalDataAsync,
            OnSyncCloudDataAsync = SyncCloudDataAsync
        }).Forget();
    }

    private static async UniTask LoadLocalDataAsync()
    {
        await UniTask.Delay(300);
    }

    private static async UniTask SyncCloudDataAsync()
    {
        await UniTask.Delay(500);
    }
}
```

### 4) Handle loaded scene lifecycle / Xu ly lifecycle sau khi scene duoc load

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
{
    public async UniTask OnSceneLoaded(object payload)
    {
        if (payload is ScenePayload p && p.FromLogin)
        {
            // optional logic
        }

        await SceneFlowManager.Instance.LoadSceneByName(
            "GameplayScene",
            payload: null,
            mode: LoadSceneMode.Additive,
            useManualActivation: true,
            activateDelay: 0.15f);
    }
}
```

## Pipeline and Steps / Pipeline va cac Step

EN: `SceneFlowManager` builds a pipeline by composing `ILoadingStep`.  
VI: `SceneFlowManager` tao pipeline bang cach ghep cac `ILoadingStep`.

EN: Default step chain.  
VI: Chuoi step mac dinh.

1. `InitSDKStep`
2. `LoadSceneStep(InitSdkScene)`
3. `DelegateStep` for local data
4. `DelegateStep` for cloud sync
5. `DelayStep` (fake/smoothing support)
6. `LoadSceneStep(ControllerScene)`
7. `PostInitStep`

### Add custom step / Them step tuy chinh

```csharp
using Cysharp.Threading.Tasks;
using Jis.LoadingSystems;
using UnityEngine;

public sealed class FetchRemoteConfigStep : ILoadingStep
{
    public float Weight => 0.2f;

    public async UniTask Execute(LoadingContext context)
    {
        // fake fetch
        for (var i = 0; i <= 10; i++)
        {
            context.ReportStepProgress(i / 10f);
            await UniTask.DelayFrame(1, cancellationToken: context.CancellationToken);
        }

        context.Set("remote_config_ready", true);
    }
}
```

## Progress Model / Mo hinh Progress

EN:
- `real progress`: computed from weighted step completion.
- `displayed progress`: smoothed by `ProgressSmoother` (lerp-like easing).
- `fake progress`: add `DelayStep` or report incremental progress inside each step.

VI:
- `real progress`: tien do thuc tinh theo trong so tung step.
- `displayed progress`: tien do hien thi duoc lam muot boi `ProgressSmoother`.
- `fake progress`: them `DelayStep` hoac bao cao tien do nho ben trong moi step.

## Scene Loading Features / Tinh nang load scene

EN: `LoadSceneStep` supports.  
VI: `LoadSceneStep` ho tro.

- async loading
- `allowSceneActivation = false` (manual activation mode)
- optional activation delay
- both `LoadSceneMode.Single` and `LoadSceneMode.Additive`

## Event-driven UI / UI dua tren event

EN: Global events.  
VI: Event toan cuc.

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

EN: `LoadingUIPresenter` subscribes events and updates `ILoadingUI`.  
VI: `LoadingUIPresenter` lang nghe event va cap nhat `ILoadingUI`.

## StubLoadingUI Lifecycle Note / Luu y vong doi StubLoadingUI

EN: `StubLoadingUI` can be destroyed if not placed under DDOL root.  
VI: `StubLoadingUI` co the bi huy neu khong nam duoi root DDOL.

EN: Safe setup.  
VI: Cach setup an toan.

1. `BootstrapRoot` has `SceneFlowManager` (DDOL).
2. `LoadingUI` is child of `BootstrapRoot`.
3. Do not create duplicate `SceneFlowManager` in other scenes.

## Advanced Usage / Cach dung nang cao

### Load a scene directly with pipeline-safe API / Load scene truc tiep bang API an toan voi pipeline

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: new ScenePayload { FromLogin = false, IsReload = false },
    mode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

### Use cancellation token inside scene / Dung cancellation token trong scene

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## FAQ (EN/VI)

### Q1. Can I skip SDK init for login flow? / Co the bo qua SDK init khi vao tu login khong?

EN: Yes. Call `StartGame(StartGameOptions.WhenFromLogin())` or set `FromLogin = true`.  
VI: Co. Goi `StartGame(StartGameOptions.WhenFromLogin())` hoac set `FromLogin = true`.

### Q2. How do I add Ads/IAP/Tracking later? / Them Ads/IAP/Tracking sau nay nhu the nao?

EN: Add dedicated steps implementing `ILoadingStep` and insert them in the pipeline.  
VI: Tao step rieng implement `ILoadingStep` va chen vao pipeline.

### Q3. Why is progress not jumping instantly? / Vi sao progress khong nhay ngay lap tuc?

EN: Displayed progress is intentionally smoothed for better UX.  
VI: Progress hien thi duoc lam muot co chu y de UX tot hon.

## License / Giay phep

MIT
