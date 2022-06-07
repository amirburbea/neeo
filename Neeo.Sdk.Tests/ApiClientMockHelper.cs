﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Tests;

internal static class ApiClientMockMethods
{
    private static readonly MethodInfo _fromResultMethod = typeof(Task).GetMethod("FromResult", BindingFlags.Static | BindingFlags.Public)!;
    private static readonly ConcurrentDictionary<Type, Func<Delegate, object, Task>> _functionCache = new();

    public static IReturnsResult<IApiClient> ReturnsTransformOf(this ISetup<IApiClient, Task<It.IsAnyType>> setup, object data) => setup.Returns(new InvocationFunc(invocation =>
    {
        Delegate transform = (Delegate)invocation.Arguments[invocation.Arguments.Count - 2];
        return ApiClientMockMethods._functionCache.GetOrAdd(transform.GetType(), ApiClientMockMethods.CreateFunction)(transform, data);
    }));

    private static Func<Delegate, object, Task> CreateFunction(Type transformType)
    {
        Type[] typeArguments = transformType.GetGenericArguments();
        ParameterExpression transform = Expression.Parameter(typeof(Delegate));
        ParameterExpression data = Expression.Parameter(typeof(object));
        return Expression.Lambda<Func<Delegate, object, Task>>(
            Expression.Convert(
                Expression.Call(
                    ApiClientMockMethods._fromResultMethod.MakeGenericMethod(typeArguments[1]),
                    Expression.Invoke(
                        Expression.Convert(
                            transform,
                            transformType
                        ),
                        Expression.Convert(
                            data,
                            typeArguments[0]
                        )
                    )
                ),
                typeof(Task)
            ),
            transform,
            data
        ).Compile();
    }
}