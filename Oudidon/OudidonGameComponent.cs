using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Oudidon
{
    public class OudidonGameComponent : DrawableGameComponent
    {
        private class DelayedAction
        {
            public Action Action;
            public float Delay;
        }

        protected new OudidonGame Game => base.Game as OudidonGame;
        protected SpriteBatch SpriteBatch => Game.SpriteBatch;
        private List<DelayedAction> _delayedActions = new();

        public OudidonGameComponent(Game game) : base(game) { }

        public override void Update(GameTime gameTime)
        {
            for (int i = _delayedActions.Count - 1; i >= 0; i--)
            {
                DelayedAction delayedAction = _delayedActions[i];
                delayedAction.Delay -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (delayedAction.Delay <= 0)
                {
                    delayedAction.Action.Invoke();
                    _delayedActions.RemoveAt(i);
                }
            }
        }

        public void DelayAction(Action action, float delay)
        {
            if (delay == 0)
            {
                action.Invoke();
                return;
            }

            DelayedAction delayedAction = new DelayedAction { Action = action, Delay = delay };
            _delayedActions.Add(delayedAction);
        }
    }
}
