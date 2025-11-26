using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Sequence_Break
{
    public class MainMenuScreen : Screen
    {
        private SpriteFont _titleFont;
        private SpriteFont _menuFont;
        private Random _random;

        private Song _menuMusic;

        // Logica de Glitch
        private float _glitchTimer;
        private bool _isGlitching;

        // Opciones del Menu
        private string[] _menuOptions =
        {
            "[ EMPEZAR ]",
            "[ CONTINUAR ]",
            "[ OPCIONES ]",
            "[ SALIR ]",
        };
        private string[] _menuOptionsHover =
        {
            "[ SINTONIZAR ]",
            "[ RECORDAR ]",
            "[ ALTERAR CONSTANTES ]",
            "[ ESCAPAR ]",
        };
        private int _selectedMenuIndex = -1;
        private List<Rectangle> _menuOptionRects;
        private Color _menuNormalColor = Color.White;
        private Color _menuHoverColor = new Color(200, 100, 255);
        private Color _menuDisabledColor = Color.Gray * 0.5f; // Color para cuando no hay savefile

        // Mouse
        private MouseState _previousMouseState;

        // Estado del Savefile
        private bool _hasSaveFile;

        public MainMenuScreen(Game1 game)
            : base(game)
        {
            _random = new Random();
            _menuOptionRects = new List<Rectangle>();
        }

        public override void LoadContent()
        {
            _titleFont = Content.Load<SpriteFont>("fonts/BebasNeue");
            _menuFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");

            // Verificar si existe partida guardada al cargar el menú
            _hasSaveFile = SaveManager.SaveFileExists();

            try
            {
                _menuMusic = Content.Load<Song>("audio/MaybeMain");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la musica del menu: {ex.Message}");
                _menuMusic = null;
            }

            float maxWidth = 0f;
            foreach (string option in _menuOptionsHover)
            {
                float width = _menuFont.MeasureString(option).X;
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }

            float menuY = 350f;
            float menuSpacing = 50f;
            float windowWidth = GraphicsDevice.Viewport.Width;
            float itemHeight = _menuFont.MeasureString("A").Y;
            float posX = (windowWidth - maxWidth) / 2f;

            for (int i = 0; i < _menuOptions.Length; i++)
            {
                float posY = menuY + (i * menuSpacing);
                _menuOptionRects.Add(
                    new Rectangle((int)posX, (int)posY, (int)maxWidth, (int)itemHeight)
                );
            }

            _previousMouseState = Mouse.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            // Usar SettingsManager para el volumen
            if (_menuMusic != null && MediaPlayer.Queue.ActiveSong != _menuMusic)
            {
                MediaPlayer.Play(_menuMusic);
                MediaPlayer.IsRepeating = true;
                SettingsManager.ApplyMusicVolume(); // Aplica el volumen guardado
            }

            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            if (kbs.IsKeyDown(Keys.Escape))
                _game.Exit();

            // Glitch Logic
            _glitchTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_isGlitching)
            {
                if (_glitchTimer <= 0)
                {
                    _isGlitching = false;
                    _glitchTimer = (float)_random.NextDouble() * 3.0f + 1.0f;
                }
            }
            else
            {
                if (_glitchTimer <= 0)
                {
                    _isGlitching = true;
                    _glitchTimer = (float)_random.NextDouble() * 0.1f + 0.1f;
                }
            }

            // Mouse Hover
            _selectedMenuIndex = -1;
            Point mousePosition = ms.Position;
            for (int i = 0; i < _menuOptionRects.Count; i++)
            {
                if (_menuOptionRects[i].Contains(mousePosition))
                {
                    // Si es la opcion CONTINUAR y no hay savefile, no la seleccionamos
                    if (i == 1 && !_hasSaveFile)
                        continue;

                    _selectedMenuIndex = i;
                    break;
                }
            }

            // Mouse Click
            if (
                _selectedMenuIndex != -1
                && ms.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released
            )
            {
                switch (_selectedMenuIndex)
                {
                    case 0: // EMPEZAR
                        // Reiniciamos el bucle (borramos todo)
                        PlayerStatus.Initialize();

                        MediaPlayer.Stop();
                        _game.IsMouseVisible = false;
                        _game.ChangeScreen(new GameplayScreen(_game));
                        break;

                    case 1: // CONTINUAR
                        if (_hasSaveFile)
                        {
                            // Cargamos los datos del disco a la memoria RAM (PlayerStatus)
                            if (SaveManager.LoadGame())
                            {
                                MediaPlayer.Stop();
                                _game.IsMouseVisible = false;

                                // Cargamos GameplayScreen. Como los datos ya estan en PlayerStatus,
                                // el juego usara la vida e items guardados.
                                // TO DO: hacer que spawnee al lado de la cama
                                _game.ChangeScreen(new GameplayScreen(_game));
                            }
                        }
                        break;

                    case 2: // OPCIONES
                        _game.ChangeScreen(new OptionsScreen(_game));
                        break;

                    case 3: // SALIR
                        MediaPlayer.Stop();
                        _game.Exit();
                        break;
                }
            }

            _previousMouseState = ms;
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Titulo
            string title = "SEQUENCE BREAK";
            Vector2 titleSize = _titleFont.MeasureString(title);
            Vector2 titlePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - titleSize.X) / 2f,
                150f
            );

            if (_isGlitching)
            {
                System.Text.StringBuilder glitchedTitle = new System.Text.StringBuilder(title);
                int glitchCount = _random.Next(1, 3);
                for (int j = 0; j < glitchCount; j++)
                {
                    int pos = _random.Next(glitchedTitle.Length);
                    glitchedTitle[pos] = '█';
                }
                string titleToDraw = glitchedTitle.ToString();
                float offsetX = (_random.NextSingle() * 8f) - 4f;
                float offsetY = (_random.NextSingle() * 8f) - 4f;
                SpriteBatch.DrawString(
                    _titleFont,
                    titleToDraw,
                    titlePosition + new Vector2(offsetX, 0),
                    Color.Red * 0.7f
                );
                SpriteBatch.DrawString(
                    _titleFont,
                    titleToDraw,
                    titlePosition + new Vector2(0, offsetY),
                    Color.Cyan * 0.7f
                );
                SpriteBatch.DrawString(_titleFont, titleToDraw, titlePosition, Color.White * 0.9f);
            }
            else
            {
                SpriteBatch.DrawString(_titleFont, title, titlePosition, Color.White);
            }

            // Opciones del Menu
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                // Determinamos el estado
                bool isSelected = (_selectedMenuIndex == i);
                bool isDisabled = (i == 1 && !_hasSaveFile); // Deshabilitar "Continuar" si no hay save

                string text = isSelected ? _menuOptionsHover[i] : _menuOptions[i];

                Color color = _menuNormalColor;
                if (isDisabled)
                    color = _menuDisabledColor;
                else if (isSelected)
                    color = _menuHoverColor;

                Vector2 textSize = _menuFont.MeasureString(text);
                Rectangle rect = _menuOptionRects[i];
                float posX = rect.X + (rect.Width / 2f) - (textSize.X / 2f);
                float posY = rect.Y;

                SpriteBatch.DrawString(_menuFont, text, new Vector2(posX, posY), color);
            }

            SpriteBatch.End();
        }
    }
}
