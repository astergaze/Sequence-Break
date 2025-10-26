using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Graphics;

public class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation _animation;

    //gets or sets the animation for this sprite
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            if (_animation != null && _animation.Frames.Count > 0)
            {
                Region = _animation.Frames[0];
            }
        }
    }

    /// Gets or sets the actual animation frame, when it does, it resets the temporizer
    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            if (_animation == null || _animation.Frames.Count == 0)
                return;

            _currentFrame = value % _animation.Frames.Count;
            if (_currentFrame < 0)
                _currentFrame += _animation.Frames.Count;

            Region = _animation.Frames[_currentFrame];

            _elapsed = TimeSpan.Zero;
        }
    }

    //creates a new animated sprite
    public AnimatedSprite() { }

    //creates a new animated sprite with the frames and delay
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }

    //Updates this sprite
    //gameTime = snapshot of the game timing
    public void Update(GameTime gameTime)
    {
        // <-- ¡IMPORTANTE! Agrega esta comprobación
        if (_animation == null)
            return;

        _elapsed += gameTime.ElapsedGameTime;
        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;
            if (_currentFrame >= _animation.Frames.Count)
            {
                _currentFrame = 0;
            }
            Region = _animation.Frames[_currentFrame];
        }
    }
}
