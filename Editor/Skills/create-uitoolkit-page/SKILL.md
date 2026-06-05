---
name: create-uitoolkit-page
description: 'Create a new UI Toolkit page aligned with UNIHper Editor templates. Use when asked to create a UIToolkit page, UIDocument-based interface, or UXML-driven page based on UIToolkitBase.'
---

# Create UI Toolkit Page

Use this skill when creating a new UI Toolkit page.

## Alignment Rule

- Follow the UNIHper Editor UI Toolkit template.
- Use `OnLoaded()` for one-time setup.
- Use `OnShowing()` to bind UI events before interaction.
- Keep the page aligned with editor-provided defaults such as panel settings, default USS, and default font setup.

## Default Skeleton

```csharp
using UNIHper;
using UNIHper.UI;
using UnityEngine.UIElements;

[UIToolkitPage(Asset = "UI/{AssetName}", Type = UIType.Normal)]
public class {ClassName} : UIToolkitBase
{
    protected override void OnLoaded()
    {
    }

    protected override void OnShowing()
    {
        // BindButton("SaveButton", OnSaveClicked);
    }

    protected override void OnShown() { }
    protected override void OnHiding() { }
    protected override void OnHidden() { }
}
```

## Creation Notes

- File name should follow `{Feature}ToolkitUI.cs` unless the task requires a different convention.
- UXML asset naming should stay aligned with the page class.
- Prefer framework helpers such as `BindButton`, `BindTextField`, `BindToggle`, and `BindSlider`.
