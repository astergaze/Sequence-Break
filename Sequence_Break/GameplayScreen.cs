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
        private KeyboardState _previousKeyboardState; // Para F11

        // Constantes de Tamaño y Colision
        private const float PLAYER_SCALE = 3.0f;
        private const int PLAYER_BASE_WIDTH = 22; // Ancho base del sprite frontal (en píxeles)
        private const int PLAYER_BASE_HEIGHT = 40; // Alto base del sprite frontal (en píxeles)

        // Referencia de tamaño fijo (ya escalado)
        private const float PLAYER_REFERENCE_WIDTH = PLAYER_BASE_WIDTH * PLAYER_SCALE;
        private const float PLAYER_REFERENCE_HEIGHT = PLAYER_BASE_HEIGHT * PLAYER_SCALE;

        // Lista de barreras de colision
        private List<Rectangle> _collisionBarriers;

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

            // Empezar mirando al jugador
            _specterCurrent = _specterWalkFront;

            // Posicion del mapa, para despues tener la posicion del jugador
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

            _previousKeyboardState = Keyboard.GetState();
        }

        private void PopulateCollisionBarriers()
        {
            int scale = MAP_SCALE_FACTOR;
            int mapX = (int)_mapPosition.X;
            int mapY = (int)_mapPosition.Y;

            // Bordes habitacion
            _collisionBarriers.Add(
                new Rectangle(mapX + (0 * scale), mapY + (0 * scale), 128 * scale, 4 * scale)
            );
            // Muro inferior
            _collisionBarriers.Add(
                new Rectangle(mapX + (0 * scale), mapY + (125 * scale), 128 * scale, 3 * scale)
            );
            // Muro izquierdo
            _collisionBarriers.Add(
                new Rectangle(mapX + (0 * scale), mapY + (0 * scale), 4 * scale, 128 * scale)
            );
            // Muro derecho
            _collisionBarriers.Add(
                new Rectangle(mapX + (125 * scale), mapY + (0 * scale), 3 * scale, 127 * scale)
            );

            // Objetos
            _collisionBarriers.Add(
                new Rectangle(mapX + (3 * scale), mapY + (78 * scale), 23 * scale, 47 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (41 * scale), mapY + (4 * scale), 13 * scale, 35 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (55 * scale), mapY + (5 * scale), 22 * scale, 22 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (77 * scale), mapY + (5 * scale), 13 * scale, 34 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (98 * scale), mapY + (100 * scale), 27 * scale, 25 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (96 * scale), mapY + (5 * scale), 29 * scale, 28 * scale)
            );
            _collisionBarriers.Add(
                new Rectangle(mapX + (3 * scale), mapY + (4 * scale), 35 * scale, 15 * scale)
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
                {
                    return true;
                }
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                _game.Exit(); // para hacer: pasarlo a un menu de pausa en vez de salir directamente

            // Pantalla completa
            if (
                currentKeyboardState.IsKeyDown(Keys.F11)
                && !_previousKeyboardState.IsKeyDown(Keys.F11)
            )
            {
                Core.Graphics.ToggleFullScreen();
                Core.Graphics.ApplyChanges();
            }

            // Movimiento
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

            // Actualizar estado de animacion y 'isMoving'
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

            // Verificar colisiones
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

            // Actualizar animacion
            if (_isMoving)
            {
                _specterCurrent.Update(gameTime);
            }
            else
            {
                _specterCurrent.CurrentFrame = 0;
            }

            _previousKeyboardState = currentKeyboardState;
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

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

            _specterCurrent.Draw(SpriteBatch, _specterPosition);

            SpriteBatch.End();
        }
    }
}
