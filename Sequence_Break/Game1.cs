using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Sequence_Break;

public class Game1 : Core
{
    // Specter variables
    private AnimatedSprite _specterWalkFront;
    private AnimatedSprite _specterWalkLeft;
    private AnimatedSprite _specterWalkRight;

    // To do: Add specter walk back when we get the animation thing

    private AnimatedSprite _specterCurrent;
    private Vector2 _specterPosition;
    private bool _isMoving;
    private Texture2D _roomTexture;

    //speed multiplier when moving
    private const float MOVEMENT_SPEED = 5.0f;

    public Game1()
        : base("Sequence Break", 1280, 720, false) { }

    protected override void Initialize()
    {
        // Specter Initial position, centered
        _specterPosition = new Vector2(
            Window.ClientBounds.Width / 2,
            Window.ClientBounds.Height / 2
        );

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Load Specter's 3 atlases (To do: fusion them into one atlas)
        //front
        TextureAtlas atlasFront = TextureAtlas.FromFile(
            Content,
            "textures/Specter-front-atlas-definition.xml"
        );
        _specterWalkFront = atlasFront.CreateAnimatedSprite("luka-walk-front");
        _specterWalkFront.Scale = new Vector2(4.0f, 4.0f); // Adjust scale
        //left
        TextureAtlas atlasLeft = TextureAtlas.FromFile(
            Content,
            "textures/Specter-left-atlas-definition.xml"
        );
        _specterWalkLeft = atlasLeft.CreateAnimatedSprite("luka-walk-left");
        _specterWalkLeft.Scale = new Vector2(4.0f, 4.0f);
        //right
        TextureAtlas atlasRight = TextureAtlas.FromFile(
            Content,
            "textures/Specter-right-atlas-definition.xml"
        );
        _specterWalkRight = atlasRight.CreateAnimatedSprite("luka-walk-right");
        _specterWalkRight.Scale = new Vector2(4.0f, 4.0f);
        //main room
        _roomTexture = Content.Load<Texture2D>("textures/Habitacion principal");
        //Load game with specter looking to the front
        _specterCurrent = _specterWalkFront;
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        _isMoving = false;

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        if (_isMoving)
        {
            _specterCurrent.Update(gameTime); // if Specter moves, change animation
        }
        else
        {
            _specterCurrent.CurrentFrame = 0; // if specter doesn't move, get to idle
        }

        base.Update(gameTime);
    }

    private void CheckKeyboardInput()
    {
        KeyboardState keyboardState = Keyboard.GetState();

        float speed = MOVEMENT_SPEED;
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        // Up
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
        {
            _specterPosition.Y -= speed;
            _specterCurrent = _specterWalkFront; // front for up
            _isMoving = true;
        }

        // down
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
        {
            _specterPosition.Y += speed;
            _specterCurrent = _specterWalkFront; // to do: change this into _specterwalkback (we don't have it)
            _isMoving = true;
        }

        // left
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            _specterPosition.X -= speed;
            _specterCurrent = _specterWalkLeft; // change sprite
            _isMoving = true;
        }

        // right
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            _specterPosition.X += speed;
            _specterCurrent = _specterWalkRight; // change sprite
            _isMoving = true;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // SamplerState.PointClamp makes the pixel art clear when scaling
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw map first
        Rectangle screenRectangle = new Rectangle(
            0,
            0,
            Window.ClientBounds.Width,
            Window.ClientBounds.Height
        );
        SpriteBatch.Draw(_roomTexture, screenRectangle, Color.White);
        // Draw specter on current position
        _specterCurrent.Draw(SpriteBatch, _specterPosition);

        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
