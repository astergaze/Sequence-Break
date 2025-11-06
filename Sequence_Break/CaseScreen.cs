// CaseScreen.cs
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
        // necesaria para que TiledMapRenderer funcione
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

        // Camara
        private Matrix _cameraTransform;

        public CaseScreen(Game1 game)
            : base(game) { }

        public override void LoadContent()
        {
            // 1. cargar sprites de specter
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

            // 2. Inicializar Listas
            _collisionBarriers = new List<Rectangle>();
            _interactableObjects = new List<InteractableObject>();

            // 3. Cargar el Mapa Inicial
            LoadMap("Lobby");

            // 4. Posición de Spawn
            _specterPosition = new Vector2(600, 750);

            _previousKeyboardState = Keyboard.GetState();
        }

        private void LoadMap(string mapName)
        {
            // 1. Ruta al .tmx (desde el .exe)
            string mapFileSystemPath = Path.Combine(
                AppContext.BaseDirectory,
                $"Content/maps/demo/{mapName}.tmx"
            );

            // 2. Ruta a las texturas (para Content.Load)
            string tilesetContentFolder = "maps/demo/textures";

            // 3. Creamos el renderizador.
            _mapRenderer = new TiledMapRenderer(Content, mapFileSystemPath, tilesetContentFolder);

            // 4. Leemos colisiones e interacciones
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
            // Tepearse
            // revisamos si es una puerta (si tiene TargetMap)
            if (interactable.TargetMap != null && interactable.TargetSpawn != null)
            {
                Console.WriteLine(
                    $"Interactuaste con: {interactable.Name}. Cargando mapa: {interactable.TargetMap}..."
                );

                // 1. Cargamos el mapa definido en Tiled
                LoadMap(interactable.TargetMap);

                // 2. Buscamos el punto de spawn definido en Tiled
                _specterPosition = _mapRenderer.GetSpawnPoint(interactable.TargetSpawn);

                // 3. Centramos la cámara en el nuevo spawn
                _cameraTransform = Matrix.CreateTranslation(
                    -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                    -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                    0
                );

                // Salimos de la función, ya que la interacción principal fue el teletransporte.
                return;
            }

            // Objetos comunes
            // Si no era una puerta, entonces es un objeto normal.
            switch (interactable.Name)
            {
                // Un trigger de lore
                case "lore":
                    Console.WriteLine("");
                    break;

                // Un trigger para volver al hub, dudo usarlo
                case "":
                    Console.WriteLine("");
                    _game.IsMouseVisible = true;
                    _game.ChangeScreen(new GameplayScreen(_game));
                    break;

                default:
                    Console.WriteLine(
                        $"Interactuaste con: {interactable.Name} (sin accion definida)"
                    );
                    break;
            }
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

            // Movimiento y Colisión
            _isMoving = false;
            Vector2 movement = Vector2.Zero;

            if (currentKeyboardState.IsKeyDown(Keys.W) || currentKeyboardState.IsKeyDown(Keys.Up))
                movement.Y = -1;
            if (currentKeyboardState.IsKeyDown(Keys.S) || currentKeyboardState.IsKeyDown(Keys.Down))
                movement.Y = 1;
            if (currentKeyboardState.IsKeyDown(Keys.A) || currentKeyboardState.IsKeyDown(Keys.Left))
                movement.X = -1;
            if (
                currentKeyboardState.IsKeyDown(Keys.D) || currentKeyboardState.IsKeyDown(Keys.Right)
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
            if (currentKeyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CheckForInteraction();
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
            // Centramos la cámara en el jugador
            _cameraTransform = Matrix.CreateTranslation(
                -_specterPosition.X + (GraphicsDevice.Viewport.Width / 2),
                -_specterPosition.Y + (GraphicsDevice.Viewport.Height / 2),
                0
            );

            _previousKeyboardState = currentKeyboardState;
        }

        public override void Draw(GameTime gameTime)
        {
            // // Limpia la pantalla
            // GraphicsDevice.Clear(Color.Black);

            // 1. Dibuja el Mapa
            // Le pasamos la matriz de la cámara
            _mapRenderer.Draw(SpriteBatch, _cameraTransform);

            // 2. Dibuja al Jugador (usando la cámara)
            SpriteBatch.Begin(
                transformMatrix: _cameraTransform, // Aplicamos la misma matriz
                samplerState: SamplerState.PointClamp
            );
            _specterCurrent.Draw(SpriteBatch, _specterPosition);
#if DEBUG
            // Dibuja la hitbox del jugador en verde
            Rectangle playerBox = GetPlayerBox(_specterPosition);
            DrawRectangle(SpriteBatch, playerBox, Color.Green, 2);

            // Dibuja todas las zonas de interacción en rojo
            foreach (var obj in _interactableObjects)
            {
                DrawRectangle(SpriteBatch, obj.TriggerZone, Color.Red, 2);
            }
#endif
            // --- FIN DEL CÓDIGO DE DEPURACIÓN ---
            SpriteBatch.End();
        }

        private void DrawRectangle(
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            int thickness
        )
        {
            Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(
                pixel,
                new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
                color
            );
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(
                pixel,
                new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
                color
            );
        }
    }
}
