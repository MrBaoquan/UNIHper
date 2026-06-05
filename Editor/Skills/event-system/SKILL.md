---
name: event-system
description: 'UNIHper event defaults. Use when the task needs publish-subscribe communication, custom UEvent definitions, or decoupled component interaction through Managements.Event.'
---

# Event System

Use this skill when components need to communicate without direct references.

## Default Rules

- Prefer `Managements.Event` for cross-component communication.
- Keep event types small and explicit.
- Always pair register and unregister calls.

## Define Events

```csharp
using UNIHper;

public class ScoreChangedEvent : UEvent
{
    public int NewScore;
    public int OldScore;
}
```

## Register, Fire, Unregister

```csharp
Managements.Event.Register<ScoreChangedEvent>(OnScoreChanged);
Managements.Event.Fire(new ScoreChangedEvent { NewScore = 100 });
Managements.Event.Unregister<ScoreChangedEvent>(OnScoreChanged);
```

## Common Placement

```csharp
protected override void OnShown()
{
    Managements.Event.Register<ScoreChangedEvent>(OnScoreChanged);
}

protected override void OnHidden()
{
    Managements.Event.Unregister<ScoreChangedEvent>(OnScoreChanged);
}
```
