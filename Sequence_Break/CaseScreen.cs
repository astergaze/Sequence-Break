using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using TiledSharp;

namespace Sequence_Break
{
    public class CaseScreen : Screen
    {
        // Estructura de Interaccion
        public struct InteractableObject
        {
            public string Name;
            public Rectangle TriggerZone;
            public string TargetMap; // El mapa al que nos teletransporta
            public string TargetSpawn; // El punto de spawn en ese nuevo mapa
        }

        // Variables de Specter
        private AnimatedSprite _specterWalkFront;
        private AnimatedSprite _specterWalkLeft;
        private AnimatedSprite _specterWalkRight;
        private AnimatedSprite _specterWalkBack;
        private AnimatedSprite _specterCurrent;
        private Vector2 _specterPosition;
        private bool _isMoving;

        // Variables de Control
        private const float MOVEMENT_SPEED = 5.0f;
        private KeyboardState _previousKeyboardState;
        private const float PLAYER_SCALE = 3.0f;
        private const int PLAYER_BASE_WIDTH = 22;
        private const int PLAYER_BASE_HEIGHT = 40;
        private const float PLAYER_REFERENCE_WIDTH = PLAYER_BASE_WIDTH * PLAYER_SCALE;
        private const float PLAYER_REFERENCE_HEIGHT = PLAYER_BASE_HEIGHT * PLAYER_SCALE;

        // Variables del Nivel (Tiled)
        private TiledMapRenderer _mapRenderer;
        private List<Rectangle> _collisionBarriers;
        private List<InteractableObject> _interactableObjects;

        // Variables de estado
        private string _currentMapName;
        private string _initialMap;
        private Vector2 _initialSpawnPoint;

        // Textura para Debug (optimizada)
        private Texture2D _pixelTexture;

        // UI
        private InteractionPanel _interactionPanel;
        private SpriteFont _uiFont;
        private TextureAtlas _uiAtlas;
        private string _currentInteractionName = string.Empty; // Para el evento de opciones

        // Camara
        private Matrix _cameraTransform;

        // Constructor por defecto
        public CaseScreen(Game1 game)
            : base(game)
        {
            _initialMap = "Lobby";
            _initialSpawnPoint = new Vector2(600, 750); // spawn por defecto
        }

        // Constructor (usado para VOLVER del combate)
        public CaseScreen(Game1 game, string mapToLoad, Vector2 positionToSpawn)
            : base(game)
        {
            _initialMap = mapToLoad;
            _initialSpawnPoint = positionToSpawn;
        }

        public override void LoadContent()
        {
            // cargar sprites de specter
            TextureAtlas atlasFront = TextureAtlas.FromFile(
                Content,
                "textures/Specter-front-atlas-definition.xml"
            );
            _specterWalkFront = atlasFront.CreateAnimatedSprite("luka-walk-front");
            _specterWalkFront.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasBack = TextureAtlas.FromFile(
                Content,
                "textures/Specter-back-atlas-definition.xml"
            );
            _specterWalkBack = atlasBack.CreateAnimatedSprite("luka-walk-back");
            _specterWalkBack.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasLeft = TextureAtlas.FromFile(
                Content,
                "textures/Specter-left-atlas-definition.xml"
            );
            _specterWalkLeft = atlasLeft.CreateAnimatedSprite("luka-walk-left");
            _specterWalkLeft.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            TextureAtlas atlasRight = TextureAtlas.FromFile(
                Content,
                "textures/Specter-right-atlas-definition.xml"
            );
            _specterWalkRight = atlasRight.CreateAnimatedSprite("luka-walk-right");
            _specterWalkRight.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);

            _specterCurrent = _specterWalkFront;

            // Inicializar Listas
            _collisionBarriers = new List<Rectangle>();
            _interactableObjects = new List<InteractableObject>();

            // Cargar el Mapa Inicial
            LoadMap(_initialMap); // Usa la variable inicial

            // Posición de Spawn
            _specterPosition = _initialSpawnPoint; // Usa la variable inicial

            // Crear la textura de píxel para Debug
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Cargar Assets de UI
            try
            {
                _uiFont = Content.Load<SpriteFont>("fonts/IBMPlexMono");
                _uiAtlas = TextureAtlas.FromFile(
                    Content,
                    "Interface/Combat/interface-combat-atlas-definition.xml"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error cargando assets de UI para InteractionPanel: {ex.Message}"
                );
                throw;
            }

            // Inicializar Panel de Interacción
            _interactionPanel = new InteractionPanel(_uiFont, _uiAtlas, GraphicsDevice);
            _interactionPanel.OnOptionSelected += HandleInteractionChoice; // Suscribirse al evento

