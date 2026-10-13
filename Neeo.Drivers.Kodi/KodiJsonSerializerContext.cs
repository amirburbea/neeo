using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Drivers.Kodi.Models;

namespace Neeo.Drivers.Kodi;

/// <summary>
/// Source-generated JSON metadata for the Kodi RPC and model types.
/// Combined with a reflection fallback in <see cref="KodiJsonSerializerOptions"/>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AlbumInfo))]
[JsonSerializable(typeof(AlbumInfo[]))]
[JsonSerializable(typeof(ArtistInfo))]
[JsonSerializable(typeof(ArtistInfo[]))]
[JsonSerializable(typeof(EpisodeInfo))]
[JsonSerializable(typeof(EpisodeInfo[]))]
[JsonSerializable(typeof(Filter))]
[JsonSerializable(typeof(FilterOperator))]
[JsonSerializable(typeof(ItemType))]
[JsonSerializable(typeof(PlayerDescriptor))]
[JsonSerializable(typeof(PlayerDescriptor[]))]
[JsonSerializable(typeof(PlayerState))]
[JsonSerializable(typeof(PlayState))]
[JsonSerializable(typeof(PlayerType))]
[JsonSerializable(typeof(SongInfo))]
[JsonSerializable(typeof(SongInfo[]))]
[JsonSerializable(typeof(TVShowInfo))]
[JsonSerializable(typeof(TVShowInfo[]))]
[JsonSerializable(typeof(VideoInfo))]
[JsonSerializable(typeof(VideoInfo[]))]
[JsonSerializable(typeof(QueryData<AlbumInfo>))]
[JsonSerializable(typeof(QueryData<ArtistInfo>))]
[JsonSerializable(typeof(QueryData<EpisodeInfo>))]
[JsonSerializable(typeof(QueryData<SongInfo>))]
[JsonSerializable(typeof(QueryData<TVShowInfo>))]
[JsonSerializable(typeof(QueryData<VideoInfo>))]
[JsonSerializable(typeof(KodiClient.AlbumListResult))]
[JsonSerializable(typeof(KodiClient.ArtistListResult))]
[JsonSerializable(typeof(KodiClient.EpisodeListResult))]
[JsonSerializable(typeof(KodiClient.ItemInfo))]
[JsonSerializable(typeof(KodiClient.JsonRpcError))]
[JsonSerializable(typeof(KodiClient.JsonRpcRequest))]
[JsonSerializable(typeof(KodiClient.JsonRpcResponse))]
[JsonSerializable(typeof(KodiClient.Limits))]
[JsonSerializable(typeof(KodiClient.MoviesListResult))]
[JsonSerializable(typeof(KodiClient.Notification))]
[JsonSerializable(typeof(KodiClient.PictureInfo))]
[JsonSerializable(typeof(KodiClient.PlayerInfo))]
[JsonSerializable(typeof(KodiClient.PlayerInfo[]))]
[JsonSerializable(typeof(KodiClient.PlayParameters))]
[JsonSerializable(typeof(KodiClient.ResponseParameters))]
[JsonSerializable(typeof(KodiClient.SeekResult))]
[JsonSerializable(typeof(KodiClient.SeekResult[]))]
[JsonSerializable(typeof(KodiClient.SongListResult))]
[JsonSerializable(typeof(KodiClient.SortOrder))]
[JsonSerializable(typeof(KodiClient.Time))]
[JsonSerializable(typeof(KodiClient.TVShowsListResult))]
[JsonSerializable(typeof(KodiClient.VolumeInfo))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement?))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
internal sealed partial class KodiJsonSerializerContext : JsonSerializerContext;
