using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Oudidon
{
    public class SimpleStateMachine
    {
        public class State
        {
            public string name;
            public Action OnEnter;
            public Action<GameTime, float> OnUpdate;
            public Action OnExit;
            public Action<SpriteBatch, GameTime> OnDraw;
            public float timer;
        }

        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();

        private State _currentState;
        public string CurrentState => _currentState?.name;
        public float CurrentStateTimer => _currentState != null ? _currentState.timer : 0;

        public void AddState(string name, Action OnEnter = null, Action OnExit = null, Action<GameTime, float> OnUpdate = null, Action<SpriteBatch, GameTime> OnDraw = null)
        {
            if (!_states.ContainsKey(name))
            {
                _states.Add(name, new State { name = name, OnEnter = OnEnter, OnExit = OnExit, OnUpdate = OnUpdate, OnDraw = OnDraw });
            }
        }

        public void SetState(string name)
        {
            _currentState?.OnExit?.Invoke();
            if (_states.TryGetValue(name, out _currentState))
            {
                _currentState.timer = 0;
                _currentState.OnEnter?.Invoke();
            }
            else
            {
                _currentState = null;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_currentState != null)
            {
                _currentState.timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _currentState.OnUpdate?.Invoke(gameTime, _currentState.timer);
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _currentState?.OnDraw?.Invoke(spriteBatch, gameTime);
        }
    }
}
