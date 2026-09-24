using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xoggas.Flow
{
    public sealed class StateMachine : MonoBehaviour
    {
        private readonly Dictionary<Type, State> _states = new();
        private State _currentState;
        private bool _isTransitioning;

        public void RegisterState(State state)
        {
            _states.Add(state.GetType(), state);
        }

        public void ChangeState<T>() where T : State
        {
            ChangeStateAsync<T>().Forget();
        }

        private async UniTaskVoid ChangeStateAsync<T>() where T : State
        {
            if (!_states.TryGetValue(typeof(T), out var state))
            {
                throw new Exception($"State {typeof(T).Name} not found");
            }

            _isTransitioning = true;

            try
            {
                if (_currentState is not null)
                {
                    await _currentState.ExitAsync();
                }

                _currentState = state;

                await _currentState.EnterAsync();
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private void Update()
        {
            if (_isTransitioning)
            {
                return;
            }

            _currentState?.Update(Time.deltaTime);
        }
    }
}