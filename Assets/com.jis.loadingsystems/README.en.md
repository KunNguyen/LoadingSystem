# JIS Loading System (English)

SDK-ready Unity loading system with modular step-based pipeline, event-driven UI, and smooth progress rendering.

Vietnamese default version: `README.md`

## 1) Requirements

- Unity `2022.3+`
- [UniTask](https://github.com/Cysharp/UniTask)

## 2) Install via UPM

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## 3) Runtime Architecture

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

## 4) Quick Setup via Template

Menu:

- `Tools > JIS Loading System > Setup Template (1-click)`
- or `Tools > JIS Loading System > Setup Template`
- or `Assets > Create > JIS Loading System > Setup Template`

## 5) Basic Usage

```csharp
SceneFlowManager.Instance.StartGame(new StartGameOptions
{
    Reload = false,
    FromLogin = false,
    OnLoadLocalDataAsync = LoadLocalDataAsync,
    OnSyncCloudDataAsync = SyncCloudDataAsync
}).Forget();
```

## 6) Easiest Pipeline Customization (recommended)

You do not need to rewrite the whole pipeline.  
Subclass `SceneFlowManager` and override `CustomizePipeline(...)`.

Built-in default step keys:

- `SceneFlowManager.StepKeyInitSdk`
- `SceneFlowManager.StepKeyLoadInitScene`
- `SceneFlowManager.StepKeyLoadLocalData`
- `SceneFlowManager.StepKeySyncCloudData`
- `SceneFlowManager.StepKeyFakeDelay`
- `SceneFlowManager.StepKeyLoadControllerScene`
- `SceneFlowManager.StepKeyPostInit`

`LoadingPipeline` customization API:

- `AddStep(step, key)`
- `InsertBefore(anchorKey, step, key)`
- `InsertAfter(anchorKey, step, key)`
- `ReplaceStep(key, newStep, newKey)`
- `RemoveStep(key)`

Example:

```csharp
public class MySceneFlowManager : SceneFlowManager
{
    protected override void CustomizePipeline(
        LoadingPipeline pipeline,
        LoadingContext context,
        StartGameOptions options)
    {
        pipeline.InsertBefore(
            StepKeyLoadLocalData,
            new FetchRemoteConfigStep(),
            key: "fetch-remote-config");

        pipeline.ReplaceStep(
            StepKeyFakeDelay,
            new DelayStep(0.6f, 0.08f));

        if (Debug.isDebugBuild)
            pipeline.RemoveStep(StepKeySyncCloudData);

        pipeline.InsertAfter(
            StepKeyLoadControllerScene,
            new PostWarmupStep(),
            key: "post-warmup");
    }
}
```

## 7) Custom Step Example

```csharp
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

## License

MIT
