namespace Neeo.Drivers.Kodi.Models;

public interface IMediaInfo
{
    string GetId();

    string GetTitle();

    string GetDescription();

    string GetCoverArt();
}