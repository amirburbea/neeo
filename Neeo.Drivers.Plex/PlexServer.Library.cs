using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

partial class PlexServer
{
    private sealed class MediaLibrary(PlexServer server) : IMediaLibrary
    {
        public async Task<LibrarySectionDetail> GetSectionDetailAsync(int sectionKey, CancellationToken cancellationToken)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}", PaginationParameters.Empty, cancellationToken).ConfigureAwait(false);
            return new(container.Title, server.GetImageUri(container.Thumbnail!));
        }

        public async Task<char[]> ListFirstCharactersAsync(int sectionKey, CancellationToken cancellationToken)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync($"{sectionKey}/firstCharacter", null, cancellationToken).ConfigureAwait(false);
            return container.Directories is { } directories ? Array.ConvertAll(directories, directory => directory.Title[0]) : [];
        }

        public Task<MediaDirectory> ListMoviesAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/all", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListMoviesByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/firstCharacter/{Uri.EscapeDataString(char.ToString(character))}", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListMoviesRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/recentlyAdded", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListMusicAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/all", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListMusicByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/firstCharacter/{Uri.EscapeDataString(char.ToString(character))}", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListMusicRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/recentlyAdded", pagination, cancellationToken);
        }

        public async Task<LibrarySection[]> ListSectionsAsync(CancellationToken cancellationToken) => [..
            from directory in (await server.BrowseLibraryAsync(null, null, cancellationToken).ConfigureAwait(false)).Directories ?? []
            where directory.Type is not LibraryDirectoryType.Photo
            select new LibrarySection(
                int.Parse(directory.Key),
                directory.Title,
                directory.Type is { } type ? (LibrarySectionType)type : LibrarySectionType.Movie
            )
        ];

        public Task<MediaDirectory> ListTVShowsAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/all", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListTVShowsByFirstCharacterAsync(int sectionKey, char character, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/firstCharacter/{Uri.EscapeDataString(char.ToString(character))}", pagination, cancellationToken);
        }

        public Task<MediaDirectory> ListTVShowsRecentlyAddedAsync(int sectionKey, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            return this.ListMediaAsync($"{sectionKey}/recentlyAdded", pagination, cancellationToken);
        }

        private async Task<MediaDirectory> ListMediaAsync(string path, PaginationParameters pagination, CancellationToken cancellationToken)
        {
            LibraryMediaContainer container = await server.BrowseLibraryAsync(path, pagination, cancellationToken).ConfigureAwait(false);
            return new MediaDirectory(
                container.Title,
                container.TotalSize,
                container.Metadata is { } metadata
                    ? Array.ConvertAll(metadata, metadata => (IMediaDirectoryItem)server.CreateMediaItem(metadata))
                    : [],
                container.Thumbnail is { } thumbnail ? server.GetImageUri(thumbnail) : null
            );
        }
    }
}
