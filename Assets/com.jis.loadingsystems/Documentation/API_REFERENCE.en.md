# API Reference — JIS Loading System

This document explains **what important types do** and what fields like **`ILoadingStep.Weight`** and **`LoadingUIPresenter`** mean.

Vietnamese version: `TAI_LIEU_API.md`

---

## End-to-end flow

1. `SceneFlowManager` (or subclass) builds a `LoadingPipeline` of `ILoadingStep` items.
2. `LoadingManager.RunPipeline` calls `LoadingPipelineRunner.Run`.
3. The runner executes each step and updates **real** progress from `Weight` and `ReportStepProgress`.
4. A separate loop **smooths** displayed progress and raises `LoadingEvents.OnProgress`.
5. `LoadingUIPresenter` listens and drives `ILoadingUI` (bar, percent text).

Loading logic **does not** call UI directly; only `LoadingEvents`.

---

## `ILoadingStep`

**Role:** One unit of work in the pipeline.

| Member | Meaning |
|--------|---------|
| `Weight` | **Relative weight** of this step in the whole pipeline — **not** a fixed percentage. Total weight = sum of all step weights; each step owns a **segment** of the 0→1 bar with length `Weight / total`. Two steps with the same `Weight` contribute **equal** share of the overall bar; double `Weight` → double segment length. |
| `Execute(LoadingContext context)` | Async work. Prefer `context.CancellationToken` on awaitables. |

**`ReportStepProgress`:** Call `context.ReportStepProgress(p)` with `p` in `[0, 1]` for **intra-step** progress. The runner maps `p` into the current step’s segment. If you never call it, the step still jumps to the end of its segment when `Execute` completes.

**Tip:** Default steps use small weights (0.05–0.35); adjust freely as long as each effective weight stays positive (runner clamps a minimum to avoid divide-by-zero).

---

## `LoadingContext`

**Role:** Shared state for **one** pipeline run.

| Member | Meaning |
|--------|---------|
| `Payload` | Arbitrary payload (e.g. `ScenePayload`). |
| `CancellationToken` | Pipeline/step cancellation (set by `LoadingManager` when running). |
| Flags | `IsLoggedIn`, `IsReload`, `FromLogin`, `CloudDataAvailable`. |
| `Set` / `TryGet` | Key-value bag between steps. |
| `ReportStepProgress` | Intra-step 0→1, see `ILoadingStep`. |

---

## `LoadingPipeline`

**Role:** Ordered list of steps with optional **string keys** for insert/replace/remove.

See `AddStep`, `InsertBefore`, `InsertAfter`, `ReplaceStep`, `RemoveStep`, `Steps`.

Default keys live on `SceneFlowManager` (`StepKeyInitSdk`, …).

---

## `LoadingPipelineRunner`

**Role:** Runs steps sequentially; bridges **real** progress (`_realProgress`) and **displayed** progress via `ProgressSmoother`, then `LoadingEvents.RaiseProgress`.

Empty step list → Start → Progress(1) → Complete.

---

## `ProgressSmoother`

**Role:** Easing-style smoothing so the UI bar does not jitter (`Next(current, target, deltaTime)`).

---

## `LoadingManager`

**Role:** Base MonoBehaviour: singleton, `DontDestroyOnLoad`, owns `LoadingPipelineRunner`, run/cancel pipeline.

| Member | Meaning |
|--------|---------|
| `progressSmoothingSpeed` | How fast displayed progress catches the real target (Inspector). |
| `RunPipeline` | Cancels previous run, creates new CTS (may link external token), runs runner. |
| `CancelCurrentPipeline` | Cancel + dispose CTS. |

---

## `LoadingEvents`

**Role:** Static events between core and UI (or anything else).

| Event | When |
|-------|------|
| `OnStart` | Pipeline with steps starts. |
| `OnProgress(float)` | **Smoothed** display progress, usually 0→1. |
| `OnComplete` | Pipeline finished. |
| `OnStepStarted(LoadingStepInfo)` | Right **before** each step’s `Execute`. |
| `OnStepCompleted(LoadingStepInfo)` | After `Execute` returns; still called in `finally` if the step throws. |

