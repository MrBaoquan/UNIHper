---
name: create-ugui-page
description: 'Create a new UGUI page script aligned with UNIHper Editor templates. Use when asked to create a UI page, panel, popup, or other UGUI component based on UIBase.'
---

# Create UGUI Page

Use this skill when creating a new UGUI page.

## Alignment Rule

- Follow the UNIHper Editor UI script template.
- Do not add `Start()` or `Update()` by default.
- Use `OnLoaded()` for one-time setup, `OnShown()` for subscriptions, and `OnHidden()` for cleanup.

## Default Skeleton

```csharp
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UNIHper;
using UNIHper.UI;
using TMPro;

[UIPage(Asset = "{AssetName}", Type = UIType.Normal, Order = -1)]
public class {ClassName} : UIBase
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    protected override void OnLoaded()
    {
        // Cache child references with Get<T>("path") when needed.
    }

    protected override void OnShown()
    {
        // Bind reactive events here.
    }

    protected override void OnHidden()
    {
        _disposables.Clear();
    }
}
```

## Creation Notes

- File name should follow `{Feature}UI.cs`.
- Keep the asset name and class name aligned unless the task explicitly requires otherwise.
- Prefer `Get<T>("path")` and consult `ugui-prefabs` when the path is unclear.
