using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

public interface IMediaLibrary
{
    Task<MediaDirectory> ListMoviesAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMoviesByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMoviesRecentlyAdded(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<char[]> ListFirstCharactersAsync(int sectionKey, CancellationToken cancellationToken = default);

    Task<LibrarySection[]> ListSectionsAsync(CancellationToken cancellationToken = default);

    Task<LibrarySectionDetail> GetSectionDetailAsync(int sectionKey, CancellationToken cancellationToken = default);
}
