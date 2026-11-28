# Unity Toolkit

Two independent Unity packages and a project template in one repository. Each one lives in its own folder under `Packages/`, has its own `package.json`, assemblies and version, and neither depends on the other.

| Package | Folder | What it is | Needs |
|---|---|---|---|
| **Flow** (`com.xoggas.flow`) | [`Packages/com.xoggas.flow`](Packages/com.xoggas.flow) | Type-based EventBus with sync and async events, and an async FSM | UniTask, VContainer |
| **Type Reference** (`com.xoggas.type-reference`) | [`Packages/com.xoggas.type-reference`](Packages/com.xoggas.type-reference) | Serializable `System.Type` field with a searchable, filtered Inspector dropdown | nothing |

## Installation

Install only the packages you need. Add them to `Packages/manifest.json` with the `path` query:

```json
"com.xoggas.flow": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.flow",
"com.xoggas.type-reference": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.type-reference"
```

Flow's dependencies (UniTask, VContainer) are git packages, so Unity can't resolve them from `package.json`: install them in the project first.

## Versions

Each package is tagged separately as `<package>/<version>`, for example `flow/1.2.0`. Pin a package by appending the tag:

```json
"com.xoggas.flow": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.flow#flow/1.2.0"
```

## Project template

[`Templates/com.xoggas.template.universal-2d`](Templates/com.xoggas.template.universal-2d) is a Unity Hub template for Unity 6000.6: Universal 2D (URP) with both packages above plus VContainer, UniTask, R3, ObservableCollections, NuGetForUnity, Newtonsoft.Json, Alchemy and LitMotion, and Newtonsoft converters for R3 and ObservableCollections. Build and install it with:

```powershell
./Templates/pack.ps1 -Install
```

See its [README](Templates/com.xoggas.template.universal-2d/README.md) for details.

## History

Flow and Type Reference were merged here from the `Xoggas/flow` and `Xoggas/type-reference` repositories with their commit history.
