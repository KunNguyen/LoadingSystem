# Tài liệu API — JIS Loading System

Tài liệu này giải thích **chức năng** của các class / interface quan trọng và **ý nghĩa** của các thuộc tính hay dùng (ví dụ `Weight` trong `ILoadingStep`, vai trò của `LoadingUIPresenter`).

English version: `API_REFERENCE.en.md`

---

## Luồng dữ liệu tổng quan

1. `SceneFlowManager` (hoặc subclass) dựng `LoadingPipeline` gồm nhiều `ILoadingStep`.
2. `LoadingManager.RunPipeline` gọi `LoadingPipelineRunner.Run`.
3. Runner chạy từng step, cập nhật **tiến độ thực** (`_realProgress`) theo `Weight` và `ReportStepProgress`.
4. Một vòng lặp riêng làm **mượt** tiến độ hiển thị rồi bắn `LoadingEvents.OnProgress`.
5. `LoadingUIPresenter` lắng nghe event và gọi `ILoadingUI` (thanh %, text…).

Logic loading **không** gọi trực tiếp UI; chỉ thông qua `LoadingEvents`.

---

## `ILoadingStep`

**Vai trò:** Một bước trong pipeline (init SDK, load scene, load data, delay giả, v.v.).

| Thành phần | Ý nghĩa |
|------------|---------|
| `Weight` | **Trọng số tương đối** của step trong toàn pipeline. Không phải phần trăm cố định. Hệ thống tính `tổngWeight = sum(Weight)`; mỗi step chiếm một **đoạn** trên thanh 0→1 có độ dài `Weight / tổngWeight`. Hai step cùng `Weight` đóng góp **bằng nhau** về mặt “quota” progress; step có `Weight` gấp đôi sẽ chiếm đoạn dài gấp đôi trên thanh tổng. |
| `Execute(LoadingContext context)` | Chạy async logic của step. Nên dùng `context.CancellationToken` cho mọi `UniTask` có thể hủy. |

**`ReportStepProgress` bên trong step:**  
Gọi `context.ReportStepProgress(p)` với `p` trong `[0, 1]` để báo tiến độ **bên trong** step hiện tại. Runner map `p` vào đoạn progress tương ứng với `Weight` của step đó. Nếu không gọi, step vẫn được coi là xong 100% khi `Execute` kết thúc (tiến độ nhảy lên cuối đoạn của step).

**Gợi ý:** Các step mặc định dùng `Weight` nhỏ (0.05–0.35) để tổng dễ cân; bạn có thể đổi tỷ lệ miễn đủ `Weight > 0` (runner clamp tối thiểu nhỏ để tránh chia cho 0).

---

## `LoadingContext`

**Vai trò:** Dữ liệu dùng chung cho mọi step trong **một lần** chạy pipeline.

| Thành phần | Ý nghĩa |
|------------|---------|
| `Payload` | Dữ liệu truyền kèm (ví dụ `ScenePayload` khi boot). |
| `CancellationToken` | Token hủy cho pipeline / step (do `LoadingManager` gán khi chạy). |
| `IsLoggedIn`, `IsReload`, `FromLogin`, `CloudDataAvailable` | Cờ nghiệp vụ; pipeline mặc định có thể set / đọc. |
| `Set<T>(key, value)` / `TryGet<T>(key, out)` | Lưu trữ key-value giữa các step (ví dụ `remote_config_ready`). |
| `ReportStepProgress(float)` | Báo tiến độ nội bộ step (0→1), xem phần `ILoadingStep`. |

---

## `LoadingPipeline`

**Vai trò:** Danh sách có thứ tự các `ILoadingStep`, có thể gắn **key** để chèn / thay / xóa.

| API | Chức năng |
|-----|-----------|
| `AddStep(step, key)` | Thêm step cuối danh sách. |
| `InsertBefore/After(anchorKey, step, key)` | Chèn relative theo key step có sẵn. |
| `ReplaceStep(key, newStep, newKey)` | Thay step đã có key. |
| `RemoveStep(key)` | Xóa step theo key. |
| `Steps` | Trả về danh sách step thuần để chạy. |

