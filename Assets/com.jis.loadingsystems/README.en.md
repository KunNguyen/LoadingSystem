# JIS Loading System (English)

SDK-ready Unity loading system with modular step-based pipeline, event-driven UI, and smooth progress rendering.

Vietnamese default version: `README.md`

## 1) Requirements

- Unity `2022.3+`
- [UniTask](https://github.com/Cysharp/UniTask)

## 2) Install via UPM

Add this to `Packages/manifest.json`:

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

## 4) Quick Integration by Template (recommended)

Use one of these menus:

- `Tools > JIS Loading System > Setup Template (1-click)`
- `Tools > JIS Loading System > Setup Template`
- `Assets > Create > JIS Loading System > Setup Template`

Template creates:

- `BootstrapScene`, `InitSdkScene`, `ControllerScene`, `GameplayScene`
- `BootstrapRoot` containing:
  - `SceneFlowManager`
  - `BootstrapController`
  - child `LoadingUI` + `StubLoadingUI` (already bound to `loadingUIRaw`)
- sample scripts:
  - `InitSdkSceneController_Example.cs`
  - `ControllerSceneController_Example.cs`
- Build Settings entries

Why this setup is safe:

- `SceneFlowManager` is `DontDestroyOnLoad`.
- `LoadingUI` is a child of `BootstrapRoot`, so it survives `LoadSceneMode.Single`.
- Loading logic is UI-agnostic; UI updates are event-driven.

## 5) Manual Integration (step-by-step)

### Step 1: Create bootstrap root

In `BootstrapScene`:

1. Create `BootstrapRoot`.
2. Add `SceneFlowManager`.
3. Add `BootstrapController`.
4. Create child `LoadingUI` and add `StubLoadingUI` (or your own adapter).
5. Assign that component to `SceneFlowManager.loadingUIRaw`.

### Step 2: Implement UI adapter (optional)

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

### Step 3: Start the loading flow

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

### Step 4: Handle loaded scene lifecycle

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

## 6) Default Pipeline

`SceneFlowManager` composes these default steps:

1. `InitSDKStep`
2. `LoadSceneStep(InitSdkScene)`
3. `DelegateStep` (local data)
4. `DelegateStep` (cloud sync)
5. `DelayStep`
6. `LoadSceneStep(ControllerScene)`
7. `PostInitStep`

## 7) Custom Step Example

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

## 8) Progress Model

- `real progress`: weighted completion of all steps.
- `displayed progress`: smoothed output via `ProgressSmoother`.
- `fake progress`: use `DelayStep` or incremental `ReportStepProgress`.

## 9) Advanced Scene Loading

`LoadSceneStep` supports:

- async loading
- `allowSceneActivation = false`
- activation delay
- both `Single` and `Additive`

Direct call example:

```csharp
await SceneFlowManager.Instance.LoadSceneByName(
    "ShopScene",
    payload: new ScenePayload { FromLogin = false, IsReload = false },
    mode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
    useManualActivation: true,
    activateDelay: 0.1f);
```

## 10) Global Events

```csharp
public static class LoadingEvents
{
    public static Action OnStart;
    public static Action<float> OnProgress;
    public static Action OnComplete;
}
```

`LoadingUIPresenter` subscribes to these events and updates `ILoadingUI`.

## 11) `StubLoadingUI` Lifecycle Note

`StubLoadingUI` can be destroyed if it is not under a DDOL root.

Safe setup:

1. Keep `SceneFlowManager` on `BootstrapRoot`.
2. Keep `LoadingUI` as a child of `BootstrapRoot`.
3. Do not duplicate `SceneFlowManager` in other scenes.

## 12) Scene Cancellation Token

```csharp
var ct = SceneFlowManager.Instance.GetCurrentSceneCancellationToken();
await SomeLongTask(ct);
```

## License

MIT
