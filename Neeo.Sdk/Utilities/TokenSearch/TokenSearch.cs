using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neeo.Sdk.Utilities.TokenSearch;

/// <summary>
/// A class to search a collection of items by tokens and return ranked results.
/// Based on <a href="https://github.com/neophob/tokensearch.js">tokensearch.js</a>.
/// </summary>
/// <typeparam name="T">The type of items being searched</typeparam>
internal sealed class TokenSearch<T>
    where T : notnull, IComparable<T>
{
    public static readonly Func<T, string, object?> GetItemValue = TokenSearch<T>.CreateGetItemValue();

    private readonly T[] _items;
    private readonly string[] _searchProperties;

    public TokenSearch(T[] items, params string[] searchProperties)
    {
        this._items = items;
        this._searchProperties = searchProperties;
    }

    public IEnumerable<SearchEntry<T>> Search(string query)
    {
        string[] searchTokens = (query ?? throw new ArgumentNullException(nameof(query)))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(5)
            .ToArray();
        List<SearchEntry<T>> list = new();
        int maxScore = 0;
        foreach (T item in this._items)
        {
            IEnumerable<string> dataTokens = this._searchProperties == null
                  ? new[] { item.ToString() ?? string.Empty }
                  : this._searchProperties.Select(property => TokenSearch<T>.GetItemValue(item, property)?.ToString() ?? string.Empty);
            int score = dataTokens.Sum(dataToken => searchTokens.Sum(searchToken => TokenSearch<T>.Score(dataToken, searchToken)));
            if (score <= 0)
            {
                continue;
            }
            maxScore = Math.Max(score, maxScore);
            list.Add(new(item) { Score = score });
        }
        return TokenSearch<T>.Normalize(list, maxScore, this._searchProperties).OrderBy(x => x);
    }

    /// <summary>
    /// Create a function such that when given an item and property name
    /// returns the value of the property.
    /// </summary>
    /// <returns>The created function.</returns>
    private static Func<T, string, object?> CreateGetItemValue()
    {
        ParameterExpression itemParameter = Expression.Parameter(typeof(T));
        ParameterExpression nameParameter = Expression.Parameter(typeof(string));
        return Expression.Lambda<Func<T, string, object?>>(
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
        ).Compile();
    }

    private static IEnumerable<SearchEntry<T>> Normalize(IEnumerable<SearchEntry<T>> entries, int maxScore, string[] searchProperties)
    {
        double normalizedScore = 1d / maxScore;
        HashSet<string> hashSet = new(StringComparer.OrdinalIgnoreCase);
        foreach (SearchEntry<T> entry in entries)
        {
            entry.Score = 1d - entry.Score * normalizedScore;
            entry.MaxScore = maxScore;
            if (entry.Score <= 0.5 & hashSet.Add(string.Join(' ', searchProperties.Select(property => TokenSearch<T>.GetItemValue(entry.Item, property)))))
            {
                yield return entry;
            }
        }
    }

    private static int Score(string text, string searchToken)
    {
        int index = text.IndexOf(searchToken, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            return 0;
        }
        if (searchToken.Length < 2)
        {
            return 1;
        }
        if (text == searchToken)
        {
            return 6;
        }
        if (index == 0)
        {
            return 2;
        }
        return 1;
    }
}