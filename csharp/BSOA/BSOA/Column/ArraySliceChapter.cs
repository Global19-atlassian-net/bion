// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using BSOA.Collections;
using BSOA.Extensions;
using BSOA.IO;

namespace BSOA.Column
{
    /// <summary>
    ///  Contains one 'Chapter' of values for an ArraySliceColumn.
    /// </summary>
    /// <remarks>
    ///  Values with length &lt; 2,048 are stored back-to-back together in a SmallValueArray.
    ///  Longer values are stored in a LargeValueArray and loaded individually into a Dictionary.
    ///  Changed values are put in the LargeValueDictionary, keeping logic simple and the SmallValueArray unchanged.
    ///  
    ///  With a 2 KB short value limit,
    ///   a 32 row page is under 64 KB, and page-relative row positions can be a ushort.
    ///   a 32,768 row chapter is under 64 MB, and chapter-relative positions fit in an int.
    ///   
    ///  The overall column uses longs for large values and chapter positions, allowing the column to be over 4 GB.
    /// </remarks>
    /// <typeparam name="T">Type of each part of Values (if each value is a string, this type is char)</typeparam>
    internal class ArraySliceChapter<T> : ITreeSerializable where T : unmanaged, IEquatable<T>
    {
        private static int[] PageStartDefault = new int[1] { 0 };

        public const int ChapterRowCount = 32768;
        public const int PageRowCount = 32;
        public const int MaximumSmallValueLength = 2047;

        private int[] _pageStartInChapter;      // Position of each Page Start relative to Chapter
        private ushort[] _valueEndInPage;       // Position after end of each Value relative to Page

        private T[] _smallValueArray;
        private Dictionary<int, ArraySlice<T>> _largeValueDictionary;

        private bool _requiresTrim;
        private int _lastNonEmptyIndex;

        public int Count { get; set; }

        public ArraySliceChapter()
        {
            Count = 0;
        }

        private int EndPosition(int index)
        {
            return _pageStartInChapter[index / PageRowCount] + _valueEndInPage[index];
        }

        private int StartPosition(int index)
        {
            return (index == 0 ? 0 : EndPosition(index - 1));
        }

        public ArraySlice<T> this[int index]
        {
            get
            {
                // VariableLengthColumn can't pass out of range values
                // if (index < 0 || index >= ChapterRowCount) { throw new ArgumentOutOfRangeException("index"); }

                if (_largeValueDictionary != null && _largeValueDictionary.TryGetValue(index, out ArraySlice<T> result)) { return result; }

                if (index < _valueEndInPage?.Length)
                {
                    int position = StartPosition(index);
                    int length = EndPosition(index) - position;
                    return new ArraySlice<T>(_smallValueArray, position, length);
                }
                else
                {
                    return ArraySlice<T>.Empty;
                }
            }

            set
            {
                if (index >= Count) { Count = index + 1; }
                _largeValueDictionary = _largeValueDictionary ?? new Dictionary<int, ArraySlice<T>>();
                _largeValueDictionary[index] = value;
                _requiresTrim |= (value.Count <= MaximumSmallValueLength);
            }
        }

        public void ForEach(Action<ArraySlice<T>> action)
        {
            // Ensure updated values re-merged first
            Trim();

            // Run over all consolidated small values
            if (_smallValueArray != null)
            {
                action(new ArraySlice<T>(_smallValueArray));
            }

            // Run over each large value array
            if (_largeValueDictionary != null)
            {
                foreach (ArraySlice<T> largeValueArray in _largeValueDictionary.Values)
                {
                    action(largeValueArray);
                }
            }
        }

        public void Clear()
        {
            _pageStartInChapter = PageStartDefault;
            _valueEndInPage = null;

            _smallValueArray = null;
            _largeValueDictionary = null;

            _lastNonEmptyIndex = -1;
            Count = 0;
        }

