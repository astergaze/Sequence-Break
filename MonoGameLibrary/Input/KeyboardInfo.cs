using Microsoft.Xna.Framework.Input;

namespace MonoGameLibrary.Input;

public class KeyboardInfo
{
    //gets the state of keyboard input during the previous update cycle
    public KeyboardState PreviousState { get; private set; }

    //gets the state of the keyboard during the current input cycle
    public KeyboardState CurrentState { get; private set; }

    //Creates a new KeyboardInfo
    public KeyboardInfo()
    {
        PreviousState = new KeyboardState();
        CurrentState = Keyboard.GetState();
    }

    //updates the sate information about keyboard input
    public void Update()
    {
        PreviousState = CurrentState;
        CurrentState = Keyboard.GetState();
    }

    //Returns value if the key is pressed
    public bool IsKeyDown(Keys key)
    {
        return CurrentState.IsKeyDown(key);
    }

    //returns a value that says wether the key is up
    public bool IsKeyUp(Keys key)
    {
        return CurrentState.IsKeyUp(key);
    }

    //Returns a value that says if the key is just pressed in the current frame
    public bool WasKeyJustPressed(Keys key)
    {
        return CurrentState.IsKeyDown(key) && PreviousState.IsKeyUp(key);
    }

    //Says if the key was released in this frame
    public bool WasKeyJustReleased(Keys key)
    {
        return CurrentState.IsKeyUp(key) && PreviousState.IsKeyDown(key);
    }
}
