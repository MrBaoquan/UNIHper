---
name: unirx-patterns
description: 'UNIHper reactive defaults. Use when the task needs reactive UI bindings, observable composition, lifecycle-managed subscriptions, debounce, throttle, or CompositeDisposable patterns.'
---

# UniRx Patterns

Use this skill when implementing subscriptions, reactive UI bindings, observable-based flow, or lifecycle-managed async logic.

## Default Rules

- Prefer reactive bindings over imperative listener glue when the framework already exposes UniRx helpers.
- Every `Subscribe` must be lifecycle-managed.
- In UGUI pages, bind in `OnShown()` and clear in `OnHidden()`.

## UI Binding

```csharp
Get<Button>("StartButton").OnClickAsObservable()
    .Subscribe(_ => OnStartClicked())
    .AddTo(_disposables);
```

## CompositeDisposable

```csharp
private readonly CompositeDisposable _disposables = new CompositeDisposable();

protected override void OnHidden()
{
    _disposables.Clear();
}
```

## Key-Based Alternatives

```csharp
someObservable.Subscribe(...).DisposeWith("serial-key");
obs1.Subscribe(...).AddTo("group-key");
obs2.Subscribe(...).AddTo("group-key");
```

## Debounce And Throttle

```csharp
inputField.OnValueChangedAsObservable()
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(OnSearch)
    .AddTo(_disposables);
```
