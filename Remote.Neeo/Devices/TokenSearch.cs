using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Remote.Neeo.Web
{

    public sealed class TokenSearch<T> : TokenSearch
        where T : class
    {
        private readonly IReadOnlyCollection<T> _items;

        public TokenSearch(IReadOnlyCollection<T> items, params string[] tokenProperties)
            : this(items, (IReadOnlyCollection<string>)tokenProperties)
        {
        }

        public TokenSearch(IReadOnlyCollection<T> items, IReadOnlyCollection<string>? tokenProperties)
            : base(typeof(T), tokenProperties)
        {
            this._items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public Func<T, bool>? Filter { get; set; }

        private new Func<T, IEnumerable<string>> GetTokens => (Func<T, IEnumerable<string>>)base.GetTokens;
    }

    public abstract class TokenSearch
    {
        private static readonly MethodInfo _addMethod = typeof(List<string>).GetMethod(nameof(List<string>.Add), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo _addRangeMethod = typeof(List<string>).GetMethod(nameof(List<string>.AddRange), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo _toStringMethod = typeof(object).GetMethod(nameof(object.ToString), BindingFlags.Public | BindingFlags.Instance)!;

        protected TokenSearch(Type itemType, IReadOnlyCollection<string>? tokenProperties)
        {
            this.GetTokens = tokenProperties is null or { Count: 0 }
                ? Delegate.CreateDelegate(
                      typeof(Func<,>).MakeGenericType(itemType, typeof(IEnumerable<string>)),
                      typeof(TokenSearch).GetMethod(nameof(TokenSearch.GetTokensViaToString), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(itemType)
                  )
                : TokenSearch.CreateGetTokens(itemType, tokenProperties);
        }

        /// <summary>
        /// At what point does the match algorithm give up. A threshold of 0 requires a perfect match
        /// (of both letters and location), a threshold of 1 would match anything.
        /// </summary>
        public double Threshold { get; set; } = 0.7;

        /// <summary>
        /// Should the result just contain unique results.
        /// </summary>
        public bool Unique { get; set; }

        protected Delegate GetTokens { get; }

        private static Delegate CreateGetTokens(Type itemType, IReadOnlyCollection<string> propertyNames)
        {
            ParameterExpression itemParameter = Expression.Parameter(itemType);
            ParameterExpression listVariable = Expression.Variable(typeof(List<string>));
            List<Expression> statements = new()
            {
                Expression.Assign(
                    listVariable,
                    Expression.New(typeof(List<string>))
                )
            };
            foreach (string propertyName in propertyNames)
            {
                MemberExpression memberExpression = Expression.Property(
                    itemParameter,
                    propertyName
                );
                bool isCollection = typeof(IEnumerable<string>).IsAssignableFrom(memberExpression.Type);
                Expression expression = Expression.Call(
                    listVariable,
                    isCollection ? TokenSearch._addRangeMethod : TokenSearch._addMethod,
                    isCollection ? memberExpression : Expression.Call(
                        memberExpression,
                        TokenSearch._toStringMethod
                    )
                );
                bool allowsNull = !memberExpression.Type.IsValueType || memberExpression.Type.IsGenericType && memberExpression.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
                statements.Add(allowsNull ? expression : Expression.IfThen(
                    Expression.NotEqual(
                        memberExpression,
                        Expression.Default(memberExpression.Type)
                    ),
                    expression
                ));
            }
            statements.Add(listVariable);
            LambdaExpression lambda = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(itemType, typeof(IEnumerable<string>)),
                Expression.Block(
                    new[] { listVariable },
                    statements
                ),
                itemParameter
            );
            return lambda.Compile();
        }

        private static string[] GetTokensViaToString<T>(T item) => new[] { item?.ToString() ?? string.Empty };
    }
}