Key mặc định do `SceneFlowManager` định nghĩa: `StepKeyInitSdk`, `StepKeyLoadInitScene`, …

---

## `LoadingPipelineRunner`

**Vai trò:** Thực thi tuần tự các step và điều phối **tiến độ thực** + **tiến độ hiển thị**.

- **Tiến độ thực (`_realProgress`):** Tính từ `Weight` và `ReportStepProgress` (xem `ILoadingStep`).
- **Tiến độ hiển thị:** Đuổi theo tiến độ thực bằng `ProgressSmoother` (hàm mượt kiểu exponential lerp), rồi gọi `LoadingEvents.RaiseProgress(displayed)`.

Khi không có step nào: bắn Start → Progress(1) → Complete.

---

## `ProgressSmoother`

**Vai trò:** Làm mượt giá trị hiển thị theo thời gian (`Next(current, target, deltaTime)`), tránh thanh loading giật.

---

## `LoadingManager`

**Vai trò:** MonoBehaviour gốc: singleton (mặc định), `DontDestroyOnLoad`, tạo `LoadingPipelineRunner`, chạy / hủy pipeline.

| Field / API | Ý nghĩa |
|-------------|---------|
| `progressSmoothingSpeed` | Tốc độ làm mượt thanh (Inspector). Càng lớn càng “bám” nhanh target. |
| `RunPipeline(steps, context)` | Hủy pipeline cũ (nếu có), tạo `CancellationTokenSource` mới (có thể link với token sẵn có trong context), chạy runner. |
| `CancelCurrentPipeline()` | Hủy và giải phóng CTS. |

---

## `LoadingEvents`

**Vai trò:** Static event — cầu nối giữa **core loading** và **UI** (hoặc bất kỳ listener nào).

| Event | Khi nào |
|-------|---------|
| `OnStart` | Bắt đầu chạy pipeline (có step). |
| `OnProgress(float)` | Tiến độ **hiển thị** đã mượt, thường 0→1. |
| `OnComplete` | Pipeline kết thúc (thành công, sau khi pump progress về 1). |

Bạn có thể subscribe thêm từ code khác (analytics, debug overlay) mà không sửa runner.

---

## `LoadingUIPresenter`

**Vai trò:** **Lớp presentation** — subscribe `LoadingEvents` và điều khiển một implementation `ILoadingUI`.

| Thành phần | Ý nghĩa |
|-------------|---------|
| `loadingUIRaw` | Reference `MonoBehaviour` implement `ILoadingUI` (Inspector). |
| `SetLoadingUI(ILoadingUI)` | Gán UI từ code (ví dụ sau khi spawn prefab). |
| `OnEnable` / `OnDisable` | Đăng ký / hủy đăng ký event — tránh leak khi tắt object. |

**Hành vi:**

- `OnStart` → `ShowLoadingUI()` + reset progress 0.
- `OnProgress` → `UpdateLoadingBar(progress)` + `SetLoadingText(floor(progress*100))`.
- `OnComplete` → `CloseLoadingUI()`.

**Lưu ý:** `SetStep` trong interface **không** được presenter gọi trong bản hiện tại; nếu cần hiển thị tên bước, bạn có thể mở rộng presenter hoặc subscribe `OnProgress` kết hợp logic riêng.

---

## `ILoadingUI` và `StubLoadingUI`

**`ILoadingUI`:** Hợp đồng UI loading (show/hide, thanh %, text %, bước, hook đổi scene).

**`StubLoadingUI`:** Bản tối giản — chủ yếu show/hide GameObject; các method khác no-op hoặc đơn giản. Dùng để test pipeline không cần UI thật.

---

## `SceneFlowManager` : các nhóm field Inspector

| Nhóm | Field | Ý nghĩa |
|------|-------|---------|
| UI | `loadingUIRaw` | Component implement `ILoadingUI` (thường child dưới cùng root DDOL). |
| UI | `loadingUIPresenter` | Optional; nếu null, `SceneFlowManager` tự thêm `LoadingUIPresenter` trên cùng GameObject. |
| Scene | `initSdkScene`, `controllerScene` | Tên scene cho pipeline mặc định. |
| Scene load | `controllerSceneMode` | `Single` hoặc `Additive` cho bước vào controller. |
| Scene load | `manualSceneActivation` | `true` → dùng `allowSceneActivation = false` rồi bật sau delay (tránh flash / chờ sẵn sàng). |
| Scene load | `activationDelaySeconds` | Delay trước khi `allowSceneActivation = true`. |
| Scene load | `fakeDelaySeconds` | Thời gian cho `DelayStep` mặc định (fake / UX). |