            _previousKeyboardState = Keyboard.GetState();
        }

        private void LoadMap(string mapName)
        {
            _currentMapName = mapName;

            string mapFileSystemPath = Path.Combine(
                AppContext.BaseDirectory,
                $"Content/maps/demo/{mapName}.tmx"
            );
            string tilesetContentFolder = "maps/demo/textures";

            _mapRenderer = new TiledMapRenderer(Content, mapFileSystemPath, tilesetContentFolder);

            _collisionBarriers = _mapRenderer.GetCollisionRectangles();
            _interactableObjects = _mapRenderer.GetInteractableObjects();
        }

        private Rectangle GetPlayerBox(Vector2 position)
        {
            float spriteWidth = PLAYER_REFERENCE_WIDTH;
            float spriteHeight = PLAYER_REFERENCE_HEIGHT;
            int boxWidth = (int)(spriteWidth * 0.7f);
            int boxHeight = (int)(spriteHeight * 0.9f);
            int boxX = (int)(position.X + (spriteWidth * 0.15f));
            int boxY = (int)(position.Y + (spriteHeight * 0.1f));
            return new Rectangle(boxX, boxY, boxWidth, boxHeight);
        }

        private bool HasCollision(Rectangle playerBox)
        {
            foreach (Rectangle barrier in _collisionBarriers)
            {
                if (playerBox.Intersects(barrier))
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckForInteraction()
        {
            Rectangle playerBox = GetPlayerBox(_specterPosition);
            foreach (InteractableObject obj in _interactableObjects)
            {
                if (playerBox.Intersects(obj.TriggerZone))
                {
                    PerformInteraction(obj);
                    break;
                }
            }
        }

        // Lógica de Interacción del Nivel
        private void PerformInteraction(InteractableObject interactable)
        {
            // Guardamos el nombre para el event handler
            _currentInteractionName = interactable.Name;

            // Tepearse (Puertas)
            if (interactable.TargetMap != null && interactable.TargetSpawn != null)
            {
                Console.WriteLine(
                    $"Interactuaste con: {interactable.Name}. Cargando mapa: {interactable.TargetMap}..."
                );

                LoadMap(interactable.TargetMap);
                _specterPosition = _mapRenderer.GetSpawnPoint(interactable.TargetSpawn);

                _cameraTransform = Matrix.CreateTranslation(
                    -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                    -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                    0
                );
                return;
            }

            // Objetos comunes
            switch (interactable.Name)
            {
                case "lore_1":
                    _interactionPanel.Show(
                        "Las paredes están cubiertas de anotaciones crípticas. 'La disonancia es la clave', 'No confíes en el reflejo'. El Alquimista estuvo aquí, y no estaba solo.",
                        null, // Sin opciones
                        "Pista"
                    );
                    break;

                case "npc_1":
                    var options = new List<string> { "Preguntar por el Alquimista", "Ignorar" };
                    _interactionPanel.Show(
                        "El hombre te mira con ojos vacíos. '¿Tú también lo buscas? Ten cuidado, lo que yace aquí... no le gusta que lo despierten.'",
                        options,
                        "Hombre Aterrado"
                    );
                    break;

                case "hub_return":
                    _game.IsMouseVisible = true;
                    _game.ChangeScreen(new GameplayScreen(_game));
                    break;

                default:
                    _interactionPanel.Show(
                        $"Interactuaste con: {interactable.Name} (sin accion definida)",
                        null,
                        null
                    );
                    break;
            }
        }

        /// Maneja la respuesta del jugador desde el InteractionPanel.
        private void HandleInteractionChoice(int optionIndex)
        {
            // Usamos la variable _currentInteractionName para saber a qué respondía el jugador
            switch (_currentInteractionName)
            {
                case "npc_1":
                    if (optionIndex == 0) // "Preguntar por el Alquimista"
                    {
                        // Podríamos mostrar otro diálogo encadenado
                        _interactionPanel.Show(
                            "'Se fue por ese pasillo... dijo algo sobre... 'romper la secuencia'. No sé qué signifique, y no quiero saberlo.'",
                            null,
                            "Hombre Aterrado"
                        );
                    }
                    else // "Ignorar"
                    {
                        // No hacer nada, el panel ya se cerró
                    }
                    break;

                // Añadir más casos aca para otras interacciones con opciones
            }

            // Limpiamos la interacción actual
            _currentInteractionName = string.Empty;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                _game.Exit(); // TO DO: Pausa

            if (
                currentKeyboardState.IsKeyDown(Keys.F11)
                && !_previousKeyboardState.IsKeyDown(Keys.F11)
            )
            {
                Core.Graphics.ToggleFullScreen();
                Core.Graphics.ApplyChanges();
            }

            // Si el panel de interacción está activo, él toma el control.
            // Toda la lógica de movimiento y juego se salta.
            if (_interactionPanel.IsActive)
            {
                _interactionPanel.Update(gameTime);
            }
            else
            {
                // El panel NO está activo, el jugador puede moverse e interactuar.

                // Movimiento y Colisión
                _isMoving = false;
                Vector2 movement = Vector2.Zero;

                if (
                    currentKeyboardState.IsKeyDown(Keys.W)
                    || currentKeyboardState.IsKeyDown(Keys.Up)
                )
                    movement.Y = -1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.S)
                    || currentKeyboardState.IsKeyDown(Keys.Down)
                )
                    movement.Y = 1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.A)
                    || currentKeyboardState.IsKeyDown(Keys.Left)
                )
                    movement.X = -1;
                if (
                    currentKeyboardState.IsKeyDown(Keys.D)
                    || currentKeyboardState.IsKeyDown(Keys.Right)
                )
                    movement.X = 1;

