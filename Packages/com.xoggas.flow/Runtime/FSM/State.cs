using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using VContainer;

namespace Xoggas.Flow
{
    public abstract class State
    {
        private static readonly CancellationToken CancelledToken = new(true);

        [Inject]
        private readonly IEventBus _eventBus;

        [Inject]
        private readonly StateMachine _stateMachine;

        private readonly List<IDisposable> _subscriptions = new();

        private CancellationTokenSource _lifetime;

        /// <summary>
        /// Cancelled when the state starts exiting (before <see cref="Exit"/> runs) or when the
        /// state machine is destroyed. Outside the state's lifetime it is already cancelled.
        /// Pass it to async work started in <see cref="Enter"/> or in event handlers.
        /// </summary>
        protected CancellationToken CancellationToken => _lifetime?.Token ?? CancelledToken;

        /// <summary>
        /// True from the start of <see cref="Enter"/> until the state starts exiting.
        /// </summary>
        public bool IsActive => _lifetime is not null;

        protected void Subscribe<T>(Action<T> action) where T : ISyncEvent
        {
            _subscriptions.Add(_eventBus.Subscribe(action));
        }

        protected void Subscribe<T>(Func<T, CancellationToken, UniTask> action) where T : IAsyncEvent
        {
            _subscriptions.Add(_eventBus.Subscribe(action));
        }

        protected void Invoke<T>(T evt) where T : ISyncEvent
        {
            _eventBus.Invoke(evt);
        }

        /// <summary>
        /// Invokes an async event, cancelled when this state exits.
        /// </summary>
        protected UniTask InvokeAsync<T>(T evt) where T : IAsyncEvent
        {
            return _eventBus.InvokeAsync(evt, CancellationToken);
        }

        protected UniTask InvokeAsync<T>(T evt, CancellationToken cancellationToken) where T : IAsyncEvent
        {
            return _eventBus.InvokeAsync(evt, cancellationToken);
        }

        internal UniTask EnterAsync()
        {
            _lifetime = new CancellationTokenSource();
            Subscribe();
            return Enter();
        }

        internal UniTask ExitAsync()
        {
            Terminate();
            return Exit();
        }

        /// <summary>
        /// Ends the state's lifetime without calling <see cref="Exit"/>: cancels
        /// <see cref="CancellationToken"/> and disposes event subscriptions.
        /// </summary>
        internal void Terminate()
        {
            var lifetime = _lifetime;

            if (lifetime is null)
            {
                return;
            }

            _lifetime = null;

            try
            {
                lifetime.Cancel();
            }
            finally
            {
                lifetime.Dispose();
                Unsubscribe();
            }
        }

        public virtual void Update(float deltaTime)
        {
        }

        protected virtual void Subscribe()
        {
        }

        protected virtual UniTask Enter()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask Exit()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Requests a transition. Safe to call from <see cref="Enter"/>, <see cref="Exit"/> and
        /// event handlers: the transition is queued until the current one finishes.
        /// </summary>
        protected void GoTo<T>() where T : State
        {
            _stateMachine.ChangeState<T>();
        }

        private void Unsubscribe()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
