---
name: framework-core
description: 'UNIHper framework core patterns. Use when the task involves framework initialization, idle detection, debug toggles, disposable key management, or HTTP helpers through Managements.Framework and related framework services.'
---

# Framework Core

Use this skill when the task touches application lifecycle, idle handling, debug entrypoints, key-based disposables, or HTTP helpers.

## Implementation Stance

- Default implementation stance: solve with UNIHper first, not with raw Unity patterns first.
- Treat UNIHper Editor templates and framework base classes as the default skeleton source for new code.

## Default Rules

- Prefer `Managements.Framework` for framework lifecycle concerns.
- Prefer `Managements.*` facades when an equivalent framework capability exists.
- Prefer UniRx and `IObservable<T>` for async flow, event handling, and UI bindings.
- Prefer `Managements.Timer` for delay, interval, countdown, throttle, and debounce.
- Prefer `Managements.Event` for cross-component communication.
- All `Subscribe` calls must be lifecycle-managed (`AddTo` / `DisposeWith`).
- Keep long-time-no-operation behavior in framework or scene startup logic, not in ad-hoc polling code.

## Avoid By Default

- Do not introduce `IEnumerator` / `StartCoroutine` unless Unity API usage requires it.
- Do not introduce polling-style `Update()` logic unless the behavior is truly frame-driven.
- Do not introduce `UnityEvent` or `SendMessage` for project logic.

## Naming Conventions

- UGUI page: `{Feature}UI.cs`
- UI Toolkit page: `{Feature}ToolkitUI.cs`
- Scene script: `Scene{Name}Script.cs`
- Config: `{Feature}Config.cs`
- Event: `{Action}Event.cs`

## Constraint Order

1. Project identity and hard defaults from workspace instructions (`.github/instructions/`).
2. Domain skills under `.github/skills/` (including this skill).
3. Project-specific deltas from `project-memory`.
4. UNIHper Editor templates and framework base-class contracts are the final implementation source of truth.

If skill guidance conflicts with editor templates or base-class contracts, follow the editor template and the actual framework API, then update the skill to match.

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
