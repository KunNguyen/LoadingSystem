# LoadingSystem (Tiếng Việt)

Repository này chứa Unity package `com.jis.loadingsystem`, phát hành qua UPM.

English version: `README.en.md`

## Cài đặt qua UPM

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Tài liệu chi tiết

- Bản tiếng Việt (mặc định): `Assets/com.jis.loadingsystems/README.md`
- Bản tiếng Anh: `Assets/com.jis.loadingsystems/README.en.md`

## Bắt đầu nhanh

1. Cài package bằng URL UPM ở trên.
2. Chạy `Tools > JIS Loading System > Setup Template (1-click)`.
3. Mở `BootstrapScene` được tạo tự động.
4. Nhấn Play để test flow.

Template đã được cấu hình để tạo `BootstrapRoot` + `LoadingUI` là object con, nên UI không bị mất khi chuyển scene kiểu `Single`.
