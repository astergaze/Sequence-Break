using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class InteractionPanel
    {
        // Estado
        public bool IsActive { get; private set; }
        public event Action<int> OnOptionSelected;

        // Assets
        private SpriteFont _font;
        private Texture2D _pixel; // Para dibujar fondos y el indicador

        // Contenido Dinamico
        private string _dialogueText; // El texto COMPLETO de la pagina actual
        private string _speakerName;
        private List<string> _options;
        private int _selectedOption;

        // paginacion logica
        private List<string> _textPages;
        private int _currentPageIndex;

        // logica animacion de escritura
        private int _visibleCharacters = 0;
        private float _typewriterTimer = 0f;

        // Ajusta esto para cambiar la velocidad. 1f / 40f = 40 caracteres por segundo.
        private const float TYPEWRITER_SPEED = 1f / 40f;
        private bool _isTyping = false;

        // Indicador de Paginacion
        private float _paginationIndicatorTime = 0f;
        private Vector2 _paginationIndicatorBasePosition;
        private const float INDICATOR_SPEED = 4.0f;
        private const float INDICATOR_AMPLITUDE = 5.0f;
        private const int INDICATOR_SIZE = 10;

        // Posicionamiento y Estilo
        private const float TEXT_SCALE = 0.6f;
        private Rectangle _mainTextBox;
        private Rectangle _optionsBox;
        private Rectangle _speakerBox;

        private Vector2 _dialogueTextPosition;
        private Vector2 _optionsStartPosition;
        private float _optionSpacing = 10f;
        private Color _menuNormalColor = Color.White;
        private Color _menuSelectedColor = new Color(112, 56, 168);
        private Color _speakerTextColor = new Color(124, 176, 255);
        private Color _textBoxColor = Color.Black * 0.50f;

        // Control
        private KeyboardState _previousKeyboardState;

        public InteractionPanel(
            SpriteFont font,
            TextureAtlas uiAtlas,
            GraphicsDevice graphicsDevice
        )
        {
            _font = font;
            _textPages = new List<string>();

            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // definir tamaño
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            const int padding = 40;

            int mainHeight = 200;
            _mainTextBox = new Rectangle(0, 0, screenWidth, mainHeight);
            _dialogueTextPosition = new Vector2(padding, padding);

            int speakerWidth = 300;
            int speakerHeight = 80;
            _speakerBox = new Rectangle(
                screenWidth - speakerWidth - padding,
                mainHeight + 5,
                speakerWidth,
                speakerHeight
            );

            _optionsBox = new Rectangle(padding, screenHeight - 200 - padding, 400, 200);
            _optionsStartPosition = new Vector2(_optionsBox.X, _optionsBox.Y);

            _paginationIndicatorBasePosition = new Vector2(
                _mainTextBox.Center.X - (INDICATOR_SIZE / 2),
                _mainTextBox.Bottom - INDICATOR_SIZE - 10
            );
        }

        public void Show(string text, List<string> options = null, string speaker = null)
        {
            _speakerName = speaker;
            _options = options;
            _selectedOption = 0;

            float textPaddingX = _dialogueTextPosition.X;
            float textPaddingY = _dialogueTextPosition.Y;
            PaginateText(
                text,
                _mainTextBox.Width - (textPaddingX * 2),
                _mainTextBox.Height - (textPaddingY * 2)
            );

            _currentPageIndex = 0;
            _dialogueText = _textPages[_currentPageIndex]; // Carga el texto completo de la pág. 0
            StartTyping(); // Inicia el efecto typewriter

            _previousKeyboardState = Keyboard.GetState();
            IsActive = true;
        }

        // Reinicia el estado del typewriter para una nueva página.
        private void StartTyping()
        {
            _visibleCharacters = 0;
            _typewriterTimer = 0f;
            _isTyping = true;
            _paginationIndicatorTime = 0f; // Esconde el indicador de paginación
        }

        public void Hide()
        {
            IsActive = false;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;

            _paginationIndicatorTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Logica de Typewriter
            if (_isTyping)
            {
                _typewriterTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_typewriterTimer >= TYPEWRITER_SPEED)
                {
                    int charsToAdd = (int)(_typewriterTimer / TYPEWRITER_SPEED);
                    _typewriterTimer -= charsToAdd * TYPEWRITER_SPEED;

                    _visibleCharacters += charsToAdd;

                    if (_visibleCharacters >= _dialogueText.Length)
                    {
                        _isTyping = false;
                        _visibleCharacters = _dialogueText.Length;
                    }
                }
            }

            KeyboardState kbs = Keyboard.GetState();
            bool isLastPage = (_currentPageIndex == _textPages.Count - 1);
            bool hasOptions = (_options != null && _options.Count > 0);

            // Handle Input (Up/Down)
            // Solo permite navegar opciones si NO se está escribiendo y es la ultima página
            if (hasOptions && isLastPage && !_isTyping)
            {
                if (
                    kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W)
                    || kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up)
                )
                {
                    _selectedOption--;
                    if (_selectedOption < 0)
                        _selectedOption = _options.Count - 1;
                }
                if (
                    kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S)
                    || kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down)
                )
                {
                    _selectedOption++;
                    if (_selectedOption >= _options.Count)
                        _selectedOption = 0;
                }
            }

            // Handle Confirmation (Enter/E)
            if (
                kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter)
                || kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E)
            )
            {
                if (_isTyping)
                {
                    // Prioridad 1: Si está escribiendo, salta y muestra todo el texto
                    _isTyping = false;
                    _visibleCharacters = _dialogueText.Length;
                }
                else if (!isLastPage)
                {
                    // Prioridad 2: Si no está escribiendo y no es la última página, avanza de página
                    _currentPageIndex++;
                    _dialogueText = _textPages[_currentPageIndex];
                    StartTyping(); // Reinicia el efecto para la nueva página
                }
                else
                {
                    // Prioridad 3: Si no está escribiendo y ES la última página, selecciona o cierra
                    if (hasOptions)
                    {
                        OnOptionSelected?.Invoke(_selectedOption);
                    }
                    Hide();
                }
            }
            _previousKeyboardState = kbs;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            // Dibuja la Caja de Texto Principal
            spriteBatch.Draw(_pixel, _mainTextBox, _textBoxColor);

            // Dibuja solo los caracteres visibles
            string textToDraw = "";
            if (_visibleCharacters > 0)
            {
                textToDraw = _dialogueText.Substring(0, _visibleCharacters);
            }

            spriteBatch.DrawString(
                _font,
                textToDraw,
                _dialogueTextPosition,
                Color.White,
                0f,
                Vector2.Zero,
                TEXT_SCALE,
                SpriteEffects.None,
                0f
            );

            bool isLastPage = (_currentPageIndex == _textPages.Count - 1);
            bool hasOptions = (_options != null && _options.Count > 0);

            // Indicador de paginacion
            // Solo se muestra si NO se está escribiendo y NO es la última página
            if (!isLastPage && !_isTyping)
            {
                float bounceOffset =
                    (float)Math.Sin(_paginationIndicatorTime * INDICATOR_SPEED)
                    * INDICATOR_AMPLITUDE;
                Rectangle indicatorRect = new Rectangle(
                    (int)_paginationIndicatorBasePosition.X,
                    (int)(_paginationIndicatorBasePosition.Y + bounceOffset),
                    INDICATOR_SIZE,
                    INDICATOR_SIZE
                );
                spriteBatch.Draw(_pixel, indicatorRect, Color.White);
            }

            // Dibuja las Opciones
            // Solo se muestran si NO se está escribiendo y ES la última página
            if (hasOptions && isLastPage && !_isTyping)
            {
                float longestOptionWidth = 0;
                foreach (string option in _options)
                {
                    float currentWidth = _font.MeasureString($"[ {option} ]").X * TEXT_SCALE;
                    if (currentWidth > longestOptionWidth)
                    {
                        longestOptionWidth = currentWidth;
                    }
                }

                float optionBoxWidth = Math.Max(longestOptionWidth + 40, _optionsBox.Width);
                float currentOptionY = _optionsStartPosition.Y;
                float optionBoxHeight = (_font.LineSpacing * TEXT_SCALE + 10);

                for (int i = 0; i < _options.Count; i++)
                {
                    Color textColor =
                        (i == _selectedOption) ? _menuSelectedColor : _menuNormalColor;
                    string optionText =
                        (i == _selectedOption) ? $"[ {_options[i]} ]" : $"  {_options[i]}  ";

                    Rectangle optionRect = new Rectangle(
                        (int)_optionsStartPosition.X,
                        (int)currentOptionY,
                        (int)optionBoxWidth,
                        (int)optionBoxHeight
                    );

                    spriteBatch.Draw(_pixel, optionRect, _textBoxColor);

                    Vector2 textPosition = new Vector2(optionRect.X + 10, optionRect.Y + 5);

                    spriteBatch.DrawString(
                        _font,
                        optionText,
                        textPosition,
                        textColor,
                        0f,
                        Vector2.Zero,
                        TEXT_SCALE,
                        SpriteEffects.None,
                        0f
                    );
                    currentOptionY += optionRect.Height + _optionSpacing;
                }
            }

            // Dibuja el Hablante
            if (!string.IsNullOrEmpty(_speakerName))
            {
                spriteBatch.Draw(_pixel, _speakerBox, _textBoxColor);

                Vector2 speakerTextSize = _font.MeasureString(_speakerName) * TEXT_SCALE;
                Vector2 speakerTextPos = new Vector2(
                    _speakerBox.X + (_speakerBox.Width - speakerTextSize.X) / 2,
                    _speakerBox.Y + (_speakerBox.Height - speakerTextSize.Y) / 2
                );

                spriteBatch.DrawString(
                    _font,
                    _speakerName,
                    speakerTextPos,
                    _speakerTextColor,
                    0f,
                    Vector2.Zero,
                    TEXT_SCALE,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        // METODOS DE AYUDA (PAGINACIÓN Y WRAPPING)

        private void PaginateText(string text, float maxWidth, float maxHeight)
        {
            _textPages.Clear();
            if (_font == null || string.IsNullOrEmpty(text))
            {
                _textPages.Add("");
                return;
            }

            string wrappedText = WrapText(text, maxWidth);
            string[] lines = wrappedText.Split('\n');

            int linesPerPage = (int)(maxHeight / (_font.LineSpacing * TEXT_SCALE));
            if (linesPerPage <= 0)
                linesPerPage = 1;

            StringBuilder pageBuilder = new StringBuilder();
            int lineCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                pageBuilder.AppendLine(lines[i]);
                lineCount++;

                if (lineCount >= linesPerPage || i == lines.Length - 1)
                {
                    _textPages.Add(pageBuilder.ToString().TrimEnd('\n', '\r'));
                    pageBuilder.Clear();
                    lineCount = 0;
                }
            }

            if (_textPages.Count == 0)
            {
                _textPages.Add("");
            }
        }

        private string WrapText(string text, float maxLineWidth)
        {
            if (_font == null)
                return text;

            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;

            float spaceWidth = _font.MeasureString(" ").X * TEXT_SCALE;

            foreach (string word in words)
            {
                Vector2 size = _font.MeasureString(word) * TEXT_SCALE;

                if (size.X > maxLineWidth && lineWidth == 0)
                {
                    sb.Append(word + "\n");
                    lineWidth = 0f;
                }
                else if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString().TrimEnd(' ');
        }
    }
}
