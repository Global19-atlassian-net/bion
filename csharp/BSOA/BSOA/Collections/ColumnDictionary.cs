// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

using BSOA.Column;
using BSOA.Extensions;

namespace BSOA.Collections
{
    public class ColumnDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IEquatable<TKey>
    {
        private readonly DictionaryColumn<TKey, TValue> _column;
        private readonly int _rowIndex;
        private NumberList<int> Pairs => _column?._pairs[_rowIndex] ?? NumberList<int>.Empty;
        private TValue Value(int pairIndex) => _column._values[_column._pairs[_rowIndex][pairIndex]];

        public static ColumnDictionary<TKey, TValue> Empty = new ColumnDictionary<TKey, TValue>(null, 0);

        protected ColumnDictionary(DictionaryColumn<TKey, TValue> column, int index)
        {
            _column = column;
            _rowIndex = index;
        }

        public static ColumnDictionary<TKey, TValue> Get(DictionaryColumn<TKey, TValue> column, int index)
        {
            if (index < 0) { throw new IndexOutOfRangeException(nameof(index)); }
            return (column?._pairs?[index] == null ? null : new ColumnDictionary<TKey, TValue>(column, index));
        }

        public static void Set(DictionaryColumn<TKey, TValue> column, int index, IDictionary<TKey, TValue> value)
        {
            if (index < 0) { throw new IndexOutOfRangeException(nameof(index)); }

            if (value == null)
            {
                column._pairs[index] = null;
            }
            else
            {
                // Setting List to empty 'coerces' list creation in correct column
                if (column._pairs[index] == null) { column._pairs[index] = NumberList<int>.Empty; }
                new ColumnDictionary<TKey, TValue>(column, index).SetTo(value);
            }
        }

        public bool IsReadOnly => false;
        public int Count => Pairs.Count;

        public TValue this[TKey key]
        {
            get
            {
                if (this.TryGetValue(key, out TValue value))
                {
                    return value;
                }

                throw new KeyNotFoundException($"Key {key} not found in Dictionary; Keys: {string.Join(", ", Keys)}");
            }

            set
            {
                int pairIndex = InternalIndexOfKey(key);
                if (pairIndex == -1)
                {
                    AddInternal(key, value);
                }
                else
                {
                    _column._values[Pairs[pairIndex]] = value;
                }
            }
        }

        public ICollection<TKey> Keys => new IndirectCollection<TKey>(_column._keys, Pairs.Slice);
        public ICollection<TValue> Values => new IndirectCollection<TValue>(_column._values, Pairs.Slice);

        public void SetTo(IDictionary<TKey, TValue> other)
        {
            if (other is ColumnDictionary<TKey, TValue>)
            {
                // Avoid Clear() on SetTo(self)
                ColumnDictionary<TKey, TValue> otherDictionary = (ColumnDictionary<TKey, TValue>)other;
                if (object.ReferenceEquals(this._column, otherDictionary._column) && this._rowIndex == otherDictionary._rowIndex)
                {
                    return;
                }
            }

            Clear();

            if (other != null)
            {
                foreach (KeyValuePair<TKey, TValue> pair in other)
                {
                    this.AddInternal(pair.Key, pair.Value);
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (this.ContainsKey(key)) { throw new ArgumentException(nameof(key)); }
            AddInternal(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        private void AddInternal(TKey key, TValue value)
        {
            int newPairIndex = _column._values.Count;
            _column._keys[newPairIndex] = key;
            _column._values[newPairIndex] = value;
            Pairs.Add(newPairIndex);
        }

        public void Clear()
        {
            NumberList<int> pairs = Pairs;

            // Clear Keys and Values to conserve space
            foreach (int pairIndex in pairs)
            {
                _column._keys[pairIndex] = default;
                _column._values[pairIndex] = default;
            }

            // Clear pairs to empty the collection
            pairs.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return InternalIndexOfKey(key) != -1;
        }

        public bool Remove(TKey key)
        {
            int pairIndex = InternalIndexOfKey(key);

            if (pairIndex != -1)
            {
                Pairs.RemoveAt(pairIndex);
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int pairIndex = InternalIndexOfKey(item.Key);

            if (pairIndex != -1 && object.Equals(Value(pairIndex), item.Value))
            {
                Pairs.RemoveAt(pairIndex);
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            int pairIndex = InternalIndexOfKey(key);
            if (pairIndex == -1) { return false; }

            value = Value(pairIndex);
            return true;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetValue(item.Key, out TValue foundValue))
            {
                return object.Equals(foundValue, item.Value);
            }

            return false;
        }

        private int InternalIndexOfKey(TKey key)
        {
            int pairIndex = 0;

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                if (pair.Key.Equals(key)) { return pairIndex; }
                pairIndex++;
            }

            return -1;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            EnumerableExtensions.CopyTo(this, this.Count, array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new DictionaryEnumerator<TKey, TValue>(_column, Pairs.Slice);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DictionaryEnumerator<TKey, TValue>(_column, Pairs.Slice);
        }

        public override int GetHashCode()
        {
            int hashCode = 17;

            // Combine Keys with XOR to ensure GetHashCode is order-independent
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                hashCode ^= pair.Key?.GetHashCode() ?? 0;
                hashCode ^= 31 * (pair.Value?.GetHashCode() ?? 0);
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            IDictionary<TKey, TValue> other = obj as IDictionary<TKey, TValue>;
            if (other == null) { return false; }

            // Verify Dictionary counts match
            if (this.Count != other.Count) { return false; }

            // Try to compare keys in order (faster than lookup for each key)
            using (IEnumerator<KeyValuePair<TKey, TValue>> thisEnumerator = GetEnumerator())
            using (IEnumerator<KeyValuePair<TKey, TValue>> otherEnumerator = other.GetEnumerator())
            {
                int countCompared = 0;

                while (thisEnumerator.MoveNext())
                {
                    // If other ran out of items first, definite non-match
                    if (!otherEnumerator.MoveNext()) { return false; }

                    KeyValuePair<TKey, TValue> thisPair = thisEnumerator.Current;
                    KeyValuePair<TKey, TValue> otherPair = otherEnumerator.Current;

                    // If keys don't match, they aren't in the same order; must fall back
                    if (!thisPair.Key.Equals(otherPair.Key)) { break; }

                    // If keys matched but values different, definite non-match
                    if (!object.Equals(thisPair.Value, otherPair.Value)) { return false; }

                    countCompared++;
                }

                // If we got fully through the lists, they match
                if (countCompared == this.Count)
                {
                    return true;
                }
            }

            // Otherwise, retrieve values by key (any order) and compare values)
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                if (!other.Contains(pair)) { return false; }
            }

            return true;
        }

        public static bool operator ==(ColumnDictionary<TKey, TValue> left, ColumnDictionary<TKey, TValue> right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ColumnDictionary<TKey, TValue> left, ColumnDictionary<TKey, TValue> right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return !object.ReferenceEquals(right, null);
            }

            return !left.Equals(right);
        }
    }
}