`LoadingStepInfo`: `Index` (0-based), `Total`, `StepTypeName` (concrete `ILoadingStep` type name).

**Inside a step:** “start” = top of `Execute`; “end” = when `Execute` returns (or use internal `try/finally`). For **global** per-step hooks, subscribe to `OnStepStarted` / `OnStepCompleted`.

---

## `LoadingUIPresenter`

**Role:** **Presentation layer** — subscribes to `LoadingEvents` and drives one `ILoadingUI`.

| Member | Meaning |
|--------|---------|
| `loadingUIRaw` | `MonoBehaviour` implementing `ILoadingUI` (Inspector). |
| `SetLoadingUI` | Assign UI from code. |

**Behavior:**

- `OnStart` → `ShowLoadingUI()` + progress 0.
- `OnProgress` → `UpdateLoadingBar` + `SetLoadingText` (percent from 0–100).
- `OnComplete` → `CloseLoadingUI()`.

**Note:** `SetStep` on `ILoadingUI` is **not** called by this presenter in the current build; extend the presenter or add your own listeners if you need step labels.

---

## `ILoadingUI` and `StubLoadingUI`

- **`ILoadingUI`:** Contract for your real loading HUD.
- **`StubLoadingUI`:** Minimal implementation for pipeline testing.

---

## `SceneFlowManager` (Inspector groups)

| Group | Fields | Meaning |
|-------|--------|---------|
| UI | `loadingUIRaw`, `loadingUIPresenter` | UI reference; presenter auto-added if missing. |
| Scenes | `initSdkScene`, `controllerScene` | Names for default pipeline. |
| Loading | `controllerSceneMode`, `manualSceneActivation`, `activationDelaySeconds`, `fakeDelaySeconds` | Async load behavior and fake delay step. |

---

## Built-in steps (`Runtime/Steps`)

| Type | Purpose |
|------|---------|
| `InitSDKStep` | Mock SDK init; extend for real SDKs. |
| `DelayStep` | Timed wait with progress — fake loading. |
| `LoadSceneStep` | Async scene load, optional manual activation. |
| `PostInitStep` | Calls `ISceneLifecycle.OnSceneLoaded` in loaded scene. |
| `DelegateStep` | Wraps `Func<LoadingContext, UniTask>` as a step. |

---

## `SceneCancellationManager`

Maps scene name → cancellation when changing scenes (especially `Single` loads).

---

## `ISceneLifecycle`

`PostInitStep` discovers implementations and calls `OnSceneLoaded(payload)` after the target scene is loaded.

---

## `StartGameOptions` / `ScenePayload`

Boot options and small payload attached to `LoadingContext.Payload`.

---

## Cheat sheet: `Weight` vs displayed bar

| Concept | Source |
|---------|--------|
| Each step’s share of the global bar | `Weight / sum(Weights)` |
| Detail inside one step | `ReportStepProgress(0..1)` |
| What the player sees | Smoothed → `OnProgress` → `LoadingUIPresenter` → `ILoadingUI` |

---

## Troubleshooting: progress never updates

### 1) Wrong `Loading UI Raw` reference (most common)

**`Loading UI Raw` on `SceneFlowManager` must reference a component that implements `ILoadingUI`** (e.g. your `UILoadingAdapter`, `StubLoadingUI`).

- **Wrong:** assigning `UILoading (Image)` or `Canvas` — they are not `ILoadingUI` → presenter has no UI → `HandleProgress` returns immediately.
- **Right:** pick the GameObject, then choose the **`UILoadingAdapter`** component in the object picker, not `Image`.

Newer package versions: if you assigned another component on the **same** GameObject (e.g. `Image` while `UILoadingAdapter` is on the same object), runtime will try `GetComponent<ILoadingUI>()` on that object. If it still fails, check the Console for an error with fix instructions.

### 2) `Loading UI Presenter` is None

That is OK: `SceneFlowManager` adds `LoadingUIPresenter` on the manager GameObject at runtime. Fix the `Loading UI Raw` reference first.

### 3) Slider range

Pipeline sends progress in **0→1**. Default Unity Slider 0–1 matches. If you changed min/max, scale in `UpdateLoadingBar`.

---

## License

MIT
