# JIS Loading System (English)

SDK-ready Unity loading system with modular, step-based pipeline customization.

Vietnamese default version: `README.md`

## API documentation (classes, `Weight`, `LoadingUIPresenter`, …)

- **English:** `Documentation/API_REFERENCE.en.md`
- **Vietnamese:** `Documentation/TAI_LIEU_API.md`

## Fastest way to customize pipeline

You do not need to rebuild the whole flow.  
Subclass `SceneFlowManager` and override `CustomizePipeline(...)`.

### Built-in default step keys

- `SceneFlowManager.StepKeyInitSdk`
- `SceneFlowManager.StepKeyLoadInitScene`
- `SceneFlowManager.StepKeyLoadLocalData`
- `SceneFlowManager.StepKeySyncCloudData`
- `SceneFlowManager.StepKeyFakeDelay`
- `SceneFlowManager.StepKeyLoadControllerScene`
- `SceneFlowManager.StepKeyPostInit`

### Pipeline editing API

- `AddStep(step, key)`
- `InsertBefore(anchorKey, step, key)`
- `InsertAfter(anchorKey, step, key)`
- `ReplaceStep(key, newStep, newKey)`
- `RemoveStep(key)`

### Ready-to-use example

Included sample file:

- `Runtime/Examples/MySceneFlowManagerExample.cs`

Replace `SceneFlowManager` in your `BootstrapScene` with `MySceneFlowManagerExample`.
