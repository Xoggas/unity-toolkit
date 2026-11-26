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

To pin a version, append a tag: `...?path=/Packages/com.xoggas.flow#flow/1.1.0`.

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

#### Transitions

Transitions are queued. Calling `GoTo<T>()` from `Enter`, `Exit` or an event handler doesn't interrupt the running transition: the next one starts after the current state has finished entering. Requests run in the order they were made.

`ChangeState<T>()` is fire-and-forget; errors from `Enter`/`Exit` go to UniTask's unobserved exception handler. To wait for a transition or catch its errors, use `await stateMachine.ChangeStateAsync<T>()`. Don't await it from a state's own `Enter`/`Exit`: it waits for the current transition, which is waiting for that state, so it never completes. Use `GoTo<T>()` there.

`CurrentState` and `IsTransitioning` expose the machine's status. Requesting a state that wasn't registered throws immediately.

#### Cancellation

Each state has a `CancellationToken` that is cancelled when the state starts exiting (before `Exit` runs) and when the `StateMachine` is destroyed. Pass it to async work started in `Enter` or in handlers so it stops with the state. `InvokeAsync` from a state uses it by default.

```csharp
protected override async UniTask Enter()
{
    await UniTask.Delay(3000, cancellationToken: CancellationToken);
    GoTo<GameplayState>();
}
```

When the `StateMachine` is destroyed, queued transitions are cancelled and the current state's token is cancelled and its subscriptions disposed. `Exit` is not called, since the scene is being torn down.