                if (movement != Vector2.Zero)
                {
                    _isMoving = true;
                    if (movement.X < 0)
                        _specterCurrent = _specterWalkLeft;
                    else if (movement.X > 0)
                        _specterCurrent = _specterWalkRight;
                    else if (movement.Y < 0)
                        _specterCurrent = _specterWalkBack;
                    else if (movement.Y > 0)
                        _specterCurrent = _specterWalkFront;
                }

                if (movement != Vector2.Zero)
                    movement.Normalize();

                movement *= MOVEMENT_SPEED;

                Vector2 newPosition = _specterPosition;
                newPosition.X += movement.X;
                Rectangle playerBoxX = GetPlayerBox(newPosition);
                if (HasCollision(playerBoxX))
                {
                    newPosition.X = _specterPosition.X;
                }

                newPosition.Y += movement.Y;
                Rectangle playerBoxY = GetPlayerBox(newPosition);
                if (HasCollision(playerBoxY))
                {
                    newPosition.Y = _specterPosition.Y;
                }

                _specterPosition = newPosition;

                // Interacción
                if (
                    currentKeyboardState.IsKeyDown(Keys.E)
                    && !_previousKeyboardState.IsKeyDown(Keys.E)
                )
                {
                    CheckForInteraction();
                }

                // Test de Combate
                if (
                    currentKeyboardState.IsKeyDown(Keys.C)
                    && !_previousKeyboardState.IsKeyDown(Keys.C)
                )
                {
                    Console.WriteLine("Iniciando combate de prueba...");
                    _game.IsMouseVisible = false;
                    _game.ChangeScreen(new CombatScreen(_game, _currentMapName, _specterPosition));
                    return;
                }

                // Actualización de Animación
                if (_isMoving)
                {
                    _specterCurrent.Update(gameTime);
                }
                else
                {
                    _specterCurrent.CurrentFrame = 0;
                }

                // Actualización de Cámara
                _cameraTransform = Matrix.CreateTranslation(
                    -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                    -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                    0
                );
            }

            _previousKeyboardState = currentKeyboardState;
        }

        public override void Draw(GameTime gameTime)
        {
            // Dibuja el Mapa (con cámara)
            _mapRenderer.Draw(SpriteBatch, _cameraTransform);

            // Dibuja al Jugador (con cámara)
            SpriteBatch.Begin(
                transformMatrix: _cameraTransform,
                samplerState: SamplerState.PointClamp
            );
            _specterCurrent.Draw(SpriteBatch, _specterPosition);
#if DEBUG
            Rectangle playerBox = GetPlayerBox(_specterPosition);
            DrawRectangle(SpriteBatch, playerBox, Color.Green, 2);
            foreach (var obj in _interactableObjects)
            {
                DrawRectangle(SpriteBatch, obj.TriggerZone, Color.Red, 2);
            }
#endif
            SpriteBatch.End();

            // Dibuja la UI (estática, sin cámara)
            // Se dibuja en un lote separado para que esté encima de todo.
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _interactionPanel.Draw(gameTime, SpriteBatch);
            SpriteBatch.End();
        }

        // Método de dibujo optimizado (usa _pixelTexture)
        private void DrawRectangle(
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            int thickness
        )
        {
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(rect.X, rect.Y, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(rect.X, rect.Y, thickness, rect.Height),
                color
            );
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
                color
            );
        }
    }
}
