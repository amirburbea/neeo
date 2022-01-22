using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neeo.Api.Utilities.TokenSearch;

/// <summary>
/// A class to search a collection of items by tokens and return ranked results.
/// </summary>
/// <typeparam name="T">The type </typeparam>
internal sealed class TokenSearch<T>
    where T : notnull, IComparable<T>
{
    internal static readonly Func<T, string, object?> GetItemValue = TokenSearch<T>.CreateGetItemValue();

    private readonly char[] _delimiter;
    private readonly int _maxFilterTokenEntries;
    private readonly Func<T, bool>? _preProcessCheck;
    private readonly string[]? _searchProperties;
    private readonly double _threshold;
    private readonly bool _unique;

    public TokenSearch(SearchOptions<T>? options = default)
    {
        this._delimiter = options?.Delimiter ?? new[] { ' ', '_', '-' };
        this._maxFilterTokenEntries = options?.MaxFilterTokenEntries ?? 5;
        this._threshold = options?.Threshold ?? 0.7;
        this._preProcessCheck = options?.PreProcessCheck;
        this._searchProperties = options?.SearchProperties is { Length: > 1 } ? options.SearchProperties : null;
        this._unique = options?.Unique ?? false;
    }

    public IEnumerable<ISearchItem<T>> Search(IEnumerable<T> collection, string query)
    {
        string[] searchTokens = (query ?? throw new ArgumentNullException(nameof(query)))
            .Split(this._delimiter, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(this._maxFilterTokenEntries)
            .ToArray();
        List<SearchItem<T>> list = new();
        int maxScore = 0;
        foreach (T item in collection ?? throw new ArgumentNullException(nameof(collection)))
        {
            if (this._preProcessCheck != null && !this._preProcessCheck(item))
            {
                continue;
            }
            string[] dataTokens = this._searchProperties == null
                ? new[] { item.ToString() ?? string.Empty }
                : Array.ConvertAll(
                    this._searchProperties,
                    property => TokenSearch<T>.GetItemValue(item, property)?.ToString() ?? string.Empty
                  );
            int score = dataTokens.Sum(dataToken => TokenSearch<T>.Score(dataToken, searchTokens));
            if (score <= 0)
            {
                continue;
            }
            maxScore = Math.Max(score, maxScore);
            list.Add(new(item) { Score = score });
        }
        return TokenSearch<T>.Normalize(list, maxScore, this._threshold, this._unique, this._searchProperties).OrderBy(x => x);
    }

    /// <summary>
    /// Create a function such that when given an item and property name
    /// returns the value of the property for the item.
    /// </summary>
    /// <returns>The created function.</returns>
    private static Func<T, string, object?> CreateGetItemValue()
    {
        ParameterExpression itemParameter = Expression.Parameter(typeof(T));
        ParameterExpression nameParameter = Expression.Parameter(typeof(string));
        Expression<Func<T, string, object?>> lambdaExpression = Expression.Lambda<Func<T, string, object?>>(
            Expression.Switch(
                nameParameter,
                Expression.Default(typeof(object)),
                Array.ConvertAll(
                    typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy),
                    property => Expression.SwitchCase(
                        Expression.Convert(
                            Expression.Property(
                                itemParameter,
                                property
                            ),
                            typeof(object)
                        ),
                        Expression.Constant(property.Name)
                    )
                )
            ),
            itemParameter,
            nameParameter
        );
        return lambdaExpression.Compile();
    }

    private static IEnumerable<SearchItem<T>> Normalize(IEnumerable<SearchItem<T>> searchItems, int maxScore, double threshold, bool unique, string[]? searchProperties)
    {
        double normalizedScore = 1d / maxScore;
        Predicate<string> accept = unique ? new HashSet<string>(StringComparer.OrdinalIgnoreCase).Add : _ => true;
        foreach (SearchItem<T> item in searchItems)
        {
            item.Score = 1d - item.Score * normalizedScore;
            item.MaxScore = maxScore;
            if (item.Score <= threshold && accept(searchProperties == null ? item.ToString() ?? string.Empty : string.Join(' ', searchProperties.Select(item.GetValue))))
            {
                yield return item;
            }
        }
    }

    private static int Score(string text, IEnumerable<string> searchTokens) => searchTokens.Sum(token =>
    {
        int index = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            return 0;
        }
        if (token.Length < 2)
        {
            return 1;
        }
        if (text == token)
        {
            return 6;
        }
        if (index == 0)
        {
            return 2;
        }
        return 1;
    });
}
