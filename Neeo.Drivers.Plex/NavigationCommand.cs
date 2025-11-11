using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public enum NavigationCommand
{
    Back,
    ContextMenu,
    Home,

    [Text("moveUp")]
    MoveUp,

    [Text("moveDown")]
    MoveDown,

    [Text("moveLeft")]
    MoveLeft,

    [Text("moveRight")]
    MoveRight,

    [Text("select")]
    Select,

    NextLetter,
    PreviousLetter,
    PageUp, 
    PageDown,
}
