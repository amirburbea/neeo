using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Remote.Neeo
{
    public readonly struct SearchResult<T>
    {
        public SearchResult(T item, double score) => (this.Item, this.Score) = (item, score);

        public T Item { get; }

        public double Score { get; }
    }

    public sealed class TokenSearch<T>
    {
        private static readonly MethodInfo _addMethod = typeof(List<string>).GetMethod(nameof(List<string>.Add), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo _toStringMethod = typeof(object).GetMethod(nameof(object.ToString), BindingFlags.Public | BindingFlags.Instance)!;

        private readonly Func<T, IReadOnlyList<string>> _getTokens;

        public TokenSearch(IReadOnlyCollection<T> items, params string[] tokenProperties)
            : this(items, (IReadOnlyCollection<string>)tokenProperties)
        {
        }

        public TokenSearch(IReadOnlyCollection<T> items, IReadOnlyCollection<string>? tokenProperties)
        {
            this.Items = items ?? throw new ArgumentNullException(nameof(items));
            this._getTokens = tokenProperties == null || tokenProperties.Count == 0
                ? (item => new[] { item?.ToString() ?? string.Empty })
                : TokenSearch<T>.CreateGetTokens(tokenProperties);
        }

        public char Delimiter { get; set; } = ',';

        public Func<T, bool>? Filter { get; set; }

        public IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// At what point does the match algorithm give up. A threshold of 0 requires a perfect match
        /// (of both letters and location), a threshold of 1 would match anything.
        /// </summary>
        public double Threshold { get; set; } = 0.7;

        /// <summary>
        /// Should the result just contain unique results.
        /// </summary>
        public bool Unique { get; set; }

        public IReadOnlyCollection<SearchResult<T>> Search(string query)
        {
            return Array.Empty<SearchResult<T>>();
        }

        private static Func<T, IReadOnlyList<string>> CreateGetTokens(IReadOnlyCollection<string> tokenProperties)
        {
            ParameterExpression item = Expression.Parameter(
                typeof(T),
                nameof(item)
            );
            ParameterExpression output = Expression.Variable(
                typeof(List<string>),
                nameof(output)
            );
            List<Expression> statements = new()
            {
                Expression.Assign(
                    output,
                    Expression.New(output.Type)
                )
            };
            foreach (string propertyName in tokenProperties)
            {
                Expression propertyAccess = Expression.Property(
                    item,
                    propertyName
                );
                bool allowsNull = !propertyAccess.Type.IsValueType || propertyAccess.Type.IsGenericType && propertyAccess.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
                Expression addExpression = Expression.Call(
                    output,
                    TokenSearch<T>._addMethod,
                    propertyAccess.Type == typeof(string) ? propertyAccess : Expression.Call(
                        propertyAccess,
                        TokenSearch<T>._toStringMethod
                    )
                );
                statements.Add(!allowsNull ? addExpression : Expression.IfThen(
                    Expression.NotEqual(
                        propertyAccess,
                        Expression.Constant(
                            null,
                            propertyAccess.Type
                        )
                    ),
                    addExpression
                ));
            }
            statements.Add(output);
            Expression<Func<T, IReadOnlyList<string>>> lambdaExpression = Expression.Lambda<Func<T, IReadOnlyList<string>>>(
                Expression.Block(
                    new[] { output },
                    statements
                ),
                item
            );
            return lambdaExpression.Compile();
        }
    }
}