        public void Trim()
        {
            if (_requiresTrim == false) { return; }

            // Compute new size needed for SmallValueArray
            int totalSmallValueLength = _smallValueArray?.Length ?? 0;
            int newSmallValueLength = totalSmallValueLength;

            foreach (var pair in _largeValueDictionary)
            {
                int length = pair.Value.Count;
                if (length <= MaximumSmallValueLength)
                {
                    int index = pair.Key;
                    int oldLength = ((index < _valueEndInPage?.Length) ? EndPosition(index) - StartPosition(index) : 0);
                    newSmallValueLength += (length - oldLength);
                }
            }

            // Make new arrays
            T[] newSmallValueArray = new T[newSmallValueLength];
            int[] newPageStartInChapter = new int[(Count / PageRowCount) + 1];
            ushort[] newValueEndInPage = new ushort[Count];

            // Copy every small-enough value to the new array and remove from LargeValueDictionary
            int lastNonEmptyIndex = -1;
            int currentPageStart = 0;
            int nextIndex = 0;
            for (int i = 0; i < Count; ++i)
            {
                // Set new page start for each page
                if ((i % PageRowCount) == 0)
                {
                    currentPageStart = nextIndex;
                    newPageStartInChapter[i / PageRowCount] = currentPageStart;
                }

                // Get current value
                ArraySlice<T> value = this[i];

                // Copy the value to the new _smallValueArray, if it fits
                if (value.Count <= MaximumSmallValueLength)
                {
                    value.CopyTo(newSmallValueArray, nextIndex);
                    nextIndex += value.Count;
                    _largeValueDictionary.Remove(i);

                    if (value.Count > 0) { lastNonEmptyIndex = i; }
                }

                // Set new valueEnd
                newValueEndInPage[i] = (ushort)(nextIndex - currentPageStart);
            }

            if (_largeValueDictionary.Count == 0) { _largeValueDictionary = null; }
            _smallValueArray = newSmallValueArray;
            _pageStartInChapter = newPageStartInChapter;
            _valueEndInPage = newValueEndInPage;

            _requiresTrim = false;
            _lastNonEmptyIndex = lastNonEmptyIndex;
        }

        private static Dictionary<string, Setter<ArraySliceChapter<T>>> setters = new Dictionary<string, Setter<ArraySliceChapter<T>>>()
        {
            [Names.Count] = (r, me) => me.Count = r.ReadAsInt32(),
            [Names.PageStart] = (r, me) => me._pageStartInChapter = r.ReadBlockArray<int>(),
            [Names.ValueEnd] = (r, me) => me._valueEndInPage = r.ReadBlockArray<ushort>(),
            [Names.SmallValues] = (r, me) => me._smallValueArray = r.ReadBlockArray<T>(),
            [Names.LargeValues] = (r, me) => me._largeValueDictionary = r.ReadIntDictionary<ArraySlice<T>>()
        };

        public void Read(ITreeReader reader)
        {
            reader.ReadObject(this, setters);

            if (_valueEndInPage != null)
            {
                Count = _valueEndInPage.Length;
                _lastNonEmptyIndex = Count - 1;
            }
        }

        public void Write(ITreeWriter writer)
        {
            // Merge changed small values under cutoff into SmallValueArray
            Trim();

            writer.WriteStartObject();

            if (_smallValueArray?.Length > 0)
            {
                // If there are any non-empty values, write the text and end positions
                writer.WriteBlockArray(Names.ValueEnd, _valueEndInPage);
                writer.WriteBlockArray(Names.SmallValues, _smallValueArray);

                // If there is more than one page, write page starts
                int pages = (Count / PageRowCount) + 1;
                if (pages > 1)
                {
                    writer.WriteBlockArray(Names.PageStart, _pageStartInChapter);
                }
            }
            else if (Count > 0)
            {
                // If there is no text but a non-zero count, we must preserve the count
                writer.Write(Names.Count, Count);
            }

            // If there are any large values, write them
            if (_largeValueDictionary?.Count > 0)
            {
                writer.WritePropertyName(Names.LargeValues);
                writer.WriteDictionary(_largeValueDictionary);
            }

            writer.WriteEndObject();
        }
    }
}
