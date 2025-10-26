using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics;

public class Animation
{
    //Texture regions that make the frames of this animation, the order of the regions within the collection are the order that the frames should be displayed in
    public List<TextureRegion> Frames { get; set; }

    //the amount of time to delay each frame
    public TimeSpan Delay { get; set; }

    public Animation()
    {
        Frames = new List<TextureRegion>();
        Delay = TimeSpan.FromMilliseconds(100);
    }

    //create a new animation of the frames and delay
    //frames = ordered collection of the frames for the animation
    //delay = time to delay between each frame
    public Animation(List<TextureRegion> frames, TimeSpan delay)
    {
        Frames = frames;
        Delay = delay;
    }
}
