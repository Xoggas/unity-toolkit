# Flow

Lightweight game flow toolkit for Unity:

- **EventBus** — type-based event bus with synchronous (`ISyncEvent`) and asynchronous (`IAsyncEvent`, UniTask) events. Subscribing/unsubscribing during an invocation is safe: mutations are deferred until the invocation finishes.
- **FSM** — `StateMachine` MonoBehaviour with async `Enter`/`Exit` transitions and per-state event subscriptions that are disposed automatically on exit.

## Installation

Flow depends on these packages, which must be installed in your project first:

- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [VContainer](https://github.com/hadashiA/VContainer) (`jp.hadashikick.vcontainer`)

Then add to `Packages/manifest.json`:

```json
"com.xoggas.flow": "https://github.com/Xoggas/unity-toolkit.git?path=/Packages/com.xoggas.flow"
```

To pin a version, append a tag: `...?path=/Packages/com.xoggas.flow#flow/1.0.0`.

## Usage

### EventBus

```csharp
using Xoggas.Flow;

public readonly struct ScoreChanged : ISyncEvent
{
    public readonly int Score;
    public ScoreChanged(int score) => Score = score;
}

public readonly struct LevelLoading : IAsyncEvent { }

IEventBus bus = new EventBus();

var subscription = bus.Subscribe<ScoreChanged>(e => Debug.Log(e.Score));
bus.Subscribe<LevelLoading>(async (e, ct) => await UniTask.Delay(500, cancellationToken: ct));

bus.Invoke(new ScoreChanged(10));
await bus.InvokeAsync(new LevelLoading());

subscription.Dispose();
```

Async subscribers of one event run concurrently; `InvokeAsync` completes when all of them do.

### FSM

States receive `IEventBus` and `StateMachine` via VContainer injection, so both must be registered in the container and states must be resolved (or injected) through it.

```csharp
using Xoggas.Flow;

public sealed class MenuState : State
{
    protected override void Subscribe()
    {
        Subscribe<PlayPressed>(_ => GoTo<GameplayState>());
    }

    protected override UniTask Enter() => ShowMenuAsync();
    protected override UniTask Exit() => HideMenuAsync();
}
```

```csharp
builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
builder.RegisterComponentInHierarchy<StateMachine>();
builder.Register<MenuState>(Lifetime.Singleton);
builder.Register<GameplayState>(Lifetime.Singleton);

// on startup
stateMachine.RegisterState(container.Resolve<MenuState>());
stateMachine.RegisterState(container.Resolve<GameplayState>());
stateMachine.ChangeState<MenuState>();
```

`Update(float deltaTime)` is called on the current state every frame, except while a transition is in progress.
