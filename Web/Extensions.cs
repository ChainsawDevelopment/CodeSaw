using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web
{
    public static class Extensions
    {
        public static async Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> source)
        {
            return await Task.WhenAll(source);
        }

        
        public static int IndexOf<T>(this IEnumerable<T> @this, T valueToFind, IEqualityComparer<T> comparer)
        {
            int index = -1;

            foreach (var item in @this)
            {
                index++;

                if (comparer.Equals(item, valueToFind))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}