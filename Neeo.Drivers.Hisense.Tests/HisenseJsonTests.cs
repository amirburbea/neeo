using System.Text.Json;
using Xunit;

namespace Neeo.Drivers.Hisense.Tests;

public sealed class HisenseJsonTests
{
    [Fact]
    public void AppInfo_deserializes_and_serializes_correctly()
    {
        const string json = """[{"name":"Netflix","url":"netflix_url","isunInstalled":false},{"name":"YouTube","url":"youtube_url","isunInstalled":true}]""";

        AppInfo[]? apps = JsonSerializer.Deserialize(json, HisenseJsonSerializerContext.Default.AppInfoArray);

        Assert.NotNull(apps);
        Assert.Equal(2, apps.Length);
        Assert.Equal("Netflix", apps[0].Name);
        Assert.Equal("netflix_url", apps[0].Url);
        Assert.False(apps[0].IsUninstalled);

        Assert.Equal("YouTube", apps[1].Name);
        Assert.Equal("youtube_url", apps[1].Url);
        Assert.True(apps[1].IsUninstalled);

        string serialized = JsonSerializer.Serialize(apps[0], HisenseJsonSerializerContext.Default.AppInfo);
        Assert.Contains("\"name\":\"Netflix\"", serialized);
    }

    [Fact]
    public void SourceInfo_deserializes_correctly()
    {
        const string json = """[{"sourcename":"HDMI 1","sourceid":1,"displayname":"Cable Box"},{"sourcename":"HDMI 2","sourceid":2,"displayname":"PlayStation"}]""";

        SourceInfo[]? sources = JsonSerializer.Deserialize(json, HisenseJsonSerializerContext.Default.SourceInfoArray);

        Assert.NotNull(sources);
        Assert.Equal(2, sources.Length);
        Assert.Equal("HDMI 1", sources[0].Name);
        Assert.Equal(1, sources[0].SourceId);
        Assert.Equal("Cable Box", sources[0].DisplayName);

        Assert.Equal("HDMI 2", sources[1].Name);
        Assert.Equal(2, sources[1].SourceId);
        Assert.Equal("PlayStation", sources[1].DisplayName);
    }

    [Fact]
    public void VolumeData_deserializes_value()
    {
        const string json = """{"volume_type":0,"volume_value":35}""";

        HisenseTV.VolumeData data = JsonSerializer.Deserialize(json, HisenseJsonSerializerContext.Default.VolumeData);

        Assert.Equal(0, data.Type);
        Assert.Equal(35, data.Value);
    }

    [Fact]
    public void StateData_deserializes_types_and_fields()
    {
        const string appJson = """{"statetype":"app","name":"Prime Video","url":"prime_url"}""";
        HisenseTV.StateData appData = JsonSerializer.Deserialize(appJson, HisenseJsonSerializerContext.Default.StateData);
        Assert.Equal("app", appData.Type);
        Assert.Equal("Prime Video", appData.Name);
        Assert.Equal("prime_url", appData.Url);

        const string tvJson = """{"statetype":"livetv"}""";
        HisenseTV.StateData tvData = JsonSerializer.Deserialize(tvJson, HisenseJsonSerializerContext.Default.StateData);
        Assert.Equal("livetv", tvData.Type);
    }

    [Fact]
    public void AuthPayload_and_SuccessPayload_round_trip()
    {
        HisenseTV.AuthPayload auth = new("4829");
        string json = JsonSerializer.Serialize(auth, HisenseJsonSerializerContext.Default.AuthPayload);
        Assert.Contains("\"AuthNum\":\"4829\"", json);

        const string successJson = """{"result":1}""";
        HisenseTV.SuccessPayload success = JsonSerializer.Deserialize(successJson, HisenseJsonSerializerContext.Default.SuccessPayload);
        Assert.Equal(1, success.Result);

        const string failJson = """{"result":0}""";
        HisenseTV.SuccessPayload fail = JsonSerializer.Deserialize(failJson, HisenseJsonSerializerContext.Default.SuccessPayload);
        Assert.Equal(0, fail.Result);
    }
}
