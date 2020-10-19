// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace BSOA.Test
{
    public static class CollectionReadVerifier
    {
        public static void VerifySame<T>(IEnumerable<T> expected, IEnumerable<T> actual, bool quick = false)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            if (actual is IList<T> && expected is IList<T>)
            {
                VerifyList((IList<T>)expected, (IList<T>)actual, quick);
            }
            else if (actual is IReadOnlyList<T> && expected is IReadOnlyList<T>)
            {
                VerifyReadOnlyList((IReadOnlyList<T>)expected, (IReadOnlyList<T>)actual, quick);
            }
            else if (actual is ICollection<T> && expected is ICollection<T>)
            {
                VerifyCollection((ICollection<T>)expected, (ICollection<T>)actual);
            }
            else if (actual is IReadOnlyCollection<T> && expected is IReadOnlyCollection<T>)
            {
                VerifyReadOnlyCollection((IReadOnlyCollection<T>)expected, (IReadOnlyCollection<T>)actual);
            }
            else
            {
                VerifyEnumerable(expected, actual);
            }
        }

        public static void VerifyList<T>(IList<T> expected, IList<T> actual, bool quick = false)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            // Verify Counts match
            Assert.Equal(expected.Count, actual.Count);

            // Verify indexers work and return the same result
            for (int i = 0; i < actual.Count; ++i)
            {
                Assert.Equal(expected[i], actual[i]);
            }

            if (!quick)
            {
                VerifyCollection<T>(expected, actual);
            }
        }

        public static void VerifyReadOnlyList<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, bool quick = false)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            // Verify Counts match
            Assert.Equal(expected.Count, actual.Count);

            // Verify indexers work and return the same result
            for (int i = 0; i < actual.Count; ++i)
            {
                Assert.Equal(expected[i], actual[i]);
            }

            if (!quick)
            {
                VerifyReadOnlyCollection<T>(expected, actual);
            }
        }

        public static void VerifyCollection<T>(ICollection<T> expected, ICollection<T> actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.Count, actual.Count);

            VerifyEnumerable<T>(expected, actual);
        }

        public static void VerifyReadOnlyCollection<T>(IReadOnlyCollection<T> expected, IReadOnlyCollection<T> actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.Count, actual.Count);

            VerifyEnumerable<T>(expected, actual);
        }

        public static void VerifyEnumerable<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            // Verify typed enumerator (MoveNext, Current, Reset)
            using (IEnumerator<T> eTyped = expected.GetEnumerator())
            using (IEnumerator<T> aTyped = actual.GetEnumerator())
            {
                while (eTyped.MoveNext())
                {
                    Assert.True(aTyped.MoveNext());
                    Assert.Equal(eTyped.Current, aTyped.Current);
                }

                Assert.False(aTyped.MoveNext());

                eTyped.Reset();
                aTyped.Reset();
                while (eTyped.MoveNext())
                {
                    Assert.True(aTyped.MoveNext());
                    Assert.Equal(eTyped.Current, aTyped.Current);
                }

                Assert.False(aTyped.MoveNext());

                // Double dispose check
                aTyped.Dispose();
            }

            // Verify untyped enumerator
            IEnumerator eUntyped = ((IEnumerable)expected).GetEnumerator();
            IEnumerator aUntyped = ((IEnumerable)actual).GetEnumerator();

            while (eUntyped.MoveNext())
            {
                Assert.True(aUntyped.MoveNext());
                Assert.Equal(eUntyped.Current, aUntyped.Current);
            }

            Assert.False(aUntyped.MoveNext());

            eUntyped.Reset();
            aUntyped.Reset();
            while (eUntyped.MoveNext())
            {
                Assert.True(aUntyped.MoveNext());
                Assert.Equal(eUntyped.Current, aUntyped.Current);
            }

            Assert.False(aUntyped.MoveNext());
        }

        public static void VerifyEqualityMembers<T>(T value, T equivalent) where T : class
        {
            Assert.True(value.Equals(value));
            Assert.True(value.Equals(equivalent));

            Assert.Equal(value.GetHashCode(), value.GetHashCode());
            Assert.Equal(value.GetHashCode(), equivalent.GetHashCode());
        }
    }
}
