# LoadingSystem (English)

This repository contains the Unity package `com.jis.loadingsystem`, distributed via UPM.

Vietnamese default version: `README.md`

## Install via UPM

Add this to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Detailed Documentation

- Vietnamese (default): `Assets/com.jis.loadingsystems/README.md`
- English: `Assets/com.jis.loadingsystems/README.en.md`

## Quick Start

1. Install package via UPM URL above.
2. Run `Tools > JIS Loading System > Setup Template (1-click)`.
3. Open generated `BootstrapScene`.
4. Press Play.

The template creates `BootstrapRoot` with child `LoadingUI`, so UI persists across `Single` scene transitions.
