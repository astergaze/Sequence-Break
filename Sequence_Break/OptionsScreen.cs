using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary;

namespace Sequence_Break
{
    public class OptionsScreen : Screen
    {
        private SpriteFont _menuFont;
        private Texture2D _pixel;
        private KeyboardState _previousKeyboardState;
        private MouseState _previousMouseState;

        // Opciones de navegacion
        private string[] _options = { "MUSICA", "SFX", "PANTALLA", "VOLVER" };
        private int _selectedOptionIndex = 0;
        private List<Rectangle> _optionRects = new List<Rectangle>();

        // Logica de Input
        private bool _isReadyForInput = false; // Previene el click fantasma de otra pantalla
        private bool _isDraggingSlider = false; // Para el slider

        // Colores
        private Color _titleColor = new Color(200, 100, 255); // Morado
        private Color _normalColor = Color.White;
        private Color _selectedColor = new Color(200, 100, 255); // Morado

        // Color #1b0530 con 93% opacidad (237 alpha)
        private Color _panelColor = new Color(27, 5, 48, 237);

        // Geometria
        private Rectangle _panelRect;
        private Vector2 _titlePosition;

        // Diccionario para guardar los rectangulos de los sliders
        private Dictionary<string, Rectangle> _sliderBarRects = new Dictionary<string, Rectangle>();

        // Pantalla a la que volver (null si venimos del MainMenu)
        private Screen _screenToReturnTo;

        public OptionsScreen(Game1 game, Screen screenToReturnTo = null)
            : base(game)
        {
            _screenToReturnTo = screenToReturnTo;
        }

