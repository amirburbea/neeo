using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

partial class PlexServer
{
    private sealed class MediaLibrary(PlexServer server) : IMediaLibrary
    {
        public async Task<LibrarySectionDetail> GetSectionDetailAsync(int sectionKey, CancellationToken cancellationToken = default)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}", PaginationParameters.Empty, cancellationToken).ConfigureAwait(false);
            return new(container.Title, server.GetImageUri(container.Thumbnail!));
        }

        public async Task<char[]> ListFirstCharactersAsync(int sectionKey, CancellationToken cancellationToken = default)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}/firstCharacter", null, cancellationToken).ConfigureAwait(false);
            return container.Directories is { } directories ? Array.ConvertAll(directories, directory => directory.Key[0]) : [];
        }

        public async Task<MediaDirectory> ListMoviesAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}/all", pagination, cancellationToken).ConfigureAwait(false);
            return new MediaDirectory(
                container.Title,
                container.TotalSize,
                container.Metadata is { } metadata ? Array.ConvertAll(metadata, server.CreateMediaItem) : [],
                container.Thumbnail is { } thumbnail ? server.GetImageUri(thumbnail) : null
            );
        }

        public async Task<MediaDirectory> ListMoviesByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken = default)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}/firstCharacter/{character}", pagination, cancellationToken).ConfigureAwait(false);
            return new MediaDirectory(
                container.Title,
                container.TotalSize,
                container.Metadata is { } metadata ? Array.ConvertAll(metadata, server.CreateMediaItem) : [], 
                container.Thumbnail is { } thumbnail ? server.GetImageUri(thumbnail) : null
            );
        }

        public Task<MediaDirectory> ListMoviesRecentlyAdded(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<LibrarySection[]> ListSectionsAsync(CancellationToken cancellationToken = default) => [..
            from directory in (await server.BrowseLibraryAsync(null, null, cancellationToken).ConfigureAwait(false)).Directories ?? []
            where directory.Type is not LibraryDirectoryType.Photo
            select new LibrarySection(
                int.Parse(directory.Key),
                directory.Title,
                directory.Type is { } type ? (LibrarySectionType)type : LibrarySectionType.Movie
            )
        ];
    }
}
