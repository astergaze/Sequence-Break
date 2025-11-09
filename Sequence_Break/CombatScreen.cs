using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class CombatScreen : Screen
    {
        // clases de combatientes
        public class Combatant
        {
            public string Name { get; set; }
            public int CurrentHP { get; set; }
            public int MaxHP { get; set; }
            public Texture2D Sprite { get; set; } // por si acaso
            public Vector2 Position { get; set; }
        }

        public class Player : Combatant
        {
            public int CurrentCordura { get; set; }
            public int MaxCordura { get; set; }
            public int Balas { get; set; }
            public int MaxBalas { get; set; }
        }

        public class Enemy : Combatant
        {
            public AnimatedSprite AnimatedSprite { get; set; }
        }

        // variables de la UI de batalla
        private SpriteFont _uiFont;
        private Texture2D _pixel;
        private Texture2D _backgroundTexture;

        // Sprites
        private TextureAtlas _enemyAtlas;
        private const float ENEMY_SCALE = 4.0f;
        private TextureAtlas _specterAtlas;
        private AnimatedSprite _specterSprite;
        private Vector2 _specterPosition;
        private const float PLAYER_SCALE = 3.0f;

        // Combatientes
        private Player _player;
        private Enemy _enemy;

        // Estados del combate
        private enum CombatState
        {
            Start,
            PlayerSelectAction,
            PlayerAction,
            EnemyTurn,
            EnemyAction,
            Won,
            Lost,
            ShowMessage,
            Won_End,
            Lost_End,
        }

        private CombatState _currentState;
        private CombatState _nextState;

        // logica menu de combate
        private string[] _menuOptions = { "ATAQUE", "GLITCH", "DEFENSA", "OBJETOS", "ESCAPAR" };
        private int _selectedOption = 0;

        // logica atlas UI
        private TextureAtlas _uiAtlas;
        private Sprite _uiTopLeft,
            _uiTopCenter,
            _uiTopRight;
        private Sprite _uiMiddleLeft,
            _uiMiddleCenter,
            _uiMiddleRight;
        private Sprite _uiBottomLeft,
            _uiBottomCenter,
            _uiBottomRight;

        // Posiciones y Colores de la UI
        private Rectangle _uiBoxMain;
        private Rectangle _uiBoxLeft;
        private Vector2 _menuStartPosition;
        private Color _menuNormalColor = Color.White;
        private Color _menuSelectedColor = new Color(112, 56, 168);
        private Color _hpColor = new Color(111, 19, 175);
        private Color _corduraColor = new Color(124, 176, 255);
        private Color _barBackgroundColor = new Color(40, 40, 40);

        // Panel de Interaccion
        private InteractionPanel _interactionPanel;

        private KeyboardState _previousKeyboardState;

        // Variables para guardar el estado de retorno
        private string _returnMapName;
        private Vector2 _returnPosition;

        public CombatScreen(Game1 game, string returnMap, Vector2 returnPos)
            : base(game)
        {
            _returnMapName = returnMap;
            _returnPosition = returnPos;
        }

        public override void LoadContent()
        {
            _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");

            try
            {
                _backgroundTexture = Content.Load<Texture2D>("Interface/Combat/battle_background");
            }
            catch (Exception)
            {
                Console.WriteLine(
                    "ERROR: No se pudo cargar la imagen de fondo 'Interface/Combat/battle_background'."
                );
                throw;
            }

            // Cargar Atlas del Enemigo
            try
            {
                _enemyAtlas = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-1-texture-atlas.xml"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando el atlas del enemigo: {ex.Message}");
                throw;
            }
            AnimatedSprite enemyAnimatedSprite = _enemyAtlas.CreateAnimatedSprite("enemy-attack");
            enemyAnimatedSprite.Scale = new Vector2(ENEMY_SCALE, ENEMY_SCALE);

            // Cargar Atlas de Specter
            try
            {
                _specterAtlas = TextureAtlas.FromFile(
                    Content,
                    "textures/Specter-right-atlas-definition.xml"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando el atlas de Specter: {ex.Message}");
                throw;
            }
            _specterSprite = _specterAtlas.CreateAnimatedSprite("luka-walk-right");
            _specterSprite.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Cargar el Atlas de la UI
            _uiAtlas = TextureAtlas.FromFile(
                Content,
                "Interface/Combat/interface-combat-atlas-definition.xml"
            );

            // Asignar las 9 partes
            _uiTopLeft = _uiAtlas.CreateAnimatedSprite("top-left");
            _uiTopCenter = _uiAtlas.CreateAnimatedSprite("top-center");
            _uiTopRight = _uiAtlas.CreateAnimatedSprite("top-right");
            _uiMiddleLeft = _uiAtlas.CreateAnimatedSprite("middle-left");
            _uiMiddleCenter = _uiAtlas.CreateAnimatedSprite("middle-center");
            _uiMiddleRight = _uiAtlas.CreateAnimatedSprite("middle-right");
            _uiBottomLeft = _uiAtlas.CreateAnimatedSprite("down-left");
            _uiBottomCenter = _uiAtlas.CreateAnimatedSprite("down-center");
            _uiBottomRight = _uiAtlas.CreateAnimatedSprite("down-right");

            // Definir las áreas de la UI
            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int uiHeight = 250;
            _uiBoxMain = new Rectangle(0, screenHeight - uiHeight, screenWidth, uiHeight);
            _uiBoxLeft = new Rectangle(
                _uiBoxMain.X,
                _uiBoxMain.Y,
                (int)(_uiBoxMain.Width * 0.25f),
                _uiBoxMain.Height
            );
            _menuStartPosition = new Vector2(_uiBoxMain.X + 20, _uiBoxLeft.Y + 20);

            // Inicializar combatientes
            _player = new Player
            {
                Name = "Luka Specter",
                CurrentHP = 100,
                MaxHP = 100,
                CurrentCordura = 100,
                MaxCordura = 100,
                Balas = 12,
                MaxBalas = 12,
            };

            // Posiciones de Combatientes
            float combatantY = screenHeight / 2 - 150;
            _specterPosition = new Vector2(200, combatantY);

            float enemyScaledWidth = enemyAnimatedSprite.Region.SourceRectangle.Width * ENEMY_SCALE;
            _enemy = new Enemy
            {
                Name = "Disonancia",
                CurrentHP = 80,
                MaxHP = 80,
                AnimatedSprite = enemyAnimatedSprite,
                Position = new Vector2(screenWidth - enemyScaledWidth - 200, combatantY),
            };

            // Inicializar el panel de interacción
            _interactionPanel = new InteractionPanel(_uiFont, _uiAtlas, GraphicsDevice);

            // Empezar el combate
            _currentState = CombatState.Start;
            _previousKeyboardState = Keyboard.GetState();
        }

        private void ShowCombatMessage(string text, CombatState nextState, string speaker = null)
        {
            _interactionPanel.Show(text, null, speaker);
            _nextState = nextState;
            _currentState = CombatState.ShowMessage; // Pausa el juego
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            _enemy.AnimatedSprite.Update(gameTime);
            _specterSprite.Update(gameTime);

            if (_interactionPanel.IsActive)
            {
                _interactionPanel.Update(gameTime);
                if (!_interactionPanel.IsActive)
                {
                    // Cuando el panel se cierra, pasamos al siguiente estado
                    _currentState = _nextState;
                }
                _previousKeyboardState = currentKeyboardState;
                return; // No procesar nada más
            }

            switch (_currentState)
            {
                case CombatState.Start:
                    ShowCombatMessage(
                        $"{_enemy.Name} Inicia el combate",
                        CombatState.PlayerSelectAction,
                        null
                    );
                    break;

                case CombatState.PlayerSelectAction:
                    HandlePlayerInput(currentKeyboardState);
                    break;

                case CombatState.PlayerAction:
                    if (_enemy.CurrentHP <= 0)
                    {
                        _currentState = CombatState.Won;
                    }
                    else
                    {
                        _currentState = CombatState.EnemyTurn;
                    }
                    break;

                case CombatState.EnemyTurn:
                    ShowCombatMessage($"Turno de {_enemy.Name}.", CombatState.EnemyAction, null);
                    break;

                case CombatState.EnemyAction:
                    _player.CurrentHP -= 10;
                    ShowCombatMessage(
                        $"{_enemy.Name} ataca. -10 HP",
                        _player.CurrentHP <= 0 ? CombatState.Lost : CombatState.PlayerSelectAction,
                        null
                    );
                    break;

                case CombatState.Won:
                    ShowCombatMessage("COMBATE GANADO!", CombatState.Won_End);
                    break;

                case CombatState.Won_End:
                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;

                case CombatState.Lost:
                    ShowCombatMessage("HAS CAIDO...", CombatState.Lost_End);
                    break;

                case CombatState.Lost_End:
                    _game.ChangeScreen(new GameplayScreen(_game));
                    break;
            }

            _previousKeyboardState = currentKeyboardState;
        }

        private void HandlePlayerInput(KeyboardState kbs)
        {
            if (
                kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W)
                || kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up)
            )
            {
                _selectedOption--;
                if (_selectedOption < 0)
                    _selectedOption = _menuOptions.Length - 1;
            }

            if (
                kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S)
                || kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down)
            )
            {
                _selectedOption++;
                if (_selectedOption >= _menuOptions.Length)
                    _selectedOption = 0;
            }

            if (
                kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter)
                || kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E)
            )
            {
                PerformPlayerAction();
            }
        }

        private void PerformPlayerAction()
        {
            string action = _menuOptions[_selectedOption];
            string message = "";
            string speaker = null;

            switch (action)
            {
                case "ATAQUE":
                    message = "Luka ataca! HP del enemigo -25.";
                    _enemy.CurrentHP -= 25;
                    ShowCombatMessage(message, CombatState.PlayerAction, speaker);
                    break;
                case "GLITCH":
                    message = "Luka usa GLITCH... (no implementado).";
                    ShowCombatMessage(message, CombatState.PlayerAction, speaker);
                    break;
                case "DEFENSA":
                    message = "Luka se defiende... (no implementado).";
                    ShowCombatMessage(message, CombatState.PlayerAction, speaker);
                    break;
                case "OBJETOS":
                    Console.WriteLine("Abriendo inventario... (no implementado)");
                    break;
                case "ESCAPAR":
                    Console.WriteLine("Intentando escapar...");
                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Dibujar Fondo
            SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            SpriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);
            SpriteBatch.End();

            // Dibujar Sprites y UI de Combate
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            const float uiScale = 0.8f;

            // Dibujar peleadores
            _specterSprite.Draw(SpriteBatch, _specterPosition);
            _enemy.AnimatedSprite.Draw(SpriteBatch, _enemy.Position);

            bool showCombatUI =
                _currentState != CombatState.Start
                && _currentState != CombatState.Won
                && _currentState != CombatState.Lost
                && _currentState != CombatState.Won_End
                && _currentState != CombatState.Lost_End;

            if (showCombatUI)
            {
                // Dibujar las cajas de la UI
                DrawNineSlicePanel(SpriteBatch, _uiBoxMain);
                DrawNineSlicePanel(SpriteBatch, _uiBoxLeft);

                // Dibujar el menú (Caja Izquierda)
                // El menú siempre es visible, pero atenuado si no es seleccionable
                for (int i = 0; i < _menuOptions.Length; i++)
                {
                    Color color;
                    if (_currentState == CombatState.PlayerSelectAction)
                    {
                        // Turno del jugador: usar colores normales y seleccionados
                        color = (i == _selectedOption) ? _menuSelectedColor : _menuNormalColor;
                    }
                    else
                    {
                        // No es el turno: atenuar todos los colores
                        color =
                            ((i == _selectedOption) ? _menuSelectedColor : _menuNormalColor) * 0.5f;
                    }

                    string optionText = $"[ {_menuOptions[i]} ]";
                    Vector2 position = new Vector2(
                        _menuStartPosition.X,
                        _menuStartPosition.Y + (i * 40)
                    );
                    SpriteBatch.DrawString(
                        _uiFont,
                        optionText,
                        position,
                        color,
                        0f,
                        Vector2.Zero,
                        uiScale,
                        SpriteEffects.None,
                        0f
                    );
                }

                // Dibujar Stats (Caja Derecha)
                float padding = 30f;
                float statsAreaX = _uiBoxLeft.Right + padding;
                float rightAlignX = _uiBoxMain.Right - padding;
                float currentY = _uiBoxLeft.Top + 20;

                Vector2 namePosition = new Vector2(statsAreaX, currentY);
                SpriteBatch.DrawString(
                    _uiFont,
                    _player.Name,
                    namePosition,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    uiScale,
                    SpriteEffects.None,
                    0f
                );

                string balasText = $"Balas: {_player.Balas}/{_player.MaxBalas}";
                Vector2 balasTextSize = _uiFont.MeasureString(balasText) * uiScale;
                Vector2 balasPosition = new Vector2(rightAlignX - balasTextSize.X, currentY);
                SpriteBatch.DrawString(
                    _uiFont,
                    balasText,
                    balasPosition,
                    Color.Yellow,
                    0f,
                    Vector2.Zero,
                    uiScale,
                    SpriteEffects.None,
                    0f
                );

                currentY += 45;

                float hpLabelWidth = _uiFont.MeasureString("HP").X * uiScale;
                float corduraLabelWidth = _uiFont.MeasureString("Cordura").X * uiScale;
                float maxLabelWidth = Math.Max(hpLabelWidth, corduraLabelWidth);
                float barStartX = statsAreaX + maxLabelWidth + 10;
                string maxValueText = "100/100";
                float valueTextWidth = _uiFont.MeasureString(maxValueText).X * uiScale;
                float valueTextStartX = rightAlignX - valueTextWidth;
                float fixedBarWidth = valueTextStartX - barStartX - 10;
                Vector2 barRowPosition = new Vector2(statsAreaX, currentY);

                DrawStatBar(
                    "HP",
                    _player.CurrentHP,
                    _player.MaxHP,
                    barRowPosition,
                    _hpColor,
                    uiScale,
                    barStartX,
                    fixedBarWidth,
                    valueTextStartX
                );

                currentY += 35;
                barRowPosition.Y = currentY;

                DrawStatBar(
                    "Cordura",
                    _player.CurrentCordura,
                    _player.MaxCordura,
                    barRowPosition,
                    _corduraColor,
                    uiScale,
                    barStartX,
                    fixedBarWidth,
                    valueTextStartX
                );
            }

            SpriteBatch.End();

            // Dibujar el Panel de Interacción (SIEMPRE AL FINAL, ENCIMA DE TODO)
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _interactionPanel.Draw(gameTime, SpriteBatch);
            SpriteBatch.End();
        }

        // Métodos de Ayuda para Dibujar

        private void DrawStatBar(
            string label,
            int current,
            int max,
            Vector2 position,
            Color barColor,
            float uiScale,
            float barStartX,
            float barWidth,
            float valueTextStartX
        )
        {
            SpriteBatch.DrawString(
                _uiFont,
                label,
                position,
                barColor,
                0f,
                Vector2.Zero,
                uiScale,
                SpriteEffects.None,
                0f
            );

            Vector2 labelSize = _uiFont.MeasureString(label) * uiScale;
            int barHeight = 20;
            float barY = position.Y + (labelSize.Y / 2) - (barHeight / 2);
            string statText = $"{current}/{max}";
            Vector2 textPos = new Vector2(valueTextStartX, position.Y);

            SpriteBatch.DrawString(
                _uiFont,
                statText,
                textPos,
                barColor,
                0f,
                Vector2.Zero,
                uiScale,
                SpriteEffects.None,
                0f
            );

            if (barWidth < 0)
                barWidth = 0;

            float percent = (float)current / max;
            if (percent < 0)
                percent = 0;
            if (percent > 1)
                percent = 1;

            Rectangle bgRect = new Rectangle((int)barStartX, (int)barY, (int)barWidth, barHeight);
            SpriteBatch.Draw(_pixel, bgRect, _barBackgroundColor);

            Rectangle fgRect = new Rectangle(
                (int)barStartX,
                (int)barY,
                (int)(barWidth * percent),
                barHeight
            );
            SpriteBatch.Draw(_pixel, fgRect, barColor);
        }

        private void DrawCenterText(string text, float textScale)
        {
            Vector2 textSize = _uiFont.MeasureString(text) * textScale;
            Vector2 position = new Vector2(
                (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                (GraphicsDevice.Viewport.Height - textSize.Y) / 2
            );

            SpriteBatch.DrawString(
                _uiFont,
                text,
                position + new Vector2(2, 2),
                Color.Black,
                0f,
                Vector2.Zero,
                textScale,
                SpriteEffects.None,
                0f
            );
            SpriteBatch.DrawString(
                _uiFont,
                text,
                position,
                Color.White,
                0f,
                Vector2.Zero,
                textScale,
                SpriteEffects.None,
                0f
            );
        }

        private void DrawNineSlicePanel(SpriteBatch spriteBatch, Rectangle destination)
        {
            const int sourceSpriteSize = 64;
            const float scale = 1.0f;
            int cornerSize = (int)(sourceSpriteSize * scale);

            Texture2D texture = _uiTopLeft.Region.Texture;

            // Esquinas
            spriteBatch.Draw(
                texture,
                new Rectangle(destination.X, destination.Y, cornerSize, cornerSize),
                _uiTopLeft.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y,
                    cornerSize,
                    cornerSize
                ),
                _uiTopRight.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Bottom - cornerSize,
                    cornerSize,
                    cornerSize
                ),
                _uiBottomLeft.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Bottom - cornerSize,
                    cornerSize,
                    cornerSize
                ),
                _uiBottomRight.Region.SourceRectangle,
                Color.White
            );

            // Bordes (con corrección de inflate)
            Rectangle topCenterSource = _uiTopCenter.Region.SourceRectangle;
            topCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                topCenterSource,
                Color.White
            );

            Rectangle bottomCenterSource = _uiBottomCenter.Region.SourceRectangle;
            bottomCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Bottom - cornerSize,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                bottomCenterSource,
                Color.White
            );

            Rectangle middleLeftSource = _uiMiddleLeft.Region.SourceRectangle;
            middleLeftSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                middleLeftSource,
                Color.White
            );

            Rectangle middleRightSource = _uiMiddleRight.Region.SourceRectangle;
            middleRightSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                middleRightSource,
                Color.White
            );

            // Centro
            Rectangle middleCenterSource = _uiMiddleCenter.Region.SourceRectangle;
            middleCenterSource.Inflate(-1, -1);
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y + cornerSize,
                    destination.Width - (cornerSize * 2),
                    destination.Height - (cornerSize * 2)
                ),
                middleCenterSource,
                Color.White
            );
        }
    }
}
