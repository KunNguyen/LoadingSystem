# JIS Loading System (Tiếng Việt)

Hệ thống loading dạng SDK cho Unity, hỗ trợ pipeline theo step, UI tách rời bằng event, và tiến độ mượt.

English version: `README.en.md`

## Tài liệu API (giải thích class, `Weight`, `LoadingUIPresenter`, …)

- **Tiếng Việt:** `Documentation/TAI_LIEU_API.md`
- **English:** `Documentation/API_REFERENCE.en.md`

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

## Cài đặt UPM

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Template nhanh

`Tools > JIS Loading System > Setup Template (1-click)`

## License

MIT
