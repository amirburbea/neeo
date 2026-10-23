using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Neeo.Sdk.Server.Tests;

public sealed class ProgramTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("neeo-config-").FullName;

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NEEO_Kodi__Host", null);
        Environment.SetEnvironmentVariable("NEEO_Brain", null);
        Environment.SetEnvironmentVariable("NEEO_Drivers", null);
        Environment.SetEnvironmentVariable("Brain", null);
        Directory.Delete(this._directory, recursive: true);
    }

    [Fact]
    public void Environment_variable_overrides_json()
    {
        this.WriteJson("appsettings.json", """{ "Brain": "10.0.0.1" }""");
        Environment.SetEnvironmentVariable("NEEO_Brain", "10.0.0.2");

        IConfiguration configuration = this.Build();

        Assert.Equal("10.0.0.2", configuration["Brain"]);
    }

    [Fact]
    public void Environment_variable_uses_double_underscore_for_sections()
    {
        Environment.SetEnvironmentVariable("NEEO_Kodi__Host", "10.0.0.5");

        Assert.Equal("10.0.0.5", this.Build()["Kodi:Host"]);
    }

    [Fact]
    public void Environment_variables_without_the_prefix_are_ignored()
    {
        Environment.SetEnvironmentVariable("Brain", "10.0.0.9");

        Assert.Null(this.Build()["Brain"]);
    }

    [Fact]
    public void Command_line_overrides_environment_and_json()
    {
        this.WriteJson("appsettings.json", """{ "Brain": "10.0.0.1" }""");
        Environment.SetEnvironmentVariable("NEEO_Brain", "10.0.0.2");

        IConfiguration configuration = this.Build("--Brain=10.0.0.3");

        Assert.Equal("10.0.0.3", configuration["Brain"]);
    }

    [Fact]
    public void Environment_specific_json_overrides_base_json()
    {
        this.WriteJson("appsettings.json", """{ "Brain": "10.0.0.1" }""");
        this.WriteJson("appsettings.Testing.json", """{ "Brain": "10.0.0.4" }""");

        Assert.Equal("10.0.0.4", this.Build()["Brain"]);
    }

    [Fact]
    public void GetDriverPaths_reads_an_array()
    {
        this.WriteJson("appsettings.json", """{ "Drivers": ["a.dll", "b.dll"] }""");

        Assert.Equal(["a.dll", "b.dll"], Program.GetDriverPaths(this.Build()));
    }

    [Fact]
    public void GetDriverPaths_splits_a_semicolon_separated_value()
    {
        Environment.SetEnvironmentVariable("NEEO_Drivers", " a.dll;b.dll ;; c.dll ");

        Assert.Equal(["a.dll", "b.dll", "c.dll"], Program.GetDriverPaths(this.Build()));
    }

    [Fact]
    public void GetDriverPaths_semicolon_separated_value_replaces_the_json_array()
    {
        this.WriteJson("appsettings.json", """{ "Drivers": ["a.dll", "b.dll", "c.dll"] }""");
        Environment.SetEnvironmentVariable("NEEO_Drivers", "d.dll");

        Assert.Equal(["d.dll"], Program.GetDriverPaths(this.Build()));
    }

    [Fact]
    public void GetDriverPaths_throws_if_none_configured()
    {
        Assert.Throws<ApplicationException>(() => Program.GetDriverPaths(this.Build()));
    }

    private IConfiguration Build(params string[] args)
    {
        Mock<IHostEnvironment> mockEnvironment = new();
        mockEnvironment.Setup(environment => environment.EnvironmentName).Returns("Testing");
        ConfigurationBuilder builder = new();
        builder.SetFileProvider(new PhysicalFileProvider(this._directory));
        Program.ConfigureAppConfiguration(mockEnvironment.Object, builder, args);
        return builder.Build();
    }

    private void WriteJson(string fileName, string json) => File.WriteAllText(Path.Combine(this._directory, fileName), json);
}
