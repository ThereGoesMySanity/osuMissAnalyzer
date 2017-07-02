using System;

namespace BMAPI.v1
{
    public enum OverlayOptions
    {
        Above = 0,
        Below = 1,
    }
    public enum GameMode
    {
        osu = 0,
        Taiko = 1,
        CatchtheBeat = 2,
        Mania = 3
    }

    [Flags]
    public enum EffectType
    {
        None = 0,
        Whistle = (1 << 1),
        Finish = (1 << 2),
        Clap = (1 << 3),
    }

    public enum SliderType
    {
        Linear = 0,
        PSpline = 1,
        Bezier = 2,
        CSpline = 3
    }
    
    [Flags]
    public enum TimingPointOptions
    {
        None = 0,
        KiaiTime = (1 << 0),
        OmitFirstBarLine = (1 << 3)
    }

    [Flags]
    public enum HitObjectType
    {
        None = 0,
        Circle = (1 << 0),
        Slider = (1 << 1),
        NewCombo = (1 << 2),
        Spinner = (1 << 3)
    }

    public enum ContentType
    {
        Video,
        Image
    }
}
