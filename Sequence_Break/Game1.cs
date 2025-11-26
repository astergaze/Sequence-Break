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
            _currentScreen = new MainMenuScreen(this);

            Core.Graphics.IsFullScreen = SettingsManager.Data.IsFullscreen;
            Core.Graphics.ApplyChanges();
            PlayerStatus.Initialize();
            base.Initialize();

            _currentScreen.LoadContent();
        }

        // Si 'loadContent' es false, asumimos que la pantalla ya existe y tiene sus datos.
        public void ChangeScreen(Screen newScreen, bool loadContent = true)
        {
            _currentScreen = newScreen;
            if (loadContent)
            {
                _currentScreen.LoadContent();
            }
        }

        protected override void LoadContent()
        {
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
