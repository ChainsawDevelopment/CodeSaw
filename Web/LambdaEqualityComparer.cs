using System;
using System.Collections.Generic;

namespace Web
{
    public class LambdaEqualityComparer<T, TValue> : IEqualityComparer<T>
    {
        private static readonly EqualityComparer<TValue> InnerComparer = EqualityComparer<TValue>.Default;

        private readonly Func<T, TValue> _selector;

        public LambdaEqualityComparer(Func<T, TValue> selector)
        {
            _selector = selector;
        }

        public bool Equals(T x, T y) => InnerComparer.Equals(_selector(x), _selector(y));

        public int GetHashCode(T obj) => InnerComparer.GetHashCode(_selector(obj));
    }
}