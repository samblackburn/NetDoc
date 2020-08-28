using System.Collections.Generic;

namespace NetDoc.Utils
{
    internal static class Linq
    {
        internal static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using var e = source.GetEnumerator();
            if (!e.MoveNext()) yield break;
            for (var value = e.Current; e.MoveNext(); value = e.Current)
            {
                yield return value;
            }
        }
    }
}
