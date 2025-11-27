using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Sequence_Break
{
    public class Enemy
    {
        private Vector2 _position;
        private List<Vector2> _patrolPoints;
        private int _currentPatrolIndex;
        private float _speed;
        private float _visionRange;
        public int Id { get; private set; }

        // Visuales
        private AnimatedSprite _spriteBack;
        private AnimatedSprite _spriteFront;
        private AnimatedSprite _spriteSide;
        private AnimatedSprite _currentSprite;

        // Control de dirección
        private bool _facingLeft;

        public Rectangle BoundingBox =>
            new Rectangle((int)_position.X, (int)_position.Y, 36 * 3, 42 * 3);

        public Enemy(
            int id,
            AnimatedSprite back,
            AnimatedSprite front,
            AnimatedSprite side,
            List<Vector2> points,
            float speed,
            float vision
        )
        {
            Id = id;
            _spriteBack = back;
            _spriteFront = front;
            _spriteSide = side;

            _patrolPoints = points;
            _speed = speed;
            _visionRange = vision;

            _currentSprite = _spriteFront;

            if (_patrolPoints.Count > 0)
            {
                _position = _patrolPoints[0];
                _currentPatrolIndex = 0;
            }
        }

        public void Update(GameTime gameTime, Vector2 playerPosition)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _currentSprite.Update(gameTime);

            float distanceToPlayer = Vector2.Distance(_position, playerPosition);

            if (distanceToPlayer < _visionRange)
            {
                MoveTowards(playerPosition, dt);
            }
            else
            {
                Patrol(dt);
            }
        }

        private void Patrol(float dt)
        {
            if (_patrolPoints.Count == 0)
                return;

            Vector2 target = _patrolPoints[_currentPatrolIndex];

            if (Vector2.Distance(_position, target) < 5.0f)
            {
                _currentPatrolIndex++;
                if (_currentPatrolIndex >= _patrolPoints.Count)
                {
                    _currentPatrolIndex = 0;
                }
            }

            MoveTowards(target, dt);
        }

        private void MoveTowards(Vector2 target, float dt)
        {
            Vector2 direction = target - _position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();

                if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                {
                    _currentSprite = _spriteSide;

                    if (direction.X < 0)
                        _facingLeft = true;
                    else
                        _facingLeft = false;
                }
                else
                {
                    if (direction.Y < 0)
                        _currentSprite = _spriteBack;
                    else
                        _currentSprite = _spriteFront;

                    _facingLeft = false;
                }

                _position += direction * _speed;
            }
        }

        // --- CORRECCIÓN APLICADA AQUÍ ---
        public void Draw(SpriteBatch spriteBatch)
        {
            // 1. Calculamos el efecto espejo
            SpriteEffects effect = SpriteEffects.None;

            if (_currentSprite == _spriteSide && _facingLeft)
            {
                effect = SpriteEffects.FlipHorizontally;
            }

            // 2. Asignamos el efecto a la PROPIEDAD del sprite
            _currentSprite.Effects = effect;

            // 3. Llamamos al Draw simple (solo 2 argumentos)
            _currentSprite.Draw(spriteBatch, _position);
        }
    }
}
