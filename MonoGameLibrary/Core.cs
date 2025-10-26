using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary;

public class Core : Game
{
    internal static Core s_instance;

    //reference to core instance
    public static Core Instance => s_instance;

    //Graphics device manager to control graphics
    public static GraphicsDeviceManager Graphics { get; private set; }

    //Gets graphics device used for graphical resources and rendering
    public static new GraphicsDevice GraphicsDevice { get; private set; }

    //Sprite batch for 2d Rendering
    public static SpriteBatch SpriteBatch { get; private set; }

    //Get content manager
    public static new ContentManager Content { get; private set; }

    //Create a new core instance
    public Core(string title, int width, int height, bool fullScreen)
    {
        //Verify only one instance
        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only one core can be created");
        }
        //Store reference
        s_instance = this;
        //create graphics device manager
        Graphics = new GraphicsDeviceManager(this);
        //set graphics default
        Graphics.PreferredBackBufferWidth = width;
        Graphics.PreferredBackBufferHeight = height;
        Graphics.IsFullScreen = fullScreen;
        //apply graphics changes
        Graphics.ApplyChanges();
        //Set window title
        Window.Title = title;
        //Reference Content to the base game's content manager
        Content = base.Content;
        //Set root dir for cont
        Content.RootDirectory = "Content";
        //Mouse is visible by default
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        //set Core's Graphics device to the Game's graphics device
        GraphicsDevice = base.GraphicsDevice;
        //Create sprite batch instance
        SpriteBatch = new SpriteBatch(GraphicsDevice);
    }
}
