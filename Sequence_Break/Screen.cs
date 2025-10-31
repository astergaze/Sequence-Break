using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace Sequence_Break
{
    // Esta es la clase base para todas las pantallas del juego
    public abstract class Screen
    {
        // Hacemos accesibles las herramientas principales de Game1
        protected Game1 _game;
        protected ContentManager Content => Core.Content;
        protected SpriteBatch SpriteBatch => Core.SpriteBatch;
        protected GraphicsDevice GraphicsDevice => Core.GraphicsDevice;

        public Screen(Game1 game)
        {
            _game = game;
        }

        // Cada pantalla tiene que tener estos métodos
        public abstract void LoadContent();
        public abstract void Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);
    }
}
