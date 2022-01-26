namespace Neeo.Sdk.Devices.Controllers;

public interface IDirectoryController : IController
{
    ControllerType IController.Type => ControllerType.Directory;
}

internal class DirectoryController
{
}