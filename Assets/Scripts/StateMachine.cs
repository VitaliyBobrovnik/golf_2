using System;
using System.Collections.Generic;
using Golf.States.Interfaces;

namespace Golf.States
{
    public class StateMachine
    {
        private IGameState m_currentState =>
            m_stateStack.Count > 0 ? m_stateStack.Peek() : null;

        private readonly Dictionary<Type, IGameState> m_states = new();
        private readonly Stack<IGameState> m_stateStack = new();

        public void Register(IGameState state)
        {
            m_states.Add(state.GetType(), state);
        }

        public void ChangeState<TState>() where TState : IGameState
        {
            Stop();
            PushState<TState>();
        }

        public void PushState<TState>() where TState : IGameState
        {
            IGameState nextState = m_states[typeof(TState)];

            Deactivate(m_currentState);
            m_stateStack.Push(nextState);
            nextState.Enter();
            Activate(nextState);
        }

        public void PopState()
        {
            IGameState topState = m_stateStack.Pop();
            Deactivate(topState);
            topState.Exit();
            Activate(m_currentState);
        }

        public void Stop()
        {
            Deactivate(m_currentState);

            while (m_stateStack.Count > 0)
            {
                m_stateStack.Pop().Exit();
            }
        }

        public void Update()
        {
            if (m_currentState is IUpdatable updatable)
            {
                updatable.Update();
            }
        }

        private void Activate(IGameState state)
        {
            if (state is IActivate activatable)
            {
                activatable.Activate();
            }
        }

        private void Deactivate(IGameState state)
        {
            if (state is IActivate activatable)
            {
                activatable.Deactivate();
            }
        }
    }
}