using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using VContainer;

namespace Xoggas.Flow
{
    public abstract class State
    {
        [Inject]
        private readonly IEventBus _eventBus;

        [Inject]
        private readonly StateMachine _stateMachine;

        private readonly List<IDisposable> _subscriptions = new();

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

        protected UniTask InvokeAsync<T>(T evt) where T : IAsyncEvent
        {
            // TODO: Fix cancellation token
            return _eventBus.InvokeAsync(evt, CancellationToken.None);
        }

        public UniTask EnterAsync()
        {
            Subscribe();
            return Enter();
        }
        
        public UniTask ExitAsync()
        {
            Unsubscribe();
            return Exit();
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