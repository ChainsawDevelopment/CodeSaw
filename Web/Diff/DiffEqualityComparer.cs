using System.Collections.Generic;

namespace Web.Diff
{
    public class DiffEqualityComparer : IEqualityComparer<DiffMatchPatch.Diff>
    {
        private static readonly char[] TrimChars = {'\n', ' '};

        public bool Equals(DiffMatchPatch.Diff x, DiffMatchPatch.Diff y)
        {
            if (x.Equals(y))
            {
                return true;
            }

            if (!x.Operation.Equals(y.Operation))
            {
                return false;
            }

            return x.Text.Trim(TrimChars) == y.Text.Trim(TrimChars);
        }

        public int GetHashCode(DiffMatchPatch.Diff obj)
        {
            return obj.Text.Trim(TrimChars).GetHashCode();
        }
    }
}