        public override void LoadContent()
        {
            _menuFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _game.IsMouseVisible = true;
            _previousKeyboardState = Keyboard.GetState();
            _previousMouseState = Mouse.GetState();

            // Layout

            // Medir todo el contenido primero
            string title = "[ CALIBRACION DE PERCEPCION ]";
            string longMusica = "[ MUSICA: [||||||||||] - 100% ]";
            string longPantalla = "[ PANTALLA: [VENTANA] ]";
            string longVolver = "[ VOLVER ]";

            float titleWidth = _menuFont.MeasureString(title).X;
            float optionsWidth = _menuFont.MeasureString(longMusica).X;
            float pantallaWidth = _menuFont.MeasureString(longPantalla).X;
            float volverWidth = _menuFont.MeasureString(longVolver).X;

            float contentWidth = Math.Max(
                titleWidth,
                Math.Max(optionsWidth, Math.Max(pantallaWidth, volverWidth))
            );

            // Definir el panel basado en el contenido
            int panelPadding = 100;
            int panelWidth = (int)contentWidth + panelPadding;
            int panelHeight = 600;

            _panelRect = new Rectangle(
                (GraphicsDevice.Viewport.Width - panelWidth) / 2,
                (GraphicsDevice.Viewport.Height - panelHeight) / 2,
                panelWidth,
                panelHeight
            );

            // Centrar el titulo
            _titlePosition = new Vector2(_panelRect.Center.X - (titleWidth / 2), _panelRect.Y + 60);

            // Crear los rectangulos para las opciones
            _optionRects.Clear();
            _sliderBarRects.Clear();
            float itemHeight = _menuFont.LineSpacing;
            float yPos = _panelRect.Y + 200;
            float xPosBase = _panelRect.Center.X - (contentWidth / 2);

            for (int i = 0; i < _options.Length; i++)
            {
                float currentY = yPos + (i * (itemHeight + 30));
                if (_options[i] == "VOLVER")
                {
                    currentY += 40;
                }

                _optionRects.Add(
                    new Rectangle((int)xPosBase, (int)currentY, (int)contentWidth, (int)itemHeight)
                );

                // Pre-calcular sliders
                if (_options[i] == "MUSICA" || _options[i] == "SFX")
                {
                    string prefix = (_options[i] == "MUSICA") ? "[ MUSICA: " : "[ SFX   : ";
                    float prefixWidth = _menuFont.MeasureString(prefix).X;
                    float barWidth = _menuFont.MeasureString("[||||||||||]").X;

                    string fullText =
                        (_options[i] == "MUSICA")
                            ? longMusica
                            : longMusica.Replace("MUSICA", "SFX   ");
                    float fullTextWidth = _menuFont.MeasureString(fullText).X;
                    float textStartX = _panelRect.Center.X - (fullTextWidth / 2);

                    _sliderBarRects[_options[i]] = new Rectangle(
                        (int)(textStartX + prefixWidth),
                        (int)currentY,
                        (int)barWidth,
                        (int)itemHeight
                    );
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();
            Point mousePoint = ms.Position;

            if (!_isReadyForInput)
            {
                // Espera a que el jugador suelte el click inicial
                if (
                    ms.LeftButton == ButtonState.Released
                    && _previousMouseState.LeftButton == ButtonState.Released
                )
                {
                    _isReadyForInput = true;
                }
                _previousKeyboardState = kbs;
                _previousMouseState = ms;
                return;
            }

            // Control de Mouse (Hover)
            if (!_isDraggingSlider)
            {
                _selectedOptionIndex = -1;
                for (int i = 0; i < _optionRects.Count; i++)
                {
                    if (_optionRects[i].Contains(mousePoint))
                    {
                        _selectedOptionIndex = i;
                        break;
                    }
                }
            }

            // Control de Teclado (Navegacion)
            if (kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            {
                _selectedOptionIndex--;
                if (_selectedOptionIndex < 0)
                    _selectedOptionIndex = _options.Length - 1;
            }
            if (kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            {
                _selectedOptionIndex++;
                if (_selectedOptionIndex >= _options.Length)
                    _selectedOptionIndex = 0;
            }

            // Acciones de Teclado
            if (_selectedOptionIndex != -1)
            {
                string selected = _options[_selectedOptionIndex];

                // Izquierda/Derecha (Sliders)
                if (kbs.IsKeyDown(Keys.Left) && !_previousKeyboardState.IsKeyDown(Keys.Left))
                {
                    if (selected == "MUSICA")
                        UpdateMusicVolume(-1);
                    if (selected == "SFX")
                        UpdateSfxVolume(-1);
                    if (selected == "PANTALLA")
                        ToggleFullscreen();
                }
                if (kbs.IsKeyDown(Keys.Right) && !_previousKeyboardState.IsKeyDown(Keys.Right))
                {
                    if (selected == "MUSICA")
                        UpdateMusicVolume(1);
                    if (selected == "SFX")
                        UpdateSfxVolume(1);
                    if (selected == "PANTALLA")
                        ToggleFullscreen();
                }

                if (kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                {
                    if (selected == "PANTALLA")
                        ToggleFullscreen();
                    if (selected == "VOLVER")
                        GoBack();
                }
            }

            // Control de Mouse
            if (ms.LeftButton == ButtonState.Pressed)
            {
                if (
                    _previousMouseState.LeftButton == ButtonState.Released
                    && _selectedOptionIndex != -1
                )
                {
                    string selected = _options[_selectedOptionIndex];
                    if (selected == "MUSICA" || selected == "SFX")
                    {
                        _isDraggingSlider = true;
                        HandleSliderDrag(mousePoint);
                    }
                }

                if (_isDraggingSlider)
                {
                    HandleSliderDrag(mousePoint);
                }
            }
            else if (ms.LeftButton == ButtonState.Released)
            {
                if (_isDraggingSlider)
                {
                    _isDraggingSlider = false;
                    SettingsManager.SaveSettings();
                }
                else if (
                    _previousMouseState.LeftButton == ButtonState.Pressed
                    && _selectedOptionIndex != -1
                )
                {
                    string selected = _options[_selectedOptionIndex];
                    if (selected == "PANTALLA")
                        ToggleFullscreen();
                    if (selected == "VOLVER")
                        GoBack();
                }
            }

            if (kbs.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                GoBack();
            }

            _previousKeyboardState = kbs;
            _previousMouseState = ms;
        }

        // Retorno
        private void GoBack()
        {
            if (_screenToReturnTo != null)
            {
                // 'false' indica que no llame a LoadContent(),
                // preservando el estado de la pantalla de juego
                _game.ChangeScreen(_screenToReturnTo, false);
            }
            else
            {
                // Si no hay pantalla anterior, vamos al Menu Principal
                _game.ChangeScreen(new MainMenuScreen(_game));
            }
        }

        // Logica de Slider
        private void HandleSliderDrag(Point mousePoint)
        {
            if (_selectedOptionIndex < 0 || _selectedOptionIndex > 1)
                return;

            string selectedOption = _options[_selectedOptionIndex];
            Rectangle sliderBarRect = _sliderBarRects[selectedOption];

            float relativeX = Math.Clamp(mousePoint.X - sliderBarRect.X, 0, sliderBarRect.Width);
            float percentage = relativeX / (float)sliderBarRect.Width;
            int newValue = (int)Math.Round(percentage * 10);
            newValue = Math.Clamp(newValue, 0, 10);

            if (selectedOption == "MUSICA")
            {
                if (SettingsManager.Data.MusicVolume != newValue)
                {
                    SettingsManager.Data.MusicVolume = newValue;
                    SettingsManager.ApplyMusicVolume();
                }
            }
            else if (selectedOption == "SFX")
            {
                if (SettingsManager.Data.SfxVolume != newValue)
                {
                    SettingsManager.Data.SfxVolume = newValue;
                    // TODO: Tocar sonido de prueba
                }
            }
        }

        private void UpdateMusicVolume(int direction)
        {
            SettingsManager.Data.MusicVolume = Math.Clamp(
                SettingsManager.Data.MusicVolume + direction,
                0,
                10
            );
            SettingsManager.ApplyMusicVolume();
            SettingsManager.SaveSettings();
        }

        private void UpdateSfxVolume(int direction)
        {
            SettingsManager.Data.SfxVolume = Math.Clamp(
                SettingsManager.Data.SfxVolume + direction,
                0,
                10
            );
            SettingsManager.SaveSettings();
        }

        private void ToggleFullscreen()
        {
            SettingsManager.Data.IsFullscreen = !SettingsManager.Data.IsFullscreen;
            Core.Graphics.IsFullScreen = SettingsManager.Data.IsFullscreen;
            Core.Graphics.ApplyChanges();
            SettingsManager.SaveSettings();
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Fondo
            SpriteBatch.Draw(_pixel, _panelRect, _panelColor);

            // Titulo
            SpriteBatch.DrawString(
                _menuFont,
                "[ CALIBRACION DE PERCEPCION ]",
                _titlePosition,
                _titleColor
            );

            // Opciones
            for (int i = 0; i < _options.Length; i++)
            {
                Color color = (i == _selectedOptionIndex) ? _selectedColor : _normalColor;
                string text = "";

                switch (_options[i])
                {
                    case "MUSICA":
                        string musicBar = GenerateBar(SettingsManager.Data.MusicVolume);
                        text = $"[ MUSICA: {musicBar} - {SettingsManager.Data.MusicVolume * 10}% ]";
                        break;
                    case "SFX":
                        string sfxBar = GenerateBar(SettingsManager.Data.SfxVolume);
                        text = $"[ SFX   : {sfxBar} - {SettingsManager.Data.SfxVolume * 10}% ]";
                        break;
                    case "PANTALLA":
                        string screenMode = SettingsManager.Data.IsFullscreen
                            ? "COMPLETA"
                            : "VENTANA";
                        text = $"[ PANTALLA: [{screenMode}] ]";
                        break;
                    case "VOLVER":
                        text = "[ VOLVER ]";
                        break;
                }

                Rectangle rect = _optionRects[i];
                Vector2 textSize = _menuFont.MeasureString(text);
                Vector2 pos = new Vector2(rect.X + (rect.Width / 2f) - (textSize.X / 2f), rect.Y);

                SpriteBatch.DrawString(_menuFont, text, pos, color);
            }

            SpriteBatch.End();
        }

        private string GenerateBar(int value)
        {
            StringBuilder sb = new StringBuilder("[");
            value = Math.Max(0, Math.Min(10, value));
            sb.Append('|', value);
            sb.Append('-', 10 - value);
            sb.Append("]");
            return sb.ToString();
        }
    }
}
