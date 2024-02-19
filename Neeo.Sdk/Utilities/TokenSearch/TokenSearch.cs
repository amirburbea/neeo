using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Neeo.Sdk.Utilities.TokenSearch;

/// <summary>
/// A class to search a collection of items by tokens and return ranked results.
/// Based on <a href="https://github.com/neophob/tokensearch.js">tokensearch.js</a>.
/// </summary>
/// <typeparam name="T">The type of items being searched</typeparam>
internal sealed class TokenSearch<T>
    where T : notnull, IComparable<T>
{
    private readonly T[] _items;
    private readonly Func<T, object?[]> _itemValues;
    private readonly double _threshold;

    public TokenSearch(T[] items, double threshold = 0.7, params Expression<Func<T, object>>[] projections)
    {
        this._items = items;
        this._threshold = threshold;
        if (projections is not { Length: > 0 })
        {
            throw new ArgumentNullException(nameof(projections));
        }
        ParameterExpression item = Expression.Parameter(typeof(T), nameof(item));
        Visitor visitor = new(item);
        Expression<Func<T, object?[]>> lambdaExpression = Expression.Lambda<Func<T, object?[]>>(
            Expression.NewArrayInit(
                typeof(object),
                projections.Select(projection => visitor.Visit(projection.Body))
            ),
            item
        );
        this._itemValues = lambdaExpression.Compile();
    }

    public IEnumerable<SearchEntry<T>> Search(string query)
    {
        string[] searchTokens = (query ?? throw new ArgumentNullException(nameof(query)))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(5)
            .ToArray();
        int maxScore = 0;
        Dictionary<T, int> scores = [];
        foreach (T item in this._items)
        {
            int score = this._itemValues(item)
                .Select(value => value?.ToString())
                .OfType<string>()
                .Where(text => text.Length > 0)
                .Sum(text => searchTokens.Sum(searchToken => Score(text, searchToken)));
            if (score == 0)
            {
                continue;
            }
            maxScore = Math.Max(score, maxScore);
            scores[item] = score;
        }
        return CreateEntries()
            .OrderBy(entry => entry.Score)
            .ThenBy(entry => entry.Item);

        IEnumerable<SearchEntry<T>> CreateEntries()
        {
            double normalizedScore = 1d / maxScore;
            HashSet<string> hashSet = new(StringComparer.OrdinalIgnoreCase);
            foreach ((T item, int score) in scores)
            {
                double itemScore = 1d - score * normalizedScore;
                if (itemScore <= this._threshold & hashSet.Add(string.Join('|', this._itemValues(item))))
                {
                    yield return new(item, itemScore, maxScore);
                }
            }
        }

        static int Score(string text, string searchToken) => text.IndexOf(searchToken, StringComparison.OrdinalIgnoreCase) switch
        {
            // Token not found.
            -1 => 0,
            // Token is at beginning but not an exact match.
            0 when searchToken.Length != text.Length => 2,
            // Token is an exact match.
            0 => 6,
            // Token is not at the beginning.
            _ => 1,
        };
    }

    private sealed class Visitor(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression _) => parameter;
    }
}

public record struct SearchEntry<T>(T Item, double Score, int MaxScore);