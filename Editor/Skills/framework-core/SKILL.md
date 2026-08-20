---
name: framework-core
description: 'UNIHper framework core patterns. Use when the task involves framework initialization, idle detection, debug toggles, disposable key management, or HTTP helpers through Managements.Framework and related framework services.'
---

# Framework Core

Use this skill when the task touches application lifecycle, idle handling, debug entrypoints, key-based disposables, or HTTP helpers.

## Default Rules

- Prefer `Managements.Framework` for framework lifecycle concerns.
- Prefer key-based disposable management when a flow should replace or group subscriptions across calls.
- Keep long-time-no-operation behavior in framework or scene startup logic, not in ad-hoc polling code.

## Initialization

```csharp
Managements.Framework.OnInitializedAsObservable()
    .Subscribe(_ => Debug.Log("Framework initialized"))
    .AddTo(_disposables);
```

## Long-Time-No-Operation

```csharp
Managements.Framework.OnLongTimeNoOperationAsObservable()
    .Subscribe(_ => Managements.UI.Show<IdleUI>())
    .AddTo(_disposables);

Managements.Framework.SetLongTimeNoOperationTimeout(120f);
Managements.Framework.ResetLongTimeNoOperation();
Managements.Framework.DisableLongTimeNoOperationAutoReset();
```

## Debug Mode

```csharp
Managements.Framework.OnToggleDebugAsObservable()
    .Subscribe(enabled => Debug.Log($"Debug mode: {enabled}"))
    .AddTo(_disposables);
```

## DisposableManager Keys

### Serial replacement

```csharp
someObservable.Subscribe(...).DisposeWith("request-key");
DisposableManager.Instance.Cancel("request-key");
```

### Composite grouping

```csharp
obs1.Subscribe(...).AddTo("game-loop");
obs2.Subscribe(...).AddTo("game-loop");
DisposableManager.Instance.Dispose("game-loop");
```

## HTTP

```csharp
using UNIHper.Network;

HttpRequest.Get("https://api.example.com/data")
    .Subscribe(response => Debug.Log(response), error => Debug.LogError(error))
    .AddTo(_disposables);
```
