using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Neeo.Sdk.Rest;

internal static class Server
{
    public static async Task<IHost> StartSdkAsync(
        IBrainInfo brain,
        IReadOnlyCollection<IDeviceBuilder> devices,
        string adapterName,
        IPAddress hostAddress,
        int port,
        Action<HostBuilderContext, ILoggingBuilder>? configureLogging,
        CancellationToken cancellationToken
    )
    {
        IHost host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => Server.ConfigureWebHostDefaults(builder, hostAddress, port))
            .ConfigureLogging(configureLogging ?? Server.ConfigureLoggingDefaults)
            .ConfigureServices(services => Server.ConfigureServices(services, brain, devices, adapterName))
            .Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options)
    {
        options.DictionaryKeyPolicy = options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    private static void ConfigureLoggingDefaults(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder.ClearProviders();
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.AddDebug();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IBrainInfo brain, IReadOnlyCollection<IDeviceBuilder> devices, string adapterName) => services
        .AddSingleton(brain)
        .AddSingleton(devices)
        .AddSingleton((SdkAdapterName)adapterName)
        .AddSingleton(Server.CreatePgpKeys()) // Keys are created at random at the start of the server.
        .AddSingleton<HttpMessageHandler>(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
        .AddSingleton<IApiClient, ApiClient>()
        .AddSingleton<IDeviceDatabase, DeviceDatabase>()
        .AddSingleton<IDynamicDeviceRegistry, DynamicDeviceRegistry>()
        .AddSingleton<INotificationMapping, NotificationMapping>()
        .AddSingleton<INotificationService, NotificationService>()
        .AddSingleton<ISdkEnvironment, SdkEnvironment>()
        .AddSingleton<PgpPublicKeyResponse>()
        .AddHostedService<SdkRegistration>()
        .AddHostedService<SubscriptionsNotifier>()
        .AddHostedService<UriPrefixNotifier>();

    private static void ConfigureWebHostDefaults(IWebHostBuilder builder, IPAddress hostAddress, int port) => builder
        .ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
            options.Listen(hostAddress, port);
        })
        .ConfigureServices(services =>
        {
            services
               .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
               .AddJsonOptions(options => Server.ConfigureJsonOptions(options.JsonSerializerOptions))
               .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
               .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AssemblyControllerFeatureProvider.Instance));
        })
        .Configure((context, builder) =>
        {
            builder
                .UseRouting()
                .UseCors(nameof(CorsPolicy))
                .UseEndpoints(endpoints => endpoints.MapControllers());
            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.UseDeveloperExceptionPage();
            }
        });

    private static PgpKeyPair CreatePgpKeys()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        RsaKeyPairGenerator generator = new();
        generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 768, 8));
        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();
        PgpSecretKey secretKey = new(PgpSignature.DefaultCertification, PublicKeyAlgorithmTag.RsaGeneral, pair.Public, pair.Private, DateTime.Now, Dns.GetHostName(), SymmetricKeyAlgorithmTag.Aes256, passphrase, null, null, random);
        return new(secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));
    }

    private static class Constants
    {
        public const int MaxRequestBodySize = 2 * 1024 * 1024;
    }

    private sealed class AssemblyControllerFeatureProvider : ControllerFeatureProvider
    {
        public static readonly ControllerFeatureProvider Instance = new AssemblyControllerFeatureProvider();

        protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
    }
}