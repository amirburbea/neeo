using System;
using System.Collections.Generic;

namespace Neeo.Api.Utilities
{
    internal static class CollectionMethods
    {
        /// <summary>
		/// Performs a binary search on a <paramref name="collection"/> for an item which the result of the specified <paramref name="projection"/>
		/// is equivalent to the <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">The type of items in the collection.</typeparam>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		/// <param name="collection">The collection.</param>
		/// <param name="value">The value.</param>
		/// <param name="projection">The projection.</param>
		/// <param name="valueComparer">The value comparer.</param>
        /// <returns>
        /// The index of the specified value in the specified array, if value is found; otherwise, the bitwise complement of the insertion index.
        /// </returns>
		public static int BinarySearchByValue<T, TValue>(this IReadOnlyList<T> collection, TValue value, Func<T, TValue> projection, IComparer<TValue>? valueComparer = null)
        {
            if (valueComparer == null)
            {
                valueComparer = Comparer<TValue>.Default;
            }
            int startIndex = 0;
            int endIndex = collection.Count - 1;
            while (endIndex >= startIndex)
            {
                int midPoint = startIndex + (endIndex - startIndex) / 2;
                switch (Math.Sign(valueComparer.Compare(projection(collection[midPoint]), value)))
                {
                    case -1:
                        startIndex = midPoint + 1;
                        break;
                    case 0:
                        return midPoint;
                    case 1:
                        endIndex = midPoint - 1;
                        break;
                }
            }
            return ~startIndex;
        }
    }
}