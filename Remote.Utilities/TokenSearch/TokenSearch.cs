using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Remote.Utilities.TokenSearch
{
    /// <summary>
    /// A class to search a collection of items by tokens and return ranked results.
    /// </summary>
    /// <typeparam name="T">The type </typeparam>
    public sealed class TokenSearch<T>
        where T : notnull
    {
        internal static readonly Func<T, string, object?> GetItemValue = TokenSearch<T>.CreateGetItemValue();

        private readonly char[] _delimiter;
        private readonly int _maxFilterTokenEntries;
        private readonly PostProcessAlgorithm<T> _postProcessAlgorithm;
        private readonly Func<T, bool> _preProcessCheck;
        private readonly ScoringAlgorithm _scoringAlgorithm;
        private readonly string[]? _searchProperties;
        private readonly Comparer<SearchItem<T>> _sortAlgorithm;
        private readonly double _threshold;
        private readonly bool _unique;

        public TokenSearch(IReadOnlyCollection<T> collection, SearchOptions<T>? options = default)
        {
            this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this._delimiter = options?.Delimiter ?? new[] { ' ', '_', '-' };
            this._maxFilterTokenEntries = options?.MaxFilterTokenEntries ?? 5;
            this._threshold = options?.Threshold ?? 0.7;
            this._preProcessCheck = options?.PreProcessCheck ?? TokenSearch<T>.DefaultPreProcessCheck;
            this._sortAlgorithm = Comparer<SearchItem<T>>.Create(options?.SortAlgorithm ?? TokenSearch<T>.DefaultSortAlgorithm);
            this._scoringAlgorithm = options?.ScoringAlgorithm ?? TokenSearch<T>.DefaultSearchAlgorithm;
            this._postProcessAlgorithm = options?.PostProcessAlgorithm ?? TokenSearch<T>.DefaultPostProcessAlgorithm;
            this._searchProperties = options?.SearchProperties is { Length: > 1 } ? options.SearchProperties : null;
            this._unique = options?.Unique ?? false;
        }

        public IReadOnlyCollection<T> Collection { get; }

        public SearchItem<T>[] Search(string query)
        {
            string[] searchTokens = (query ?? throw new ArgumentNullException(nameof(query)))
                .Split(this._delimiter, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Take(this._maxFilterTokenEntries)
                .ToArray();
            List<SearchItem<T>> list = new();
            int maxScore = 0;
            foreach (T item in this.Collection.Where(this._preProcessCheck))
            {
                string[] dataTokens = this._searchProperties == null
                    ? new[] { item.ToString() ?? string.Empty }
                    : Array.ConvertAll(this._searchProperties, property => TokenSearch<T>.GetItemValue(item, property)?.ToString() ?? string.Empty);
                int score = dataTokens.Sum(token => this._scoringAlgorithm(token, searchTokens));
                if (score <= 0)
                {
                    continue;
                }
                maxScore = Math.Max(score, maxScore);
                list.Add(new(item) { Score = score });
            }
            SearchItem<T>[] array = this._postProcessAlgorithm(list, maxScore, this._threshold, this._unique, this._searchProperties).ToArray();
            Array.Sort(array, this._sortAlgorithm);
            return array;
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
                    Expression.Default(typeof(string)),
                    Array.ConvertAll(
                        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy),
                        property =>
                        {
                            Expression memberExpression = Expression.Property(
                                itemParameter,
                                property
                            );
                            return Expression.SwitchCase(
                                Expression.Convert(
                                    Expression.Property(
                                        itemParameter,
                                        property
                                    ),
                                    typeof(object)
                                ),
                                Expression.Constant(property.Name)
                            );
                        }
                    )
                ),
                itemParameter,
                nameParameter
            );
            return lambdaExpression.Compile();
        }

        private static IEnumerable<SearchItem<T>> DefaultPostProcessAlgorithm(IEnumerable<SearchItem<T>> searchItems, int maxScore, double threshold, bool unique, string[]? searchProperties)
        {
            double normalizedScore = 1d / maxScore;
            Predicate<string> accept = unique ? new HashSet<string>(StringComparer.OrdinalIgnoreCase).Add : key => true;
            foreach (SearchItem<T> item in searchItems)
            {
                item.Score = 1d - item.Score * normalizedScore;
                if (item.Score <= threshold && accept(searchProperties == null ? item.ToString() ?? string.Empty : string.Join(' ', searchProperties.Select(item.GetValue))))
                {
                    yield return item;
                }
            }
        }

        private static bool DefaultPreProcessCheck(T _) => true;

        private static int DefaultSearchAlgorithm(string text, IEnumerable<string> searchTokens)
        {
            return searchTokens.Sum(token =>
            {
                int index = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                {
                    return 0;
                }
                if (token.Length >= 2)
                {
                    if (text == token)
                    {
                        return 6;
                    }
                    if (index == 0)
                    {
                        return 2;
                    }
                }
                return 1;
            });
        }

        private static int DefaultSortAlgorithm(SearchItem<T> left, SearchItem<T> right) => left.Score.CompareTo(right.Score);
    }
}