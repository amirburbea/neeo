using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reflection.Emit;

namespace Neeo.Drivers.Plex;

internal static class PlexServerProxy
{
    public static readonly Func<IPlexServer, IPlexServer> Create = PlexServerProxy.CreateProxyFactory();

    private static Func<IPlexServer, IPlexServer> CreateProxyFactory()
    {
        AssemblyName assemblyName = new($"{nameof(PlexServerProxy)}Assembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{nameof(PlexServerProxy)}Module");
        TypeBuilder typeBuilder = moduleBuilder.DefineType(nameof(PlexServerProxy), TypeAttributes.Public, typeof(object), [typeof(IPlexServer)]);
        FieldBuilder subject = typeBuilder.DefineField("_disposed", typeof(Subject<Unit>), FieldAttributes.Private | FieldAttributes.InitOnly);
        FieldBuilder server = typeBuilder.DefineField("_server", typeof(IPlexServer), FieldAttributes.Private | FieldAttributes.InitOnly);
        PlexServerProxy.GenerateConstructor(typeBuilder, server: server, subject: subject);
        foreach (PropertyInfo property in typeof(IPlexServer).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (property is { Name: nameof(IPlexServer.Disposed) })
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
                continue;
            }
            PlexServerProxy.GenerateProxiedMethod(typeBuilder, server, method);
        }
        ParameterExpression parameter = Expression.Parameter(typeof(IPlexServer));
        return Expression.Lambda<Func<IPlexServer, IPlexServer>>(
            Expression.New(
                typeBuilder.CreateType().GetConstructor([typeof(IPlexServer)])!,
                parameter
            ),
            parameter
        ).Compile();
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
        typeBuilder.DefineMethodOverride(methodBuilder, declaredProperty.GetMethod!);
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
        generator.Emit(OpCodes.Callvirt, disposeMethod);
        generator.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(methodBuilder, disposeMethod);
    }

    private static void GenerateProxiedMethod(TypeBuilder typeBuilder, FieldBuilder server, MethodInfo declaredMethod)
    {
        ParameterInfo[] parameters = declaredMethod.GetParameters();
        MethodBuilder methodBuilder = typeBuilder.DefineMethod(
            declaredMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            declaredMethod.ReturnType,
            [.. from parameter in parameters select parameter.ParameterType]
        );
        ILGenerator generator = methodBuilder.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, server);
        for (int index = 0; index < parameters.Length; index++)
        {
            generator.Emit(OpCodes.Ldarg, index + 1);
        }
        generator.Emit(OpCodes.Callvirt, declaredMethod);
        generator.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(methodBuilder, declaredMethod);
    }

    private static void GenerateProxiedProperty(TypeBuilder typeBuilder, FieldBuilder server, PropertyInfo declaredProperty)
    {
        PropertyBuilder property = typeBuilder.DefineProperty(
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
        generator.Emit(OpCodes.Ldfld, server);
        generator.Emit(OpCodes.Callvirt, declaredProperty.GetMethod!);
        generator.Emit(OpCodes.Ret);
        property.SetGetMethod(methodBuilder);
        typeBuilder.DefineMethodOverride(methodBuilder, declaredProperty.GetMethod!);
    }
}
