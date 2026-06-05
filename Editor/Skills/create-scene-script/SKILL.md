---
name: create-scene-script
description: 'Create a new scene script aligned with UNIHper Editor templates. Use when asked to create scene initialization logic, scene entry points, or SceneScriptBase-based lifecycle code.'
---

# Create Scene Script

Use this skill when creating a new scene entry script.

## Alignment Rule

- Follow the UNIHper Editor scene script template.
- Do not add `Update()` by default.
- Use `OnSceneReadyAsObservable()` before adding startup logic that depends on initialized managers.

## Default Skeleton

```csharp
using UniRx;
using UnityEngine;
using UNIHper;

public class Scene{SceneName}Script : SceneScriptBase
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private void Awake()
    {
    }

    private void Start()
    {
        // OnSceneReadyAsObservable().Subscribe(_ => { }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void OnApplicationQuit()
    {
    }
}
```

## Creation Notes

- File name must match the scene name convention: `Scene{Name}Script.cs`.
- Lifecycle methods are invoked through framework conventions; keep them `private`.
- Add `Update()` only when the behavior is truly frame-driven.
