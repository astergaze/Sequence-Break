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
        // --- Clases de ejemplo para los combatientes ---
        public class Combatant
        {
            public string Name { get; set; }
            public int CurrentHP { get; set; }
            public int MaxHP { get; set; }
            public Texture2D Sprite { get; set; } // Dejado por compatibilidad
            public Vector2 Position { get; set; }
        }

        public class Player : Combatant
        {
            public int CurrentCordura { get; set; }
            public int MaxCordura { get; set; }
            public int Balas { get; set; }
            public int MaxBalas { get; set; }
        }

        // --- INICIO DE CORRECCIÓN ---
        // La clase Enemy ahora guarda un AnimatedSprite (tipo correcto)
        public class Enemy : Combatant
        {
            public AnimatedSprite AnimatedSprite { get; set; }
        }

        // --- FIN DE CORRECCIÓN ---

        // --- Variables de la pantalla de combate ---

        // Fuentes y Texturas
        private SpriteFont _uiFont;
        private Texture2D _pixel; // Textura 1x1 para dibujar barras

        private TextureAtlas _enemyAtlas; // Atlas para el enemigo
        private const float ENEMY_SCALE = 4.0f; // Escalado del sprite del enemigo

        // Combatientes
        private Player _player;
        private Enemy _enemy;

        // Máquina de estados del combate
        private enum CombatState
        {
            Start,
            PlayerSelectAction,
            PlayerAction,
            EnemyTurn,
            EnemyAction,
            Won,
            Lost,
        }

        private CombatState _currentState;

        // Lógica del Menú de Combate
        private string[] _menuOptions = { "ATAQUE", "GLITCH", "DEFENSA", "OBJETOS", "ESCAPAR" };
        private int _selectedOption = 0;

        // Lógica del Atlas de la UI
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
        private Color _menuSelectedColor = new Color(112, 56, 168); // Morado brillante
        private Color _hpColor = new Color(111, 19, 175);
        private Color _corduraColor = new Color(124, 176, 255);
        private Color _barBackgroundColor = new Color(40, 40, 40);

        private KeyboardState _previousKeyboardState;

        // Variables para guardar el estado de retorno
        private string _returnMapName;
        private Vector2 _returnPosition;

        // Constructor MODIFICADO
        public CombatScreen(Game1 game, string returnMap, Vector2 returnPos)
            : base(game)
        {
            _returnMapName = returnMap;
            _returnPosition = returnPos;
        }

        public override void LoadContent()
        {
            // Cargar fuentes
            _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");

            // --- Cargar Atlas del Enemigo ---
            // ¡¡ASEGÚRATE DE QUE ESTA RUTA AL XML ES CORRECTA!!
            try
            {
                _enemyAtlas = TextureAtlas.FromFile(
                    Content,
                    "textures/enemies/demo/enemy-1-texture-atlas.xml" // <- CAMBIA ESTO por la ruta a tu XML
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando el atlas del enemigo: {ex.Message}");
                throw;
            }

            // --- INICIO DE CORRECCIÓN ---
            // Crear el sprite animado (tipo correcto)
            AnimatedSprite enemyAnimatedSprite = _enemyAtlas.CreateAnimatedSprite("enemy-attack");

            // Asignar la escala al objeto Sprite (como en GameplayScreen)
            enemyAnimatedSprite.Scale = new Vector2(ENEMY_SCALE, ENEMY_SCALE);
            // Se eliminó la línea .Play()
            // --- FIN DE CORRECCIÓN ---

            // Crear el píxel blanco para la UI
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

            _uiBoxMain = new Rectangle(
                0, // De borde a borde
                screenHeight - uiHeight, // Pegado abajo
                screenWidth, // Ancho completo
                uiHeight
            );

            _uiBoxLeft = new Rectangle(
                _uiBoxMain.X,
                _uiBoxMain.Y,
                (int)(_uiBoxMain.Width * 0.25f), // El 25% del ancho total
                _uiBoxMain.Height
            );

            // Ajustamos el padding basado en el borde de la caja
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

            // --- Inicializar Enemigo ---
            _enemy = new Enemy
            {
                Name = "Disonancia",
                CurrentHP = 80,
                MaxHP = 80,
                AnimatedSprite = enemyAnimatedSprite, // Asignar el sprite animado
                Position = new Vector2(
                    // Centrar el sprite escalado (usamos el ancho del *frame*)
                    screenWidth / 2
                        - (enemyAnimatedSprite.Region.SourceRectangle.Width * ENEMY_SCALE / 2),
                    screenHeight / 2 - 150
                ),
            };

            // Empezar el combate
            _currentState = CombatState.Start;
            _previousKeyboardState = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // Actualizar animación del enemigo (esto es correcto, como en tu GameplayScreen)
            _enemy.AnimatedSprite.Update(gameTime);

            // Máquina de estados
            switch (_currentState)
            {
                case CombatState.Start:
                    _currentState = CombatState.PlayerSelectAction;
                    break;

                case CombatState.PlayerSelectAction:
                    HandlePlayerInput(currentKeyboardState);
                    break;

                case CombatState.PlayerAction:
                    Console.WriteLine("Acción del jugador terminada.");
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
                    Console.WriteLine("Turno del enemigo. ¡Atacando!");
                    // Aquí podrías cambiar la animación a "attack" si tuvieras una de "idle"
                    // Por ahora, la animación "enemy-attack" se reproduce en bucle
                    _currentState = CombatState.EnemyAction;
                    break;

                case CombatState.EnemyAction:
                    _player.CurrentHP -= 10;
                    Console.WriteLine($"El enemigo ataca. HP de Luka: {_player.CurrentHP}");

                    if (_player.CurrentHP <= 0)
                    {
                        _currentState = CombatState.Lost;
                    }
                    else
                    {
                        // Al terminar la acción, podrías volver a "idle"
                        _currentState = CombatState.PlayerSelectAction;
                    }
                    break;

                case CombatState.Won:
                    if (
                        currentKeyboardState.IsKeyDown(Keys.Enter)
                        && !_previousKeyboardState.IsKeyDown(Keys.Enter)
                    )
                    {
                        // ¡Volvemos al mapa y posición guardados!
                        _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    }
                    break;

                case CombatState.Lost:
                    if (
                        currentKeyboardState.IsKeyDown(Keys.Enter)
                        && !_previousKeyboardState.IsKeyDown(Keys.Enter)
                    )
                    {
                        _game.ChangeScreen(new MainMenuScreen(_game));
                    }
                    break;
            }

            _previousKeyboardState = currentKeyboardState;
        }

        private void HandlePlayerInput(KeyboardState kbs)
        {
            // Navegación del menú
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

            // Seleccionar opción
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

            switch (action)
            {
                case "ATAQUE":
                    Console.WriteLine("Luka ataca!");
                    _enemy.CurrentHP -= 25;
                    _currentState = CombatState.PlayerAction;
                    break;
                case "GLITCH":
                    Console.WriteLine("Usando GLITCH... (no implementado)");
                    _currentState = CombatState.PlayerAction;
                    break;
                case "DEFENSA":
                    Console.WriteLine("Luka se defiende... (no implementado)");
                    _currentState = CombatState.PlayerAction;
                    break;
                case "OBJETOS":
                    Console.WriteLine("Abriendo inventario... (no implementado)");
                    break;
                case "ESCAPAR":
                    Console.WriteLine("Intentando escapar...");
                    // ¡Volvemos al mapa y posición guardados!
                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // El fondo se limpia en Game1.cs

            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            const float uiScale = 0.8f;

            // 1. Dibujar al enemigo (animado y escalado)
            // Esta llamada usa la escala guardada en el objeto _enemy.AnimatedSprite
            _enemy.AnimatedSprite.Draw(SpriteBatch, _enemy.Position);

            // 2. Dibujar las cajas de la UI
            DrawNineSlicePanel(SpriteBatch, _uiBoxMain);
            DrawNineSlicePanel(SpriteBatch, _uiBoxLeft);

            // 3. Dibujar el menú (Caja Izquierda)
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                Color color = (i == _selectedOption) ? _menuSelectedColor : _menuNormalColor;
                string optionText =
                    (i == _selectedOption) ? $"[ {_menuOptions[i]} ]" : $"[ {_menuOptions[i]} ]";

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

            // 4. Dibujar Stats (Caja Derecha)
            float padding = 30f;
            float statsAreaX = _uiBoxLeft.Right + padding;
            float rightAlignX = _uiBoxMain.Right - padding;
            float currentY = _uiBoxLeft.Top + 20;

            // --- Primera Fila: Nombre (Izquierda) y Balas (Derecha) ---
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

            // --- Siguientes Filas: Barras de Stats ---
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

            // HP
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

            // Cordura
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

            // 5. Dibujar mensajes de estado (Victoria/Derrota)
            if (_currentState == CombatState.Won)
            {
                DrawCenterText("COMBATE GANADO\n[Presiona ENTER]", 1.0f); // Texto grande
            }
            else if (_currentState == CombatState.Lost)
            {
                DrawCenterText("HAS CAÍDO...\n[Presiona ENTER]", 1.0f); // Texto grande
            }

            SpriteBatch.End();
        }

        // --- Métodos de Ayuda para Dibujar ---

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
            // 1. Dibujar Etiqueta
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

            // 2. Dibujar Texto (Valor)
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

            // 3. Dibujar la Barra
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

            // 1. Dibujar Esquinas
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

            // 2. Dibujar Bordes (estirados)
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                _uiTopCenter.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Bottom - cornerSize,
                    destination.Width - (cornerSize * 2),
                    cornerSize
                ),
                _uiBottomCenter.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                _uiMiddleLeft.Region.SourceRectangle,
                Color.White
            );
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.Right - cornerSize,
                    destination.Y + cornerSize,
                    cornerSize,
                    destination.Height - (cornerSize * 2)
                ),
                _uiMiddleRight.Region.SourceRectangle,
                Color.White
            );

            // 3. Dibujar Centro (estirado en ambas direcciones)
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    destination.X + cornerSize,
                    destination.Y + cornerSize,
                    destination.Width - (cornerSize * 2),
                    destination.Height - (cornerSize * 2)
                ),
                _uiMiddleCenter.Region.SourceRectangle,
                Color.White
            );
        }
    }
}
