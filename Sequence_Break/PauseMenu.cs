using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Sequence_Break
{
    public class PauseMenu
    {
        // Variables principales
        public bool IsActive { get; private set; }
        private Game1 _game;
        private Screen _currentScreen;
        private SpriteFont _font;
        private Texture2D _pixel;
        private GraphicsDevice _graphics;

        // Opciones del menu
        private string[] _options = { "REANUDAR", "OPCIONES", "MENU PRINCIPAL" };
        private string[] _optionsHover = { "[ SINTONIZAR ]", "[ ALTERAR ]", "[ DESPERTAR ]" };

        private int _selectedIndex = 0;
        private List<Rectangle> _optionRects;

        // Estado del mouse y del teclado
        private KeyboardState _prevKbState;
        private MouseState _prevMouseState;

        // Colores del overlay
        private Color _overlayColor = Color.Black * 0.6f;
        private Color _panelColor = new Color(20, 40, 70);
        private Color _selectedColor = new Color(200, 100, 255);
        private Color _normalColor = Color.White;
        private Rectangle _panelRect;

        // Constructor
        public PauseMenu(Game1 game, Screen currentScreen, SpriteFont font, GraphicsDevice graphics)
        {
            _game = game;
            _currentScreen = currentScreen;
            _font = font;
            _graphics = graphics;
            _optionRects = new List<Rectangle>();

            _pixel = new Texture2D(_graphics, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int panelWidth = 800;
            int panelHeight = 400;
            _panelRect = new Rectangle(
                (_graphics.Viewport.Width - panelWidth) / 2,
                (_graphics.Viewport.Height - panelHeight) / 2,
                panelWidth,
                panelHeight
            );

            int startY = _panelRect.Y + 120;
            int spacing = 60;
            for (int i = 0; i < _options.Length; i++)
            {
                _optionRects.Add(
                    new Rectangle(_panelRect.X, startY + (i * spacing), _panelRect.Width, 40)
                );
            }
        }

        public void Show()
        {
            IsActive = true;
            _selectedIndex = 0;
            _game.IsMouseVisible = true;
            _prevKbState = Keyboard.GetState();
            _prevMouseState = Mouse.GetState();
        }

        public void Hide()
        {
            IsActive = false;
            _game.IsMouseVisible = false;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;

            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            // Navegacion
            if (kbs.IsKeyDown(Keys.Up) && !_prevKbState.IsKeyDown(Keys.Up))
            {
                _selectedIndex--;
                if (_selectedIndex < 0)
                    _selectedIndex = _options.Length - 1;
            }
            if (kbs.IsKeyDown(Keys.Down) && !_prevKbState.IsKeyDown(Keys.Down))
            {
                _selectedIndex++;
                if (_selectedIndex >= _options.Length)
                    _selectedIndex = 0;
            }

            // Mouse Hover
            Point mousePos = ms.Position;
            for (int i = 0; i < _optionRects.Count; i++)
            {
                if (_optionRects[i].Contains(mousePos))
                    _selectedIndex = i;
            }

            // Seleccion
            bool enterPressed = kbs.IsKeyDown(Keys.Enter) && !_prevKbState.IsKeyDown(Keys.Enter);
            bool mouseClicked =
                ms.LeftButton == ButtonState.Pressed
                && _prevMouseState.LeftButton == ButtonState.Released;

            if (enterPressed || (mouseClicked && _optionRects[_selectedIndex].Contains(mousePos)))
            {
                PerformAction();
            }

            if (kbs.IsKeyDown(Keys.Escape) && !_prevKbState.IsKeyDown(Keys.Escape))
            {
                Hide();
            }

            _prevKbState = kbs;
            _prevMouseState = ms;
        }

        private void PerformAction()
        {
            switch (_selectedIndex)
            {
                case 0: // REANUDAR
                    Hide();
                    break;
                case 1: // OPCIONES
                    // Pasamos 'this._currentScreen' al constructor de OptionsScreen
                    // Esto permite que OptionsScreen sepa a donde volver
                    _game.ChangeScreen(new OptionsScreen(_game, _currentScreen));
                    break;
                case 2: // MENU PRINCIPAL
                    _game.IsMouseVisible = true;
                    _game.ChangeScreen(new MainMenuScreen(_game));
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            // Usamos _graphics para las dimensiones del Viewport
            spriteBatch.Draw(
                _pixel,
                new Rectangle(0, 0, _graphics.Viewport.Width, _graphics.Viewport.Height),
                _overlayColor
            );

            spriteBatch.Draw(_pixel, _panelRect, _panelColor);

            for (int i = 0; i < _options.Length; i++)
            {
                bool isSelected = (_selectedIndex == i);
                string text = isSelected ? _optionsHover[i] : _options[i];
                Color color = isSelected ? _selectedColor : _normalColor;

                Vector2 textSize = _font.MeasureString(text);
                Vector2 pos = new Vector2(
                    _panelRect.Center.X - (textSize.X / 2),
                    _optionRects[i].Y
                );

                spriteBatch.DrawString(_font, text, pos, color);
            }
        }
    }
}
