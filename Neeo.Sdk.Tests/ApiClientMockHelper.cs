using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Tests;

internal static class ApiClientMockMethods
{
    private static readonly ConcurrentDictionary<Type, Delegate> _functionCache = new();

    /// <summary>
    /// Rather than mocking the result of <see cref="IApiClient.GetAsync"/> or <see cref="IApiClient.PostAsync"/>, which requires awareness of the generic transform
    /// function passed in (as the second to last argument), mock the <see cref="IApiClient"/> method such that it will act as though <paramref name="serverResponse"/>
    /// was the response of the REST call, by simply passing it to the supplied transform argument (from the call to Post/Get) and returns the result to the caller
    /// wrapped within a call to <see cref="Task.FromResult"/>.
    /// </summary>
    public static IReturnsResult<IApiClient> ReturnsTransformOf<TResponse>(this ISetup<IApiClient, Task<It.IsAnyType>> setup, [DisallowNull] TResponse serverResponse) => setup.Returns(new InvocationFunc(invocation =>
    {
        Delegate transform = (Delegate)invocation.Arguments[invocation.Arguments.Count - 2];
        Func<Delegate, TResponse, Task> function = (Func<Delegate, TResponse, Task>)ApiClientMockMethods._functionCache.GetOrAdd(transform.GetType(), ApiClientMockMethods.CreateFunction);
        return function(transform, serverResponse);
    }));

    private static Delegate CreateFunction(Type transformType)
    {
        Type[] typeArguments = transformType.GetGenericArguments();
        ParameterExpression transform = Expression.Parameter(typeof(Delegate), nameof(transform));
        ParameterExpression response = Expression.Parameter(typeArguments[0], nameof(response));
        LambdaExpression lambda = Expression.Lambda(
            Expression.Call(
                typeof(Task),
                nameof(Task.FromResult),
                [typeArguments[1]],
                Expression.Invoke(
                    Expression.Convert(
                        transform,
                        transformType
                    ),
                    response
                )
            ),
            transform,
            response
        );
        return lambda.Compile();
    }
}