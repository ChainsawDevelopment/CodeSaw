using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web
{
    public class LambdaComparer<T, TValue> : IComparer<T>
    {
        private readonly Func<T, TValue> _selector;

        public LambdaComparer(Func<T, TValue> selector)
        {
            _selector = selector;
        }

        public int Compare(T x, T y)
        {
            return Comparer<TValue>.Default.Compare(_selector(x), _selector(y));
        }
    }
}
