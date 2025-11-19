using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public enum NavigationCommand
{
    [Text("back")]
    Back,

    [Text("contextMenu")]
    ContextMenu,

    [Text("home")]
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

    [Text("nextLetter")]
    NextLetter,

    [Text("previousLetter")]
    PreviousLetter,

    [Text("pageUp")]
    PageUp,

    [Text("pageDown")]
    PageDown,
}
