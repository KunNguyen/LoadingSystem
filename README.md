# LoadingSystem

Reusable Unity loading SDK published via UPM.

## Install via UPM

```json
{
  "dependencies": {
    "com.jis.loadingsystem": "https://github.com/KunNguyen/LoadingSystem.git?path=Assets/com.jis.loadingsystems",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## Documentation

Detailed integration guide, template setup, architecture, and full examples:

- `Assets/com.jis.loadingsystems/README.md`

## Quick Start

1. Install package from UPM URL above.
2. Run `Tools > JIS Loading System > Setup Template (1-click)`.
3. Open generated `BootstrapScene`.
4. Press Play.

Template now creates `BootstrapRoot` with `SceneFlowManager` and child `LoadingUI`, so UI persists correctly across scene changes.
