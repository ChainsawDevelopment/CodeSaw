using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Tests
{
    public class Dictionary2d<TKey1,TKey2,TValue>
    {
        private readonly List<Tuple<TKey1, TKey2, TValue>> _items;

        private readonly EqualityComparer<TKey1> _key1Comparer;
        private readonly EqualityComparer<TKey2> _key2Comparer;

        public Dictionary2d()
        {
            _key1Comparer = EqualityComparer<TKey1>.Default;
            _key2Comparer = EqualityComparer<TKey2>.Default;

            _items = new List<Tuple<TKey1, TKey2, TValue>>();
        }

        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
        {
            foreach (var (k1, k2, v) in _items)
            {
                if (_key1Comparer.Equals(k1, key1) && _key2Comparer.Equals(k2, key2))
                {
                    value = v;
                    return true;
                }
            }

            value = default;

            return false;
        }

        public TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                if (TryGetValue(key1, key2, out var value))
                {
                    return value;
                }
                throw new IndexOutOfRangeException($"Key: {key1} {key2} not found");
            }

            set
            {
                if (TryGetIndex(key1, key2, out var i))
                {
                    _items[i] = Tuple.Create(key1, key2, value);
                }
                else
                {
                    _items.Add(Tuple.Create(key1, key2, value));
                }
            }
        }

        private bool TryGetIndex(TKey1 key1, TKey2 key2, out int index)
        {
            foreach (var (i, (k1, k2, _)) in _items.AsIndexed())
            {
                if (_key1Comparer.Equals(k1, key1) && _key2Comparer.Equals(k2, key2))
                {
                    index = i;
                    return true;
                }
            }

            index = default;

            return false;
        }

        public TValue GetValueOrDefault(TKey1 key1, TKey2 key2, TValue defaultValue = default)
        {
            if (TryGetValue(key1, key2, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public Dictionary<TKey2, TValue> ForKey1(TKey1 key1)
        {
            return _items.Where(x => _key1Comparer.Equals(key1, x.Item1)).ToDictionary(x => x.Item2, x => x.Item3);
        }

        public Dictionary<TKey1, TValue> ForKey2(TKey2 key2)
        {
            return _items.Where(x => _key2Comparer.Equals(key2, x.Item2)).ToDictionary(x => x.Item1, x => x.Item3);
        }

        public bool Contains(TKey1 key1, TKey2 key2)
        {
            return TryGetValue(key1, key2, out var _);
        }
    }
}