**API quan trọng:** `StartGame`, `BuildPipeline`, `CustomizePipeline`, `LoadSceneByName`, token scene qua `SceneCancellationManager`.

---

## Các step có sẵn (Runtime/Steps)

| Class | Chức năng ngắn |
|-------|----------------|
| `InitSDKStep` | Mock / placeholder init SDK; có thể mở rộng cho Ads, IAP, Analytics. |
| `DelayStep` | Chờ theo thời gian, báo progress dần — phù hợp “fake loading”. |
| `LoadSceneStep` | `LoadSceneAsync`, hỗ trợ manual activation, `Single`/`Additive`. |
| `PostInitStep` | Sau khi scene load, gọi `ISceneLifecycle.OnSceneLoaded` trên root objects trong scene đích. |
| `DelegateStep` | Gói `Func<LoadingContext, UniTask>` thành một step có `Weight`. |

---

## `SceneCancellationManager`

**Vai trò:** Map tên scene → `CancellationToken` để hủy task khi đổi scene (đặc biệt khi load `Single`).

---

## `ISceneLifecycle`

**Vai trò:** Callback sau khi scene đã load và (trong flow pipeline) `PostInitStep` quét `GetComponentsInChildren` để gọi `OnSceneLoaded(payload)`.

---

## `StartGameOptions` và `ScenePayload`

- **`StartGameOptions`:** `Reload`, `FromLogin`, delegate `OnLoadLocalDataAsync`, `OnSyncCloudDataAsync`.
- **`ScenePayload`:** Dữ liệu nhẹ gắn `LoadingContext.Payload` khi boot (reload, from login, …).

---

## Tóm tắt: `Weight` vs tiến độ hiển thị

| Khái niệm | Nguồn |
|------------|--------|
| Phần “ô” trên thanh tổng của mỗi step | `Weight / sum(Weight)` |
| Tiến độ chi tiết trong một ô | `ReportStepProgress(0..1)` |
| Thanh người chơi thấy | Đã qua `ProgressSmoother` → `LoadingEvents.OnProgress` → `LoadingUIPresenter` → `ILoadingUI` |

---

## Xử lý sự cố: progress / text không đổi

### 1) Gán nhầm `Loading UI Raw` (hay gặp nhất)

Trường **`Loading UI Raw`** trên `SceneFlowManager` phải trỏ tới component **implement `ILoadingUI`** (ví dụ `UILoadingAdapter`, `StubLoadingUI`).

- **Sai:** kéo `UILoading (Image)` hoặc `Canvas` — những component này **không** phải `ILoadingUI` → `LoadingUIPresenter` coi như không có UI → `HandleProgress` return sớm, **không cập nhật Slider/Text**.
- **Đúng:** chọn object `UILoading`, ở ô reference click **selector**, chọn script **`UILoadingAdapter`** (hoặc đúng script implement `ILoadingUI`), không chọn `Image`.

Từ bản code mới: nếu bạn gán nhầm component cùng GameObject (ví dụ gán `Image` nhưng cùng object có `UILoadingAdapter`), runtime sẽ **tự tìm** `ILoadingUI` trên cùng GameObject. Nếu vẫn lỗi, Console sẽ có log hướng dẫn.

### 2) `Loading UI Presenter` để `None`

Không sao: `SceneFlowManager` sẽ **tự thêm** `LoadingUIPresenter` lên cùng GameObject với manager khi chạy. Bạn chỉ cần gán đúng `Loading UI Raw` như trên.

### 3) Slider / giá trị

Hệ thống gửi progress **0→1**. Slider Unity mặc định min/max 0–1 là khớp. Nếu bạn đổi min/max Slider, cần scale lại trong `UpdateLoadingBar`.

---

## License

MIT
