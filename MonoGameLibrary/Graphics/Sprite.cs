using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class Sprite
{
    //gets or sets the source texture region
    public TextureRegion Region { get; set; }

    //gets or sets te color mask
    //default value = Color.White
    public Color Color { get; set; } = Color.White;

    //Gets or sets the rotation to apply when rendering
    //Default value is 0.0f
    public float Rotation { get; set; } = 0.0f;

    //Gets or sets the scale factor to apply to the x and y axes when rendering this sprite
    //default value is Vector2.One
    public Vector2 Scale { get; set; } = Vector2.One;

    //Gets or sets coordinate origin point relative to the 0,0
    //default = Vector2.Zero
    public Vector2 Origin { get; set; } = Vector2.Zero;

    //gets or sets the sprite effects to apply when rendering this sprite
    //default value is spriteeffects.none
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    //Gets or sets the layer depth to apply when rendering the sprite
    //default value = 0.0f
    public float LayerDepth { get; set; } = 0.0f;

    //Gets the width
    //Width is calculated by multiplying the width of the source texture region by the x-axis scale factor.
    public float Width => Region.Width * Scale.X;

    //Gets the height
    //Height is calculated by multiplying the height of the source texture region by the y-axis scale factor.
    public float Height => Region.Height * Scale.Y;

    //create a new sprite
    public Sprite() { }

    //create a new sprite using the source region
    //The texture region to use as the source texture region for this sprite.
    public Sprite(TextureRegion region)
    {
        Region = region;
    }

    //sets the origin's center
    public void CenterOrigin()
    {
        Origin = new Vector2(Region.Width, Region.Height) * 0.5f;
    }

    //Submit the sprite to the current batch
    //The SpriteBatch instance used for batching draw calls.
    //The xy-coordinate position to render this sprite at.
    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        Region.Draw(spriteBatch, position, Color, Rotation, Origin, Scale, Effects, LayerDepth);
    }
}
