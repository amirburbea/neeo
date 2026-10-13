using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

public interface IMediaLibrary
{
    Task<LibrarySectionDetail> GetSectionDetailAsync(int sectionKey, CancellationToken cancellationToken = default);

    Task<char[]> ListFirstCharactersAsync(int sectionKey, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMediaAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMediaByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMediaRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<LibrarySection[]> ListSectionsAsync(CancellationToken cancellationToken = default);
}
