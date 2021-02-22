using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Remote.Utilities.TokenSearch
{
    public sealed class TokenSearch<T>
    {
        internal static readonly Func<T, string, object?> GetItemValue = TokenSearch<T>.CreateGetItemValue();

        private readonly char[] _delimiter;
        private readonly int _maxFilterTokenEntries;
        private readonly PostProcessAlgorithm<T> _postProcessAlgorithm;
        private readonly Func<T, bool> _preProcessCheck;
        private readonly SearchAlgorithm _searchAlgorithm;
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
            this._searchAlgorithm = options?.SearchAlgorithm ?? TokenSearch<T>.DefaultSearchAlgorithm;
            this._postProcessAlgorithm = options?.PostProcessAlgorithm ?? TokenSearch<T>.DefaultPostProcessAlgorithm;
            this._searchProperties = options?.SearchProperties is { Length: > 1 } ? options.SearchProperties : null;
            this._unique = options?.Unique ?? false;
        }

        public IReadOnlyCollection<T> Collection { get; }

        public SearchItem<T>[] Search(string text)
        {
            string[] searchTokens = (text ?? throw new ArgumentNullException(nameof(text)))
                .Split(this._delimiter, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Take(this._maxFilterTokenEntries)
                .ToArray();
            List<SearchItem<T>> list = new();
            int maxScore = 0;
            foreach (var item in this.Collection.Where(this._preProcessCheck))
            {
                string[] dataTokens = this._searchProperties == null
                    ? new[] { item?.ToString() ?? string.Empty }
                    : Array.ConvertAll(this._searchProperties, property => TokenSearch<T>.GetItemValue(item, property)?.ToString() ?? string.Empty);
                int score = dataTokens.Select(token => this._searchAlgorithm(token, searchTokens)).Sum();
                if (score > 0)
                {
                    maxScore = Math.Max(score, maxScore);
                    list.Add(new(item) { Score = score });
                }
            }
            SearchItem<T>[] array = this._postProcessAlgorithm(list, maxScore, this._threshold, this._unique, this._searchProperties).ToArray();
            Array.Sort(array, this._sortAlgorithm);
            return array;
        }

        private static Func<T, string, object?> CreateGetItemValue()
        {
            ParameterExpression itemParameter = Expression.Parameter(typeof(T), nameof(itemParameter));
            ParameterExpression nameParameter = Expression.Parameter(typeof(string), nameof(nameParameter));
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

        private static int DefaultSearchAlgorithm(string haystack, IEnumerable<string> needles)
        {
            return needles.Aggregate(0, (score, needle) =>
            {
                int index = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                {
                    return score;
                }
                if (needle.Length >= 2)
                {
                    if (haystack == needle)
                    {
                        return score + 6;
                    }
                    if (index == 0)
                    {
                        return score + 2;
                    }
                }
                return score + 1;
            });
        }

        private static int DefaultSortAlgorithm(SearchItem<T> left, SearchItem<T> right) => left.Score.CompareTo(right.Score);
    }
}