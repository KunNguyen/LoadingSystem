# JIS Loading System

SDK-ready loading system for Unity with modular pipeline steps, event-driven UI, and smooth progress.

## Requirements

- Unity 2022.3+
- [UniTask](https://github.com/Cysharp/UniTask)

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Runtime Architecture

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

## Quick Integration (Template)

### 1-click setup (recommended)

Use menu:

- `Tools > JIS Loading System > Setup Template (1-click)`
- or `Tools > JIS Loading System > Setup Template`
- or `Assets > Create > JIS Loading System > Setup Template`

Template now creates:

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` with:
  - `SceneFlowManager`
  - `BootstrapController`
  - child `LoadingUI` + `StubLoadingUI` (already wired to `loadingUIRaw`)
- Example scripts:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`
- Build Settings entries

### Why this template is safe

- `SceneFlowManager` is `DontDestroyOnLoad`.
- `LoadingUI` is created as child of `BootstrapRoot`, so it survives `LoadSceneMode.Single`.
- UI is event-driven through `LoadingUIPresenter`, no direct UI call in loading steps.

## Manual Integration (Step-by-step)

### 1) Create bootstrap root

In `BootstrapScene`:

1. Create `BootstrapRoot` GameObject.
2. Add `SceneFlowManager`.
3. Add `BootstrapController` (calls `StartGame()` in `Start`).
4. Create child `LoadingUI` object and add `StubLoadingUI` (or your own UI adapter).
5. Assign that component to `SceneFlowManager.loadingUIRaw`.

### 2) Implement UI adapter (optional but recommended)

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

### 3) Run flow

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

### 4) Handle loaded scene lifecycle

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

## Pipeline and Steps

`SceneFlowManager` builds pipeline by composing `ILoadingStep`.

Default step chain:

1. `InitSDKStep`
2. `LoadSceneStep(InitSdkScene)`
3. `DelegateStep` for local data
4. `DelegateStep` for cloud sync
5. `DelayStep` (fake/smoothing support)
6. `LoadSceneStep(ControllerScene)`
7. `PostInitStep`

### Add custom step

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

## Progress Model

- `real progress`: computed by weighted step completion.
- `displayed progress`: smoothed by `ProgressSmoother` (lerp-like easing).
- `fake progress`: add `DelayStep` or report incremental progress from each step.

## Scene Loading Features

`LoadSceneStep` supports:

- async loading
- `allowSceneActivation = false` (manual activation mode)
- optional activation delay
- both `LoadSceneMode.Single` and `LoadSceneMode.Additive`

## Event-driven UI

Global events:

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

`LoadingUIPresenter` subscribes these events and updates `ILoadingUI`.

## StubLoadingUI Lifecycle Note

`StubLoadingUI` can be destroyed if not placed under DDOL root.

Safe setup:

1. `BootstrapRoot` has `SceneFlowManager` (DDOL).
2. `LoadingUI` is child of `BootstrapRoot`.
3. Do not create duplicate `SceneFlowManager` in other scenes.

## Advanced Usage

### Load a scene directly with pipeline-safe API

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: new ScenePayload { FromLogin = false, IsReload = false },
    mode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

### Use cancellation token inside scene

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## License

MIT
