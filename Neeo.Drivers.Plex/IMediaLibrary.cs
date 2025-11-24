using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

public interface IMediaLibrary
{
    Task<LibrarySectionDetail> GetSectionDetailAsync(int sectionKey, CancellationToken cancellationToken = default);

    Task<char[]> ListFirstCharactersAsync(int sectionKey, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMoviesAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMoviesByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMoviesRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMusicAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMusicByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListMusicRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<LibrarySection[]> ListSectionsAsync(CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListTVShowsAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default);

    Task<MediaDirectory> ListTVShowsByFirstCharacterAsync(int sectionKey, char character, PaginationParameters parameters, CancellationToken cancellationToken);

    Task<MediaDirectory> ListTVShowsRecentlyAddedAsync(int sectionKey, PaginationParameters parameters, CancellationToken cancellationToken);
}
