# UI

A small panel system for Unity UI Toolkit, extracted from Pumpkin Quest.

- **PanelSwitcher**: stack-based navigation. `GoTo` pushes a panel, `GoBack` pops it, `GoBackUntil<T>` pops until a panel of type `T` is on top.
- **UIPanel**: a panel with its own `UIDocument` and separate UXML/PanelSettings for portrait and landscape screens. Show/hide toggles the `enabled` USS class, so transitions are styled in USS. Optionally also toggles `display`.
- **CompoundPanel**: activates/deactivates several panels as one navigation step.
- **Popup**: a timed element inside a document; `ShowInternal()` adds the `visible` USS class for a set duration.
- **UnderlineOnHover**: underlines a Label/Button's text while hovered, with an Inspector that lists the document's named elements.

## Installation

UI depends on [VContainer](https://github.com/hadashiA/VContainer) (`jp.hadashikick.vcontainer`), which must be installed first. Then add to `Packages/manifest.json`:

```json
"com.xoggas.ui": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.ui"
```

To pin a version, append a tag: `...?path=/Packages/com.xoggas.ui#ui/1.0.0`.

## Usage

Panels receive `PanelSwitcher` via VContainer injection, so register it and make sure panels are injected:

```csharp
builder.RegisterComponentInHierarchy<PanelSwitcher>();
builder.RegisterComponentInHierarchy<MainMenuPanel>();
builder.RegisterComponentInHierarchy<SettingsPanel>();
```

```csharp
using UnityEngine.UIElements;
using Xoggas.UI;

public sealed class MainMenuPanel : UIPanel
{
    [SerializeField] private SettingsPanel _settings;

    protected override void Awake()
    {
        base.Awake();
        Root.Q<Button>("Settings").clicked += () => PanelSwitcher.GoTo(_settings);
        Root.Q<Button>("Back").clicked += PanelSwitcher.GoBack;
    }
}
```

Passing data to a panel:

```csharp
public sealed class SaveSlotArgs : PanelActivationArgs
{
    public int Slot;
}

PanelSwitcher.GoTo(_loadPanel, new SaveSlotArgs { Slot = 2 });
```

USS for the transitions:

```css
.panel-root { opacity: 0; transition-duration: 0.5s; }
.panel-root.enabled { opacity: 1; }
```

## Migrating from the in-game copy

The script `.meta` GUIDs are the same as in Pumpkin Quest, so scenes and prefabs keep their component references once the old `Assets/Scripts/UI` scripts are deleted and the package is installed. The differences:

- All types are in the `Xoggas.UI` namespace (editor code in `Xoggas.UI.Editor`). Add `using Xoggas.UI;` to the game's panels.
- `Popup` no longer injects `GameSettingsProvider`; popups that need it (such as `RelationshipPopup`) inject it themselves.
- `PanelBase` moved to its own file, `PanelSwitcher.GoTo` accepts any `PanelBase`, and the `[Required]` marker on `UIPanel` fields was dropped.
- `ClickSoundPlayer`, `LocalizationBinding` and `CustomButton` stayed in the game: they depend on its audio, settings and localization code.
- `ConfirmationPanel` stayed in the game too: it is a concrete panel built on `UIPanel` (it relies on a document with `Ok`, `Close` and `Cancel` buttons), not part of the panel system.
