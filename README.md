# Unity Toolkit

Three independent Unity packages in one repository. Each one lives in its own folder under `Packages/`, has its own `package.json`, assemblies and version, and none of them depends on another.

| Package | Folder | What it is | Needs |
|---|---|---|---|
| **Flow** (`com.xoggas.flow`) | [`Packages/com.xoggas.flow`](Packages/com.xoggas.flow) | Type-based EventBus with sync and async events, and an async FSM | UniTask, VContainer |
| **Type Reference** (`com.xoggas.type-reference`) | [`Packages/com.xoggas.type-reference`](Packages/com.xoggas.type-reference) | Serializable `System.Type` field with a searchable, filtered Inspector dropdown | nothing |
| **UI** (`com.xoggas.ui`) | [`Packages/com.xoggas.ui`](Packages/com.xoggas.ui) | UI Toolkit panels: stack navigation, responsive panels, popups | VContainer |

## Installation

Install only the packages you need. Add them to `Packages/manifest.json` with the `path` query:

```json
"com.xoggas.flow": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.flow",
"com.xoggas.type-reference": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.type-reference",
"com.xoggas.ui": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.ui"
```

Third-party dependencies (UniTask, VContainer) are git packages, so Unity can't resolve them from `package.json`: install them in the project first.

## Versions

Each package is tagged separately as `<package>/<version>`, for example `flow/1.2.0`. Pin a package by appending the tag:

```json
"com.xoggas.flow": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.flow#flow/1.2.0"
```

## History

Flow and Type Reference were merged here from the `Xoggas/flow` and `Xoggas/type-reference` repositories with their commit history. UI was extracted from Pumpkin Quest.
