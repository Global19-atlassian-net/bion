﻿using BSOA.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BSOA.Column
{
    /// <summary>
    ///  DistinctColumn compresses data by storing each distinct value of the column
    ///  only once. It reverts to storing values individually if there are too many
    ///  distinct values.
    /// </summary>
    /// <typeparam name="T">Type of values in column</typeparam>
    public class DistinctColumn<T> : IColumn<T>
    {
        private T _defaultValue;
        private Dictionary<T, byte> _distinct;
        private IColumn<T> _values;
        private NumberColumn<byte> _indices;
        
        public DistinctColumn(IColumn<T> values, T defaultValue)
        {
            _defaultValue = defaultValue;
            _values = values;
            Clear();
        }

        public bool IsMappingValues => (_distinct != null);
        public int DistinctCount => (IsMappingValues ? _distinct.Count : -1);
        public int Count => (IsMappingValues ? _indices.Count : _values.Count);
        public bool Empty => (Count == 0);

        public T this[int index]
        {
            get => (IsMappingValues ? _values[_indices[index]] : _values[index]);

            set
            {
                if (IsMappingValues && TryGetDistinctIndex(value, out byte valueIndex))
                {
                    _indices[index] = valueIndex;
                }
                else
                {
                    _values[index] = value;
                }
            }
        }

        private bool TryGetDistinctIndex(T value, out byte index)
        {
            index = 0;

            if (_distinct.TryGetValue(value, out index))
            {
                // Existing value - return current index
                return true;
            }
            else if (_distinct.Count <= 256)
            {
                // New value, count still ok - add and return new index
                index = (byte)(_distinct.Count);
                _distinct[value] = index;
                _values[index] = value;
                return true;
            }
            else
            {
                // Too many values - convert to per-value
                List<T> distinctValues = _values.ToList();
                _values.Clear();
                for (int i = 0; i < _indices.Count; ++i)
                {
                    _values[i] = distinctValues[_indices[i]];
                }

                _indices = null;
                _distinct = null;
                return false;
            }
        }

        public void Clear()
        {
            // Indices empty but non-null
            _indices = new NumberColumn<byte>(0);

            // One distinct value; the default
            _distinct = new Dictionary<T, byte>();
            _distinct[_defaultValue] = 0;
            _values.Clear();
            _values[0] = _defaultValue;
        }

        public void RemoveFromEnd(int count)
        {
            if (IsMappingValues)
            {
                _indices.RemoveFromEnd(count);
            }
            else
            {
                _values.RemoveFromEnd(count);
            }
        }

        public void Swap(int index1, int index2)
        {
            T item = this[index1];
            this[index1] = this[index2];
            this[index2] = item;
        }

        public void Trim()
        {
            if (IsMappingValues)
            {
                // Find all distinct indices used
                HashSet<byte> unusedValues = new HashSet<byte>();
                unusedValues.UnionWith(_distinct.Values);

                // Remove default, and all values used in indices
                unusedValues.Remove(0);
                Remapper.ExceptWith(unusedValues, _indices.Slice);

                // If there are unused values, ...
                if (unusedValues.Count > 0)
                {
                    byte[] remapped = unusedValues.ToArray();
                    byte remapFrom = (byte)(_distinct.Count - remapped.Length);

                    // Swap the *values* to the end of the values array
                    for (int i = 0; i < remapped.Length; ++i)
                    {
                        _values.Swap(remapFrom + i, remapped[i]);
                    }

                    // Swap indices using those values to use the new ones
                    Remapper.RemapAbove(_indices.Slice, remapFrom, remapped);

                    // Remove the unused values that are now at the end of the array
                    _values.RemoveFromEnd(remapped.Length);

                    // Rebuild Distinct Dictionary
                    RebuildDistinctDictionary();
                }
            }

            _values.Trim();
            _indices?.Trim();
        }

        private static Dictionary<string, Setter<DistinctColumn<T>>> setters = new Dictionary<string, Setter<DistinctColumn<T>>>()
        {
            [Indices] = (r, me) => me._indices.Read(r),
            [Values] = (r, me) => me._values.Read(r)
        };

        public void Read(ITreeReader reader)
        {
            reader.ReadObject(this, setters);

            if (_indices.Count > 0)
            {
                // If column has indices, it's mapped; reconstruct distinct dictionary
                RebuildDistinctDictionary();
            }
            else if (_values.Count > 1)
            {
                // If it has no indices and more than one value (the default is always added), it is non-mapped
                _indices = null;
                _distinct = null;
            }
        }

        private void RebuildDistinctDictionary()
        {
            _distinct.Clear();
            _distinct[_defaultValue] = 0;

            for (int i = 0; i < _values.Count; ++i)
            {
                _distinct[_values[i]] = (byte)i;
            }
        }

        private const string Indices = nameof(Indices);
        private const string Values = nameof(Values);

        public void Write(ITreeWriter writer)
        {
            writer.WriteStartObject();
            writer.Write(Indices, _indices);
            writer.Write(Values, _values);
            writer.WriteEndObject();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ListEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ListEnumerator<T>(this);
        }
    }
}
