using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics; // <--- 1. AÑADIDO ESTE USING

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
            public Texture2D Sprite { get; set; }
            public Vector2 Position { get; set; }
        }

        public class Player : Combatant
        {
            public int CurrentCordura { get; set; }
            public int MaxCordura { get; set; }
            public int Balas { get; set; }
            public int MaxBalas { get; set; }
        }

        public class Enemy : Combatant { }
        
        // --- Variables de la pantalla de combate ---

        // Fuentes y Texturas
        private SpriteFont _uiFont;
        private Texture2D _pixel; // Textura 1x1 para dibujar barras
        private Texture2D _enemySprite; // Sprite del enemigo

        // Combatientes
        private Player _player;
        private Enemy _enemy;

        // Máquina de estados del combate
        private enum CombatState
        {
            Start, PlayerSelectAction, PlayerAction, EnemyTurn, EnemyAction, Won, Lost,
        }
        private CombatState _currentState;
        
        // Lógica del Menú de Combate
        private string[] _menuOptions = { "ATAQUE", "GLITCH", "DEFENSA", "OBJETOS", "ESCAPAR" };
        private int _selectedOption = 0;

        // Lógica del Atlas de la UI
        private TextureAtlas _uiAtlas;
        private Sprite _uiTopLeft, _uiTopCenter, _uiTopRight;
        private Sprite _uiMiddleLeft, _uiMiddleCenter, _uiMiddleRight;
        private Sprite _uiBottomLeft, _uiBottomCenter, _uiBottomRight;
        
        // Posiciones y Colores de la UI
        private Rectangle _uiBoxMain;
        private Rectangle _uiBoxLeft;
        private Vector2 _menuStartPosition;
        private Color _menuNormalColor = Color.White;
        private Color _menuSelectedColor = new Color(170, 0, 255); // Morado brillante
        private Color _hpColor = new Color(170, 0, 255);
        private Color _corduraColor = new Color(0, 150, 255);
        private Color _barBackgroundColor = new Color(40, 40, 40);

        private KeyboardState _previousKeyboardState;

        // --- INICIO DE MODIFICACIONES ---

        // Variables para guardar el estado de retorno
        private string _returnMapName;
        private Vector2 _returnPosition;

        // Constructor MODIFICADO
        public CombatScreen(Game1 game, string returnMap, Vector2 returnPos) : base(game) 
        {
            _returnMapName = returnMap;
            _returnPosition = returnPos;
        }

        // --- FIN DE MODIFICACIONES ---

        public override void LoadContent()
        {
            // Cargar fuentes
            _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");

            // Cargar sprite enemigo (con placeholder)
            try 
            {
                 _enemySprite = Content.Load<Texture2D>("textures/enemy_placeholder");
            }
            catch (Exception)
            {
                _enemySprite = new Texture2D(GraphicsDevice, 64, 64);
                Color[] data = new Color[64*64];
                for(int i=0; i < data.Length; ++i) data[i] = Color.Red;
                _enemySprite.SetData(data);
            }

            // Crear el píxel blanco para la UI
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Cargar el Atlas de la UI
            // ¡¡ASEGÚRATE DE QUE ESTA RUTA ES CORRECTA!!
            _uiAtlas = TextureAtlas.FromFile(Content, "Interface/Combat/interface-combat-atlas-definition.xml"); 

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
                (int)(screenWidth * 0.05f),
                screenHeight - uiHeight - (int)(screenHeight * 0.02f),
                (int)(screenWidth * 0.9f),
                uiHeight
            );
            
            _uiBoxLeft = new Rectangle(
                _uiBoxMain.X,
                _uiBoxMain.Y,
                (int)(_uiBoxMain.Width * 0.25f),
                _uiBoxMain.Height
            );
            
            _menuStartPosition = new Vector2(_uiBoxLeft.X + 20, _uiBoxLeft.Y + 20);

            // Inicializar combatientes
            _player = new Player
            {
                Name = "Luka Specter",
                CurrentHP = 100, MaxHP = 100,
                CurrentCordura = 100, MaxCordura = 100,
                Balas = 12, MaxBalas = 12
            };

            _enemy = new Enemy
            {
                Name = "Disonancia",
                CurrentHP = 80, MaxHP = 80,
                Sprite = _enemySprite,
                Position = new Vector2(screenWidth / 2 - _enemySprite.Width / 2, screenHeight / 2 - 150)
            };

            // Empezar el combate
            _currentState = CombatState.Start;
            _previousKeyboardState = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

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
                        _currentState = CombatState.PlayerSelectAction;
                    }
                    break;

                // --- INICIO DE MODIFICACIÓN ---
                case CombatState.Won:
                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                    {
                        // ¡Volvemos al mapa y posición guardados!
                        _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    }
                    break;
                // --- FIN DE MODIFICACIÓN ---

                case CombatState.Lost:
                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
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
            if (kbs.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W) ||
                kbs.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            {
                _selectedOption--;
                if (_selectedOption < 0) _selectedOption = _menuOptions.Length - 1;
            }
            
            if (kbs.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S) ||
                kbs.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            {
                _selectedOption++;
                if (_selectedOption >= _menuOptions.Length) _selectedOption = 0;
            }

            // Seleccionar opción
            if (kbs.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter) ||
                kbs.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
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

                // --- INICIO DE MODIFICACIÓN ---
                case "ESCAPAR":
                    Console.WriteLine("Intentando escapar...");
                    // ¡Volvemos al mapa y posición guardados!
                    _game.ChangeScreen(new CaseScreen(_game, _returnMapName, _returnPosition));
                    break;
                // --- FIN DE MODIFICACIÓN ---
            }
        }
        
        public override void Draw(GameTime gameTime)
        {
            // El fondo se limpia en Game1.cs
            
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // 1. Dibujar al enemigo
            SpriteBatch.Draw(_enemy.Sprite, _enemy.Position, Color.White);

            // 2. Dibujar las cajas de la UI
            DrawNineSlicePanel(SpriteBatch, _uiBoxMain);
            DrawNineSlicePanel(SpriteBatch, _uiBoxLeft);
            
            // 3. Dibujar el menú (Caja Izquierda)
            for (int i = 0; i < _menuOptions.Length; i++)
            {
                Color color = (i == _selectedOption) ? _menuSelectedColor : _menuNormalColor;
                string optionText = (i == _selectedOption) ? $"[ {_menuOptions[i]} ]" : $"[ {_menuOptions[i]} ]";
                
                Vector2 position = new Vector2(
                    _menuStartPosition.X,
                    _menuStartPosition.Y + (i * 30)
                );
                float scale = 0.8f;
                SpriteBatch.DrawString(_uiFont, optionText, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            // 4. Dibujar Stats (Caja Derecha)
            Vector2 statsPosition = new Vector2(_uiBoxLeft.Right + 30, _uiBoxLeft.Top + 20);

            // Nombre
            SpriteBatch.DrawString(_uiFont, _player.Name, statsPosition, Color.White);
            
            // HP
            statsPosition.Y += 35;
            DrawStatBar("HP", _player.CurrentHP, _player.MaxHP, statsPosition, 200, _hpColor);

            // Cordura
            statsPosition.Y += 35;
            DrawStatBar("Cordura", _player.CurrentCordura, _player.MaxCordura, statsPosition, 200, _corduraColor);

            // Balas
            Vector2 balasPosition = new Vector2(statsPosition.X + 250, _uiBoxLeft.Top + 20);
            SpriteBatch.DrawString(_uiFont, $"Balas: {_player.Balas}/{_player.MaxBalas}", balasPosition, Color.Yellow);

            // 5. Dibujar mensajes de estado (Victoria/Derrota)
            if (_currentState == CombatState.Won)
            {
                DrawCenterText("COMBATE GANADO\n[Presiona ENTER]");
            }
            else if (_currentState == CombatState.Lost)
            {
                DrawCenterText("HAS CAÍDO...\n[Presiona ENTER]");
            }
            
            SpriteBatch.End();
        }

        // --- Métodos de Ayuda para Dibujar ---

        private void DrawStatBar(string label, int current, int max, Vector2 position, int barWidth, Color barColor)
        {
            // Etiqueta (HP, Cordura)
            SpriteBatch.DrawString(_uiFont, label, position, Color.White);

            // Posición y tamaño de la barra
            Vector2 barPosition = position + new Vector2(_uiFont.MeasureString(label).X + 10, 0);
            int barHeight = 20;
            float percent = (float)current / max;

            // Barra de fondo
            Rectangle bgRect = new Rectangle((int)barPosition.X, (int)barPosition.Y + 2, barWidth, barHeight);
            SpriteBatch.Draw(_pixel, bgRect, _barBackgroundColor);
            
            // Barra de vida/cordura
            Rectangle fgRect = new Rectangle((int)barPosition.X, (int)barPosition.Y + 2, (int)(barWidth * percent), barHeight);
            SpriteBatch.Draw(_pixel, fgRect, barColor);

            // Texto (100 / 100)
            string statText = $"{current}/{max}";
            Vector2 textPos = barPosition + new Vector2(barWidth + 10, 0);
            SpriteBatch.DrawString(_uiFont, statText, textPos, Color.White);
        }

        private void DrawCenterText(string text)
        {
            Vector2 textSize = _uiFont.MeasureString(text);
            Vector2 position = new Vector2(
                (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                (GraphicsDevice.Viewport.Height - textSize.Y) / 2
            );
            
            // Sombra
            SpriteBatch.DrawString(_uiFont, text, position + new Vector2(2, 2), Color.Black);
            // Texto
            SpriteBatch.DrawString(_uiFont, text, position, Color.White);
        }
        
        private void DrawNineSlicePanel(SpriteBatch spriteBatch, Rectangle destination)
        {
            // De tu XML, sabemos que los sprites fuente son 64x64
            const int sourceSpriteSize = 64; 
            
            // --- ¡AJUSTA ESTA ESCALA! ---
            const float scale = 1.0f; 
            
            int cornerSize = (int)(sourceSpriteSize * scale);

            // La textura (spritesheet) es la misma para todas las piezas
            Texture2D texture = _uiTopLeft.Region.Texture;

            // 1. Dibujar Esquinas (sin escalar, solo posicionar)
            // Top-Left
            spriteBatch.Draw(texture, 
                new Rectangle(destination.X, destination.Y, cornerSize, cornerSize), 
                _uiTopLeft.Region.SourceRectangle, Color.White);
            
            // Top-Right
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - cornerSize, destination.Y, cornerSize, cornerSize),
                _uiTopRight.Region.SourceRectangle, Color.White);
            
            // Bottom-Left
            spriteBatch.Draw(texture,
                new Rectangle(destination.X, destination.Bottom - cornerSize, cornerSize, cornerSize),
                _uiBottomLeft.Region.SourceRectangle, Color.White);
            
            // Bottom-Right
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - cornerSize, destination.Bottom - cornerSize, cornerSize, cornerSize),
                _uiBottomRight.Region.SourceRectangle, Color.White);

            // 2. Dibujar Bordes (estirados)
            // Top-Center
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + cornerSize, destination.Y, destination.Width - (cornerSize * 2), cornerSize),
                _uiTopCenter.Region.SourceRectangle, Color.White);
            
            // Bottom-Center
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + cornerSize, destination.Bottom - cornerSize, destination.Width - (cornerSize * 2), cornerSize),
                _uiBottomCenter.Region.SourceRectangle, Color.White);

            // Middle-Left
            spriteBatch.Draw(texture,
                new Rectangle(destination.X, destination.Y + cornerSize, cornerSize, destination.Height - (cornerSize * 2)),
                _uiMiddleLeft.Region.SourceRectangle, Color.White);
            
            // Middle-Right
            spriteBatch.Draw(texture,
                new Rectangle(destination.Right - cornerSize, destination.Y + cornerSize, cornerSize, destination.Height - (cornerSize * 2)),
                _uiMiddleRight.Region.SourceRectangle, Color.White);

            // 3. Dibujar Centro (estirado en ambas direcciones)
            spriteBatch.Draw(texture,
                new Rectangle(destination.X + cornerSize, destination.Y + cornerSize, destination.Width - (cornerSize * 2), destination.Height - (cornerSize * 2)),
                _uiMiddleCenter.Region.SourceRectangle, Color.White);
        }
    }
}