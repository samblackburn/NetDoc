using System;
using System.Collections.Generic;
using System.IO;

namespace NetDoc
{
    internal class FileNameOnlyComparer : IEqualityComparer<string?>
    {
        public bool Equals(string? left, string? right)
        {
            if (left == null) return right == null;
            return Path.GetFileName(left).Equals(Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string? obj)
        {
            return Path.GetFileName(obj)?.GetHashCode() ?? -1;
        }
    }
}