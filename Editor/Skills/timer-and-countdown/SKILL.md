---
name: timer-and-countdown
description: 'UNIHper timer defaults. Use when the task needs delay, interval, countdown, timeout, throttle, debounce, or key-cancellable time-based behavior through Managements.Timer.'
---

# Timer And Countdown

Use this skill when the task needs time-based behavior. Default to `Managements.Timer` before considering coroutines.

## Default Rules

- Prefer `Managements.Timer` over `StartCoroutine` for delay, interval, timeout, throttle, or debounce.
- Use keyed timers when repeated calls should cancel or replace earlier pending work.

## Delay

```csharp
Managements.Timer.Delay(2f, () => ShowResult());
Managements.Timer.Delay(2f, () => ShowResult(), "show-result");
Managements.Timer.Cancel("show-result");
```

## Interval

```csharp
Managements.Timer.Interval(1f, () => Debug.Log("Tick"));
```

## Countdown

```csharp
var countdown = Managements.Timer.Countdown(10f, tickInterval: 1f);
countdown.OnUpdate(remaining => timerLabel.text = $"{remaining:F0}")
    .OnComplete(() => Debug.Log("Countdown complete"))
    .Start();
```

## Debounce And Throttle

```csharp
var debounced = Managements.Timer.Debounce(0.3f, RefreshSearch);
var throttled = Managements.Timer.Throttle(0.5f, RefreshSearch);
```
