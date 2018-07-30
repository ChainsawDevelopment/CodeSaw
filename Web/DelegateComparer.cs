using System;
using System.Collections.Generic;

namespace Web
{
    public class DelegateComparer<T, TCompareBy> : IComparer<T>
    {
        private static readonly Comparer<TCompareBy> Comparer = Comparer<TCompareBy>.Default;

        private readonly Func<T, TCompareBy> _selector;

        public DelegateComparer(Func<T, TCompareBy> selector)
        {
            _selector = selector;
        }

        public int Compare(T x, T y) => Comparer.Compare(_selector(x), _selector(y));
    }

    public class DelegateComparer
    {
        public static DelegateComparer<T, TCompareBy> For<T, TCompareBy>(Func<T, TCompareBy> selector) => new DelegateComparer<T, TCompareBy>(selector);
    }
}