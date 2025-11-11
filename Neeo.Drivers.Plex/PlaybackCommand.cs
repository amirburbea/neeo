using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public enum PlaybackCommand
{
    [Text("pause")]
    Pause,

    [Text("play")]
    Play,

    [Text("skipNext")]
    SkipNext,

    [Text("skipPrevious")]
    SkipPrevious,

    [Text("stepBack")]
    StepBack,

    [Text("stepForward")]
    StepForward,

    [Text("stop")]
    Stop
}
