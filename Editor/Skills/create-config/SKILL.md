---
name: create-config
description: 'Create a new config class aligned with UNIHper Editor templates. Use when asked to create settings, app configuration, or UConfig-based serializable data.'
---

# Create Config

Use this skill when creating a new config class.

## Alignment Rule

- Follow the UNIHper Editor config template.
- Prefer framework serialization attributes over ad-hoc persistence code.
- Put validation and normalization in `OnLoaded()`.

## Default Skeleton

```csharp
using System.Xml.Serialization;
using Newtonsoft.Json;
using UNIHper;

[SerializedAt(AppPath.StreamingDir, "Configs", Priority = 0)]
[SerializeWith(ConfigDriver.XML)]
public class {ClassName} : UConfig
{
    [XmlIgnore, JsonIgnore]
    public bool IsInitialized { get; set; }

    protected override void OnLoaded()
    {
        IsInitialized = true;
    }

    protected override string Comment()
    {
        return "{ClassName}";
    }
}
```

## Creation Notes

- File name should follow `{Feature}Config.cs`.
- Keep runtime-only fields explicitly excluded from serialization.
