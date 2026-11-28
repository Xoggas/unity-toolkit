# Universal 2D (Xoggas)

A Unity Hub project template: Unity's built-in **Universal 2D** (URP with the 2D Renderer), minus the Welcome tutorial, plus this toolkit's packages and the libraries they are used with.

Built from Unity 6000.6.0f1's `com.unity.template.universal-2d` 7.0.0.

## What's in it

| Package | Version | Source |
|---|---|---|
| Flow (`com.xoggas.flow`) | 1.2.0 | this repository |
| Type Reference (`com.xoggas.type-reference`) | 1.0.0 | this repository |
| VContainer | 1.19.0 | git |
| UniTask | 2.5.11 | git |
| R3 (Unity part) | 1.3.1 | git |
| R3 core, ObservableCollections, ObservableCollections.R3 | 1.3.1 / 3.3.4 / 3.3.4 | NuGet |
| NuGetForUnity | 4.5.0 | git |
| Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) | 3.2.1 | Unity registry |
| Alchemy | 2.1.1 | git |
| LitMotion | 2.0.2 | git |

The NuGet libraries (R3, ObservableCollections and their dependencies) are already in `Assets/Packages`, not just listed in `packages.config`. R3's Unity package compiles against them, and with compile errors on first open NuGetForUnity never gets a chance to restore them, so a fresh project would open broken. Keep `Assets/Packages` under version control.

## JSON serialization for R3 and ObservableCollections

`Assets/Scripts/Serialization` (assembly `Serialization`) has Newtonsoft converters:

- `ReactivePropertyJsonConverter`: `ReactiveProperty<T>`, `SerializableReactiveProperty<T>`, `BindableReactiveProperty<T>` and fields typed `ReadOnlyReactiveProperty<T>` are written as their value: `{"Hp": 42}`.
- `ObservableCollectionJsonConverter`: `ObservableList`, `ObservableHashSet`, `ObservableQueue`, `ObservableStack`, `ObservableRingBuffer` and `ObservableFixedSizeRingBuffer` are written as arrays, `ObservableDictionary` as an object.

When the target already holds an instance, the converters update it instead of replacing it, so `JsonConvert.PopulateObject` keeps subscriptions and views alive and fires their events. This also covers getter-only properties.

```csharp
var settings = ReactiveJson.CreateSettings();
var json = JsonConvert.SerializeObject(save, settings);
JsonConvert.PopulateObject(json, save, settings);

// or once at startup, for every JsonConvert call:
ReactiveJson.UseAsDefault();
```

Limits:

- `ObservableFixedSizeRingBuffer` capacity is not stored in JSON. An existing instance keeps its own; a new one is sized to the loaded items. Initialize the field to keep a larger capacity.
- The converters build generic handlers with `MakeGenericType`. Under IL2CPP with value-type arguments this relies on full generic sharing (the default in Unity 6) and has not been tested in a player build.

## Install

Build the `.tgz` and copy it into an editor's template folder with [`pack.ps1`](../pack.ps1):

```powershell
./Templates/pack.ps1 -Install
```

Without `-Install` it only writes `Templates/dist/com.xoggas.template.universal-2d-<version>.tgz`. With it, the archive is also copied into `Editor/Data/Resources/PackageManager/ProjectTemplates` of every Unity Hub editor whose version matches the template's `unity` field (asks for administrator rights). Restart Unity Hub, then pick **Universal 2D (Xoggas)** when creating a project.

## License

The project settings and assets come from Unity's template and stay under the Unity Companion License (see `LICENSE.md`). The serialization scripts are MIT, like the rest of this repository.
