using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Oudidon
{
    public class OudidonGameComponent : DrawableGameComponent
    {
        protected new OudidonGame Game => base.Game as OudidonGame;
        protected SpriteBatch SpriteBatch => Game.SpriteBatch;

        public OudidonGameComponent(Game game) : base(game) { }
    }
}
