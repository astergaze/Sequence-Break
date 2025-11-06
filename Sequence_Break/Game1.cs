using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Sequence_Break
{
    public class Game1 : Core
    {
        private Screen _currentScreen;

        public Game1()
            : base("Sequence Break", 1280, 720, false) { }

        protected override void Initialize()
        {
            IsMouseVisible = true;

            // crea la instancia de la pantalla
            _currentScreen = new MainMenuScreen(this);

            // Llama a base.Initialize()
            // Esto ejecuta Core.Initialize() y Game1.LoadContent()
            base.Initialize();
            //    Ahora que base.Initialize() y base.LoadContent()
            //    han terminado, tanto Core.GraphicsDevice como Core.Content
            //    existen y son seguros de usar.
            _currentScreen.LoadContent();
        }

        public void ChangeScreen(Screen newScreen)
        {
            _currentScreen = newScreen;
            _currentScreen.LoadContent();
        }

        protected override void LoadContent()
        {
            // Llama a base.LoadContent() para inicializar Core.Content
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            _currentScreen?.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(9, 0, 18));
            _currentScreen?.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}
