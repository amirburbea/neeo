using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IReadOnlyDictionary<string, ServerData> Data { get; }

    IPlexServer? GetServer(string machineIdentifier);

    Task InitializeAsync();
}

internal sealed class PlexServerManager : IPlexServerManager, IDisposable
{
    private static readonly byte[] _gdmRequest = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly ConcurrentDictionary<string, ServerData> _discoveryData = [];
    private readonly Lazy<IDisposable> _discoverySubscription;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initializationSource = new();
    private readonly Lock _lock = new();
    private readonly ILogger<PlexServerManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, (PlexServer Server, List<IPlexServer> Proxies)> _servers = [];
    private readonly IPlexSettingsManager _settingsManager;
    private readonly IPlexTokenStore _tokenStore;

    public PlexServerManager(
        IHttpClientFactory httpClientFactory,
        IPlexSettingsManager settingsManager,
        IPlexTokenStore tokenStore,
        ILoggerFactory loggerFactory
    )
    {
        this._settingsManager = settingsManager;
        this._tokenStore = tokenStore;
        this._loggerFactory = loggerFactory;
        this._httpClient = httpClientFactory.CreateClient(nameof(Plex));
        this._logger = loggerFactory.CreateLogger<PlexServerManager>();
        this._discoverySubscription = new(
            () => Observable.Interval(TimeSpan.FromMinutes(5d))
                .Select(index => index + 1) // make interval index one-based.
                .StartWith(0L) // start immediately with 0.
                .Do(index =>
                {
                    if (index is 0)
                    {
                        this._logger.LogInformation("Starting Plex Discovery...");
                    }
                })
                .Select(_ => Observable.FromAsync(this.DiscoverServersViaGdmAsync))
                .Switch()
                .Subscribe(),
            true
        );
    }

    IReadOnlyDictionary<string, ServerData> IPlexServerManager.Data => this._discoveryData;

    public void Dispose()
    {
        if (this._discoverySubscription.IsValueCreated)
        {
            this._discoverySubscription.Value.Dispose();
        }
        List<IPlexServer> proxies;
        using (this._lock.EnterScope())
        {
            proxies = [.. this._servers.Values.SelectMany(tuple => tuple.Proxies)];
        }
        foreach (IPlexServer proxy in proxies)
        {
            proxy.Dispose();
        }
    }

    public IPlexServer? GetServer(string machineIdentifier)
    {
        if (!this._discoveryData.ContainsKey(machineIdentifier))
        {
            return null;
        }
        IPlexServer proxy;
        using (this._lock.EnterScope())
        {
            if (this._servers.GetValueOrDefault(machineIdentifier) is ({ } server, { } proxies))
            {
                proxies.Add(proxy = PlexServerProxy.Create(server));
            }
            else
            {
                this._servers.Add(
                    machineIdentifier,
                    (server = this.CreateServer(machineIdentifier), [proxy = PlexServerProxy.Create(server)])
                );
            }
        }
        proxy.Disposed
            .Take(1)
            .Subscribe(_ => this.OnProxyDisposed(proxy));
        return proxy;
    }

    public Task InitializeAsync()
    {
        _ = this._discoverySubscription.Value; // Ensure initialized.
        return this._initializationSource.Task;
    }

    private PlexServer CreateServer(string machineIdentifier) => new(
        machineIdentifier,
        this._httpClient,
        this._settingsManager,
        this._tokenStore,
        () => this._discoveryData[machineIdentifier],
        this._loggerFactory.CreateLogger<PlexServer>()
    );

    private async Task DiscoverServersViaGdmAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            this._initializationSource.TrySetCanceled(cancellationToken);
            return;
        }
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(TimeSpan.FromSeconds(1.5d));
        using UdpClient udpClient = new() { Client = { EnableBroadcast = true } };
        await udpClient.SendAsync(PlexServerManager._gdmRequest, new(IPAddress.Broadcast, Constants.DiscoveryPort), source.Token).ConfigureAwait(false);
        while (!source.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(source.Token).ConfigureAwait(false);
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (!response.StartsWith("HTTP/1.0 200 OK"))
                {
                    continue;
                }
                string ipAddress = result.RemoteEndPoint.Address.ToString();
                using StringReader reader = new(response);
                string name = string.Empty;
                string machineIdentifier = string.Empty;
                int port = 0;
                while (reader.ReadLine() is { } line)
                {
                    int index = line.IndexOf(':');
                    if (index is -1)
                    {
                        continue;
                    }
                    string value = line[(index + 2)..]; // : is always followed first by a space.
                    switch (line[..index])
                    {
                        case Constants.NamePrefix:
                            name = value;
                            break;
                        case Constants.PortPrefix:
                            port = int.Parse(value);
                            break;
                        case Constants.ResourceIdentifierPrefix:
                            machineIdentifier = value;
                            break;
                        default:
                            continue;
                    }
                    if ((name, machineIdentifier, port) is (not "", not "", not 0))
                    {
                        this._discoveryData.AddOrUpdate(
                            machineIdentifier,
                            (id) =>
                            {
                                this._logger.LogInformation("Discovered Plex server '{Name}' ({IPAddress})", name, ipAddress);
                                return new(name, id, ipAddress, port);
                            },
                            (id, existing) =>
                            {
                                if ((name, port, ipAddress) == (existing.Name, existing.Port, existing.IPAddress))
                                {
                                    return existing;
                                }
                                this._logger.LogInformation("Plex server '{Name}' ({IPAddress}) updated", name, ipAddress);
                                return new(name, id, ipAddress, port);
                            }
                        );
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.TimedOut)
            {
                break;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error receiving data");
                break;
            }
            finally
            {
                this._initializationSource.TrySetResult();
            }
        }
    }

    private void OnProxyDisposed(IPlexServer proxy)
    {
        using (this._lock.EnterScope())
        {
            if (this._servers.GetValueOrDefault(proxy.MachineIdentifier) is ({ } server, { } proxies) && proxies.Remove(proxy) && proxies.Count == 0)
            {
                this._servers.Remove(proxy.MachineIdentifier);
                server.Dispose();
            }
        }
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;
        public const string NamePrefix = "Name";
        public const string PortPrefix = "Port";
        public const string ResourceIdentifierPrefix = "Resource-Identifier";
    }

    private static class PlexServerProxy
    {
        public static Func<IPlexServer, IPlexServer> Create = PlexServerProxy.CreateProxyFactory();
        
        private static Func<IPlexServer, IPlexServer> CreateProxyFactory()
        {
            AssemblyName assemblyName = new($"{nameof(PlexServer)}Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{nameof(PlexServer)}Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType($"{nameof(PlexServer)}Proxy", TypeAttributes.Public, typeof(object), [typeof(IPlexServer)]);
            FieldBuilder subject = typeBuilder.DefineField("_disposed", typeof(Subject<Unit>), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder server = typeBuilder.DefineField("_server", typeof(IPlexServer), FieldAttributes.Private | FieldAttributes.InitOnly);
            PlexServerProxy.GenerateConstructor(typeBuilder, server: server, subject: subject);
            foreach (PropertyInfo property in typeof(IPlexServer).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.Name is nameof(IPlexServer.Disposed))
                {
                    PlexServerProxy.GenerateDisposedProperty(typeBuilder, subject, property);
                }
                else
                {
                    PlexServerProxy.GenerateProxiedProperty(typeBuilder, server, property);
                }
            }
            PlexServerProxy.GenerateDisposeMethod(typeBuilder, subject: subject);
            foreach (MethodInfo method in typeof(IPlexServer).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                {
                    // Getters/setters were already generated, so ignore.
                    continue;
                }
                PlexServerProxy.GenerateProxiedMethod(typeBuilder, server, method);
            }
            return PlexServerProxy.GenerateFactory(typeBuilder.CreateType());
        }

        private static void GenerateConstructor(TypeBuilder typeBuilder, FieldBuilder server, FieldBuilder subject)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName,
                CallingConventions.Standard,
                [typeof(IPlexServer)]
            );
            ILGenerator generator = constructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, server);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, typeof(Subject<Unit>).GetConstructor(Type.EmptyTypes)!);
            generator.Emit(OpCodes.Stfld, subject);
            generator.Emit(OpCodes.Ret);
        }

        private static void GenerateDisposedProperty(TypeBuilder typeBuilder, FieldBuilder subject, PropertyInfo declaredProperty)
        {
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                declaredProperty.Name,
                PropertyAttributes.None,
                declaredProperty.PropertyType,
                Type.EmptyTypes
            );
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                declaredProperty.GetMethod!.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                declaredProperty.PropertyType,
                Type.EmptyTypes
            );
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, subject);
            generator.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(methodBuilder);
        }

        private static void GenerateDisposeMethod(TypeBuilder typeBuilder, FieldBuilder subject)
        {
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose), Type.EmptyTypes)!;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                disposeMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(void),
                Type.EmptyTypes
            );
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, subject);
            generator.Emit(OpCodes.Call, typeof(Unit).GetProperty(nameof(Unit.Default), BindingFlags.Static | BindingFlags.Public)!.GetMethod!);
            generator.Emit(OpCodes.Callvirt, typeof(Subject<Unit>).GetMethod(nameof(Subject<>.OnNext), [typeof(Unit)])!);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, subject);
            generator.Emit(OpCodes.Callvirt, typeof(Subject<Unit>).GetMethod(nameof(Subject<>.OnCompleted), Type.EmptyTypes)!);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, subject);
            generator.Emit(OpCodes.Callvirt, disposeMethod);
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Compile a delegate equivalent to: <code>IPlexServer server => new PlexServerProxy(server);</code>
        /// </summary>
        private static Func<IPlexServer, IPlexServer> GenerateFactory(Type proxyType)
        {
            DynamicMethod dynamicMethod = new("CreateProxy", typeof(IPlexServer), [typeof(IPlexServer)]);
            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, proxyType.GetConstructor([typeof(IPlexServer)])!);
            generator.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate<Func<IPlexServer, IPlexServer>>();
        }

        private static MethodBuilder GenerateProxiedMethod(TypeBuilder typeBuilder, FieldBuilder server, MethodInfo declaredMethod)
        {
            ParameterInfo[] parameters = declaredMethod.GetParameters();
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                declaredMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                declaredMethod.ReturnType,
                parameters.Length != 0 ? Array.ConvertAll(parameters, parameter => parameter.ParameterType) : Type.EmptyTypes
            );
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, server);
            for (int index = 0; index < parameters.Length; index++)
            {
                generator.Emit(OpCodes.Ldarg_S, (short)(index + 1));
            }
            generator.Emit(OpCodes.Callvirt, declaredMethod);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        private static void GenerateProxiedProperty(TypeBuilder typeBuilder, FieldBuilder server, PropertyInfo declaredProperty)
        {
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                declaredProperty.Name,
                PropertyAttributes.None,
                declaredProperty.PropertyType,
                Type.EmptyTypes
            );
            if (declaredProperty.GetMethod is { } getMethod)
            {
                propertyBuilder.SetGetMethod(PlexServerProxy.GenerateProxiedMethod(typeBuilder, server, getMethod));
            }
            if (declaredProperty.SetMethod is { } setMethod)
            {
                propertyBuilder.SetSetMethod(PlexServerProxy.GenerateProxiedMethod(typeBuilder, server, setMethod));
            }
        }
    }
}
