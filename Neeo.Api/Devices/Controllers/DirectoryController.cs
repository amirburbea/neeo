namespace Neeo.Api.Devices.Controllers;

public interface IDirectoryController : IController
{
    ControllerType IController.Type => ControllerType.Directory;
}

internal class DirectoryController
{
}