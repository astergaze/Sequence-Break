using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sequence_Break
{
    public class MainMenuScreen : Screen
    {
        private SpriteFont _titleFont; // Bebas neue
        private SpriteFont _menuFont; // IBM Plex Mono
        private Random _random;

        // Lógica de Glitch
        private float _glitchTimer;
        private bool _isGlitching;

        // Opciones del Menú
        private string[] _menuOptions =
        {
            "[ EMPEZAR ]", //Lamentablemente no puedo ni usar 「 」 ni ⸢⸥
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
        private int _selectedMenuIndex = -1; // -1 = ninguno
        private List<Rectangle> _menuOptionRects;
        private Color _menuNormalColor = Color.White;
        private Color _menuHoverColor = new Color(200, 100, 255); // morado

        // Mouse
        private MouseState _previousMouseState;

        public MainMenuScreen(Game1 game)
            : base(game)
        {
            _random = new Random();
            _menuOptionRects = new List<Rectangle>();
        }

        public override void LoadContent()
        {
            // Cargar las fuentes
            _titleFont = Content.Load<SpriteFont>("fonts/BebasNeue");
            _menuFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");

            // Encontrar el ancho maximo midiendo las opciones de hover
            float maxWidth = 0f;
            foreach (string option in _menuOptionsHover)
            {
                float width = _menuFont.MeasureString(option).X;
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }

            // Usar el ancho para crear los rectángulos
            float menuY = 350f;
            float menuSpacing = 50f;
            float windowWidth = GraphicsDevice.Viewport.Width;

            // Medir la altura con letra de test
            float itemHeight = _menuFont.MeasureString("A").Y;

            // Calcular el posX (posición X) una sola vez para que todo se alinee
            float posX = (windowWidth - maxWidth) / 2f;

            for (int i = 0; i < _menuOptions.Length; i++)
            {
                float posY = menuY + (i * menuSpacing);
                _menuOptionRects.Add(
                    // Usamos el 'posX' calculado y el 'maxWidth' para todos
                    new Rectangle((int)posX, (int)posY, (int)maxWidth, (int)itemHeight)
                );
            }

            _previousMouseState = Mouse.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            // Salir del juego (global)
            if (kbs.IsKeyDown(Keys.Escape))
                _game.Exit();

            // Logica del Glitch del Titulo
            _glitchTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isGlitching)
            {
                // Si esta en glitch, ver si ya termino
                if (_glitchTimer <= 0)
                {
                    _isGlitching = false;
                    _glitchTimer = (float)_random.NextDouble() * 3.0f + 1.0f; // Espera de 1-4 seg
                }
            }
            else
            {
                // Si no esta en glitch, ver si debe empezar uno
                if (_glitchTimer <= 0)
                {
                    _isGlitching = true;
                    _glitchTimer = (float)_random.NextDouble() * 0.1f + 0.1f; // Duración de 0.1-0.2 seg
                }
            }

            // Hover y Click del Menu
            _selectedMenuIndex = -1;
            Point mousePosition = ms.Position;

            for (int i = 0; i < _menuOptionRects.Count; i++)
            {
                if (_menuOptionRects[i].Contains(mousePosition))
                {
                    _selectedMenuIndex = i;
                    break;
                }
            }

            // Comprobar click
            if (
                _selectedMenuIndex != -1
                && ms.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released
            )
            {
                switch (_selectedMenuIndex)
                {
                    case 0: // EMPEZAR (SINTONIZAR)
                        _game.IsMouseVisible = false; // Ocultar mouse en el juego
                        _game.ChangeScreen(new GameplayScreen(_game));
                        break;
                    case 1: // CONTINUAR
                        break;
                    case 2: // OPCIONES
                        break;
                    case 3: // SALIR
                        _game.Exit();
                        break;
                }
            }

            _previousMouseState = ms;
        }

        public override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.Clear(Color.Black);
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
                //Glitch

                // Crear un string "mutable" a partir del título
                System.Text.StringBuilder glitchedTitle = new System.Text.StringBuilder(title);

                // Decidir cuántos caracteres reemplazar (ej. 1 o 2)
                int glitchCount = _random.Next(1, 3);

                for (int j = 0; j < glitchCount; j++)
                {
                    // Elegir una posición aleatoria en el string
                    int pos = _random.Next(glitchedTitle.Length);

                    //  Reemplazar el carácter en esa posición con el bloque
                    glitchedTitle[pos] = '█';
                }

                // Convertir de nuevo a string
                string titleToDraw = glitchedTitle.ToString();

                // Dibujar 3 versiones con offset y color para el efecto "glitch"
                float offsetX = (_random.NextSingle() * 8f) - 4f;
                float offsetY = (_random.NextSingle() * 8f) - 4f;
                SpriteBatch.DrawString(
                    _titleFont,
                    titleToDraw, // Usar el título glitcheado
                    titlePosition + new Vector2(offsetX, 0),
                    Color.Red * 0.7f
                );
                SpriteBatch.DrawString(
                    _titleFont,
                    titleToDraw, // Usar el título glitcheado
                    titlePosition + new Vector2(0, offsetY),
                    Color.Cyan * 0.7f
                );
                SpriteBatch.DrawString(
                    _titleFont,
                    titleToDraw, // Usar el título glitcheado
                    titlePosition,
                    Color.White * 0.9f
                );
            }
            else
            {
                // Dibujo normal
                SpriteBatch.DrawString(_titleFont, title, titlePosition, Color.White);
            }

            // Opciones del Menu
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                bool isSelected = (_selectedMenuIndex == i);

                string text = isSelected ? _menuOptionsHover[i] : _menuOptions[i];
                Color color = isSelected ? _menuHoverColor : _menuNormalColor;

                // Medir el texto que vamos a dibujar
                Vector2 textSize = _menuFont.MeasureString(text);

                // Obtener  rectángulo ( basado en el ancho max)
                Rectangle rect = _menuOptionRects[i];

                // Calcular el 'X' para centrar este texto en ese rectángulo
                //    (rect.X) + (ancho del rect / 2) - (ancho de este texto / 2)
                float posX = rect.X + (rect.Width / 2f) - (textSize.X / 2f);

                // 4. El 'Y' es el mismo que el del rectángulo
                float posY = rect.Y;

                // 5. Dibujar en la nueva posición calculada
                SpriteBatch.DrawString(_menuFont, text, new Vector2(posX, posY), color);
            }

            SpriteBatch.End();
        }
    }
}
