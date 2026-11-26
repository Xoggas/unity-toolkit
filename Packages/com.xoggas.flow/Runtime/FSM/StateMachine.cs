using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xoggas.Flow
{
    /// <summary>
    /// Runs one <see cref="State"/> at a time. Transitions are queued: a transition requested while
    /// another is in progress (for example, from a state's <c>Enter</c>) starts after it finishes.
    /// </summary>
    public sealed class StateMachine : MonoBehaviour
    {
        private readonly Dictionary<Type, State> _states = new();
        private readonly Queue<(State State, UniTaskCompletionSource Completion)> _pendingTransitions = new();

        private bool _isDestroyed;

        /// <summary>
        /// The active state. Null before the first transition, and while the previous state is
        /// exited but the next one hasn't started entering yet.
        /// </summary>
        public State CurrentState { get; private set; }

        /// <summary>
        /// True while a transition (including queued ones) is running. <see cref="State.Update"/>
        /// is not called during transitions.
        /// </summary>
        public bool IsTransitioning { get; private set; }

        public void RegisterState(State state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (_states.ContainsKey(state.GetType()))
            {
                throw new InvalidOperationException($"State {state.GetType().Name} is already registered with this StateMachine");
            }

            state.AttachTo(this);
            _states.Add(state.GetType(), state);
        }

        /// <summary>
        /// Requests a transition without waiting for it. Errors from <c>Enter</c>/<c>Exit</c> are
        /// reported through UniTask's unobserved exception handler.
        /// </summary>
        public void ChangeState<T>() where T : State
        {
            ChangeStateAsync<T>().Forget();
        }

        /// <summary>
        /// Requests a transition and completes when it (and every transition queued before it) is done.
        /// Don't await this from a state's <c>Enter</c> or <c>Exit</c>: the transition waits for the
        /// current one to finish, so awaiting it there never completes. Use <see cref="ChangeState{T}"/>.
        /// </summary>
        public UniTask ChangeStateAsync<T>() where T : State
        {
            return ChangeStateAsync(typeof(T));
        }

        public UniTask ChangeStateAsync(Type stateType)
        {
            if (_isDestroyed)
            {
                return UniTask.FromCanceled();
            }

            if (!_states.TryGetValue(stateType, out var state))
            {
                throw new InvalidOperationException($"State {stateType.Name} is not registered");
            }

            var completion = new UniTaskCompletionSource();

            _pendingTransitions.Enqueue((state, completion));

            if (!IsTransitioning)
            {
                ProcessTransitionsAsync().Forget();
            }

            return completion.Task;
        }

        private async UniTaskVoid ProcessTransitionsAsync()
        {
            IsTransitioning = true;

            try
            {
                while (_pendingTransitions.Count > 0 && !_isDestroyed)
                {
                    var (state, completion) = _pendingTransitions.Dequeue();

                    try
                    {
                        await TransitionAsync(state);
                        completion.TrySetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        completion.TrySetCanceled();
                    }
                    catch (Exception exception)
                    {
                        completion.TrySetException(exception);
                    }
                }
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private async UniTask TransitionAsync(State next)
        {
            if (CurrentState is not null)
            {
                try
                {
                    await CurrentState.ExitAsync();
                }
                finally
                {
                    CurrentState = null;
                }
            }

            if (_isDestroyed)
            {
                throw new OperationCanceledException();
            }

            CurrentState = next;

            await next.EnterAsync();
        }

        private void Update()
        {
            if (IsTransitioning)
            {
                return;
            }

            CurrentState?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            while (_pendingTransitions.Count > 0)
            {
                _pendingTransitions.Dequeue().Completion.TrySetCanceled();
            }

            // Exit isn't called here: the scene is being torn down, so it could touch destroyed objects.
            // The state's token is cancelled and its event subscriptions are disposed.
            CurrentState?.Terminate();
            CurrentState = null;
        }
    }
}
