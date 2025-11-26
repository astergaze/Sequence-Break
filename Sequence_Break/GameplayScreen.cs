using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class GameplayScreen : Screen
    {
        // Variables de Specter
        private AnimatedSprite _specterWalkFront;
        private AnimatedSprite _specterWalkLeft;
        private AnimatedSprite _specterWalkRight;
        private AnimatedSprite _specterWalkBack;

        private AnimatedSprite _specterCurrent;
        private Vector2 _specterPosition;
        private bool _isMoving;

        // Variables del Mapa
        private Texture2D _roomTexture;
        private const int MAP_SCALE_FACTOR = 5;
        private Vector2 _mapPosition;

        // Variables de Control
        private const float MOVEMENT_SPEED = 5.0f;
        private KeyboardState _previousKeyboardState;

        // Constantes de Tamaño y Colision
        private const float PLAYER_SCALE = 3.0f;
        private const int PLAYER_BASE_WIDTH = 22;
        private const int PLAYER_BASE_HEIGHT = 40;

        private const float PLAYER_REFERENCE_WIDTH = PLAYER_BASE_WIDTH * PLAYER_SCALE;
        private const float PLAYER_REFERENCE_HEIGHT = PLAYER_BASE_HEIGHT * PLAYER_SCALE;

        // Lista de barreras de colision
        private List<Rectangle> _collisionBarriers;

        // Objeto interactuable
        private struct InteractableObject
        {
            public string Name;
            public Rectangle TriggerZone;
        }

        // Lista de objetos interactuables
        private List<InteractableObject> _interactableObjects;

        // --- Variables de UI ---
        private InteractionPanel _interactionPanel;
        private SpriteFont _uiFont;
        private TextureAtlas _uiAtlas;
        private string _currentInteraction = string.Empty;

        // --- MENUS ---
        private PauseMenu _pauseMenu;
        private InventoryMenu _inventoryMenu; // Nuevo Inventario

        public GameplayScreen(Game1 game)
            : base(game) { }

        public override void LoadContent()
        {
            // Cargar texturas de Specter
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

            // Cargar el mapa
            _roomTexture = Content.Load<Texture2D>("textures/Specter_room");

            _specterCurrent = _specterWalkFront;

            // Posicion del mapa
            int scaledMapWidth = _roomTexture.Width * MAP_SCALE_FACTOR;
            int scaledMapHeight = _roomTexture.Height * MAP_SCALE_FACTOR;

            _mapPosition = new Vector2(
                (GraphicsDevice.Viewport.Width - scaledMapWidth) / 2,
                (GraphicsDevice.Viewport.Height - scaledMapHeight) / 2
            );

            _specterPosition =
                _mapPosition + new Vector2(scaledMapWidth / 2f, scaledMapHeight / 2f);

            _collisionBarriers = new List<Rectangle>();
            PopulateCollisionBarriers();

            _interactableObjects = new List<InteractableObject>();
            PopulateInteractableObjects();

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
                Console.WriteLine($"Error cargando assets de UI: {ex.Message}");
                throw;
            }

            // Inicializar Panel de Interaccion
            _interactionPanel = new InteractionPanel(_uiFont, _uiAtlas, GraphicsDevice);
            _interactionPanel.OnOptionSelected += HandleInteractionChoice;

            // Inicializar Menus
            _pauseMenu = new PauseMenu(_game, this, _uiFont, GraphicsDevice);
            _inventoryMenu = new InventoryMenu(_game, _uiFont, GraphicsDevice);

            _previousKeyboardState = Keyboard.GetState();
        }

        private void PopulateCollisionBarriers()
        {
            int scale = MAP_SCALE_FACTOR;
            int mapX = (int)_mapPosition.X;
            int mapY = (int)_mapPosition.Y;

            // Bordes y Muros
            _collisionBarriers.Add(new Rectangle(mapX, mapY, 128 * scale, 4 * scale)); // Arriba
            _collisionBarriers.Add(
                new Rectangle(mapX, mapY + (125 * scale), 128 * scale, 3 * scale)
            ); // Abajo
            _collisionBarriers.Add(new Rectangle(mapX, mapY, 4 * scale, 128 * scale)); // Izquierda
            _collisionBarriers.Add(
                new Rectangle(mapX + (125 * scale), mapY, 3 * scale, 127 * scale)
            ); // Derecha

            // Objetos
            _collisionBarriers.Add(
                new Rectangle(mapX + (3 * scale), mapY + (78 * scale), 23 * scale, 47 * scale)
            ); // Cama
            _collisionBarriers.Add(
                new Rectangle(mapX + (41 * scale), mapY + (4 * scale), 13 * scale, 35 * scale)
            ); // Esc. Izq
            _collisionBarriers.Add(
                new Rectangle(mapX + (55 * scale), mapY + (5 * scale), 22 * scale, 22 * scale)
            ); // Esc. Centro
            _collisionBarriers.Add(
                new Rectangle(mapX + (77 * scale), mapY + (5 * scale), 13 * scale, 34 * scale)
            ); // Esc. Der
            _collisionBarriers.Add(
                new Rectangle(mapX + (98 * scale), mapY + (100 * scale), 27 * scale, 25 * scale)
            ); // Puff
            _collisionBarriers.Add(
                new Rectangle(mapX + (96 * scale), mapY + (5 * scale), 29 * scale, 28 * scale)
            ); // Armas
            _collisionBarriers.Add(
                new Rectangle(mapX + (3 * scale), mapY + (4 * scale), 35 * scale, 15 * scale)
            ); // Bateria
            _collisionBarriers.Add(
                new Rectangle(mapX + (91 * scale), mapY + (68 * scale), 32 * scale, 6 * scale)
            ); // TV
        }

        private void PopulateInteractableObjects()
        {
            int scale = MAP_SCALE_FACTOR;
            int mapX = (int)_mapPosition.X;
            int mapY = (int)_mapPosition.Y;

            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Cama",
                    TriggerZone = new Rectangle(
                        mapX + (3 * scale),
                        mapY + (70 * scale),
                        30 * scale,
                        60 * scale
                    ),
                }
            );
            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Escritorio",
                    TriggerZone = new Rectangle(
                        mapX + (55 * scale),
                        mapY + (5 * scale),
                        22 * scale,
                        30 * scale
                    ),
                }
            );
            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Bateria",
                    TriggerZone = new Rectangle(
                        mapX + (3 * scale),
                        mapY + (4 * scale),
                        35 * scale,
                        20 * scale
                    ),
                }
            );
            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Puff",
                    TriggerZone = new Rectangle(
                        mapX + (95 * scale),
                        mapY + (90 * scale),
                        40 * scale,
                        40 * scale
                    ),
                }
            );
            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Armas_Medicinas",
                    TriggerZone = new Rectangle(
                        mapX + (96 * scale),
                        mapY + (5 * scale),
                        32 * scale,
                        32 * scale
                    ),
                }
            );
            _interactableObjects.Add(
                new InteractableObject
                {
                    Name = "Televisor",
                    TriggerZone = new Rectangle(
                        mapX + (88 * scale),
                        mapY + (65 * scale),
                        39 * scale,
                        10 * scale
                    ),
                }
            );
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
                    return true;
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // 1. Actualizar Menú de Pausa (Prioridad Máxima)
            _pauseMenu.Update(gameTime);
            if (_pauseMenu.IsActive)
                return; // Bloquea todo

            // 2. Pausar con Escape
            if (
                currentKeyboardState.IsKeyDown(Keys.Escape)
                && !_previousKeyboardState.IsKeyDown(Keys.Escape)
            )
            {
                // Si el inventario está abierto, Escape lo cierra (ya manejado dentro de InventoryMenu.Update)
                // Si NO está abierto, abrimos pausa.
                if (!_inventoryMenu.IsActive)
                {
                    _pauseMenu.Show();
                }
            }

            // 3. Toggle Pantalla Completa
            if (
                currentKeyboardState.IsKeyDown(Keys.F11)
                && !_previousKeyboardState.IsKeyDown(Keys.F11)
            )
            {
                Core.Graphics.ToggleFullScreen();
                Core.Graphics.ApplyChanges();
            }

            // 4. Actualizar Inventario (Prioridad 2)
            if (
                (
                    currentKeyboardState.IsKeyDown(Keys.Tab)
                    && !_previousKeyboardState.IsKeyDown(Keys.Tab)
                )
                || (
                    currentKeyboardState.IsKeyDown(Keys.I)
                    && !_previousKeyboardState.IsKeyDown(Keys.I)
                )
            )
            {
                // Solo abre si no hay diálogo activo
                if (!_interactionPanel.IsActive)
                {
                    _inventoryMenu.Toggle();
                }
            }

            if (_inventoryMenu.IsActive)
            {
                _inventoryMenu.Update(gameTime);
                _previousKeyboardState = currentKeyboardState;
                return; // Bloquea juego y diálogo
            }

            // 5. Actualizar Diálogos (Prioridad 3)
            if (_interactionPanel.IsActive)
            {
                _interactionPanel.Update(gameTime);
            }
            else
            {
                // 6. Lógica del Juego (Movimiento e Interacción)
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

                // Animación
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

                    movement.Normalize();
                    movement *= MOVEMENT_SPEED;
                }

                // Colisiones
                Vector2 newPosition = _specterPosition;

                newPosition.X += movement.X;
                if (HasCollision(GetPlayerBox(newPosition)))
                    newPosition.X = _specterPosition.X;

                newPosition.Y += movement.Y;
                if (HasCollision(GetPlayerBox(newPosition)))
                    newPosition.Y = _specterPosition.Y;

                _specterPosition = newPosition;

                // Interacción (E)
                if (
                    currentKeyboardState.IsKeyDown(Keys.E)
                    && !_previousKeyboardState.IsKeyDown(Keys.E)
                )
                {
                    CheckForInteraction();
                }

                // Update Animación Sprite
                if (_isMoving)
                    _specterCurrent.Update(gameTime);
                else
                    _specterCurrent.CurrentFrame = 0;
            }

            _previousKeyboardState = currentKeyboardState;
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Fondo
            SpriteBatch.Draw(
                _roomTexture,
                _mapPosition,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                MAP_SCALE_FACTOR,
                SpriteEffects.None,
                0f
            );

            // Jugador
            _specterCurrent.Draw(SpriteBatch, _specterPosition);

            SpriteBatch.End();

            // --- CAPA DE UI ---
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Diálogos
            _interactionPanel.Draw(gameTime, SpriteBatch);

            // Inventario
            _inventoryMenu.Draw(SpriteBatch);

            // Pausa (Encima de todo)
            _pauseMenu.Draw(SpriteBatch);

            SpriteBatch.End();
        }

        private void CheckForInteraction()
        {
            Rectangle playerBox = GetPlayerBox(_specterPosition);
            foreach (InteractableObject obj in _interactableObjects)
            {
                if (playerBox.Intersects(obj.TriggerZone))
                {
                    PerformInteraction(obj.Name);
                    break;
                }
            }
        }

        private void PerformInteraction(string objectName)
        {
            _currentInteraction = objectName;

            switch (objectName)
            {
                case "Cama":
                    var camaOptions = new List<string> { "Guardar y descansar", "Ahora no" };
                    _interactionPanel.Show(
                        "No es momento de dormir. Hay un caso que resolver... pero podria descansar un momento.",
                        camaOptions,
                        "Pensamiento"
                    );
                    break;

                case "Escritorio":
                    var escritorioOptions = new List<string> { "Investigar papeles", "Dejarlo" };
                    _interactionPanel.Show(
                        "Un monton de papeles... el rastro del Alquimista. Investigar?",
                        escritorioOptions,
                        null
                    );
                    break;

                case "Bateria":
                    _interactionPanel.Show(
                        "Una bateria de coche. Mantiene las luces encendidas.",
                        null,
                        null
                    );
                    break;

                case "Puff":
                    _interactionPanel.Show("Comodo, pero esta cubierto de polvo.", null, null);
                    break;

                case "Armas_Medicinas":
                    var armasOptions = new List<string> { "Abrir inventario", "Dejarlo" };
                    _interactionPanel.Show("Mis suministros y equipo.", armasOptions, null);
                    break;

                case "Televisor":
                    _interactionPanel.Show(
                        "Mi television, aunque apagada ahora mismo.",
                        null,
                        null
                    );
                    break;

                default:
                    _currentInteraction = string.Empty;
                    break;
            }
        }

        private void HandleInteractionChoice(int optionIndex)
        {
            switch (_currentInteraction)
            {
                case "Cama":
                    if (optionIndex == 0) // Guardar
                    {
                        // Lógica de Guardado
                        PlayerStatus.CurrentHP = PlayerStatus.MaxHP;
                        PlayerStatus.CurrentSanity = PlayerStatus.MaxSanity;
                        SaveManager.SaveGame();

                        _interactionPanel.Show(
                            "Progreso guardado y salud restaurada.",
                            null,
                            "Sistema"
                        );
                    }
                    break;

                case "Escritorio":
                    if (optionIndex == 0)
                        _game.ChangeScreen(new CaseScreen(_game));
                    break;

                case "Armas_Medicinas":
                    if (optionIndex == 0)
                        _inventoryMenu.Show();
                    break;
            }

            _currentInteraction = string.Empty;
        }
    }
}
