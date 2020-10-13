// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using BSOA.Column;
using BSOA.Model;

using Xunit;

namespace BSOA.Test
{
    public class ListColumnTests
    {
        // Note: ListColumn<int> is easiest to test, but don't use ListColumn for numeric types.
        //  NumberListColumn is more efficient for numeric types. Use ListColumn for Lists of more complex types.

        [Fact]
        public void ListColumn_Basics()
        {
            ListColumn<int> column = new ListColumn<int>(new NumberColumn<int>(-1), Nullability.DefaultToEmpty);
            column[0].Add(1);
            column[0].Add(2);
            column[0].Add(3);

            // Test the outer column (non-nullable)
            Column.Basics(() => new ListColumn<int>(new NumberColumn<int>(-1), Nullability.NullsDisallowed), ColumnList<int>.Empty, column[0], (index) =>
            {
                IList<int> other = column[column.Count];
                other.Add(index);
                other.Add(index + 1);
                other.Add(index + 2);
                return other;
            });

            // Test the outer column (nullable)
            Column.Basics(() => new ListColumn<int>(new NumberColumn<int>(-1), Nullability.DefaultToNull), null, column[0], (index) =>
            {
                IList<int> other = column[column.Count];
                other.Add(index);
                other.Add(index + 1);
                other.Add(index + 2);
                return other;
            });

            // Test the outer column (nullable, default not null)
            Column.Basics(() => new ListColumn<int>(new NumberColumn<int>(-1), Nullability.DefaultToEmpty), ColumnList<int>.Empty, null, (index) =>
            {
                IList<int> other = column[column.Count];
                other.Add(index);
                other.Add(index + 1);
                other.Add(index + 2);
                return other;
            });

            // Test the ColumnList item members
            CollectionChangeVerifier.VerifyList(column[1], (index) => index % 20);

            // ColumnList.Empty handling
            ColumnList<int> empty = ColumnList<int>.Empty;
            Assert.Empty(empty);
            Assert.True(empty.Count == 0);
            Assert.True(empty.Contains(7) == false);
            Assert.Equal(-1, empty.IndexOf(3));

            Assert.True(empty == ColumnList<int>.Empty);
            Assert.False(empty != ColumnList<int>.Empty);

            // ColumnList.GetHashCode and Equals w/nulls
            ListColumn<string> stringColumn = new ListColumn<string>(new StringColumn(), Nullability.DefaultToEmpty);
            
            ColumnList<string> first = (ColumnList<string>)stringColumn[0];
            first.Add("One");
            first.Add(null);
            first.Add("Two");

            ColumnList<string> second = (ColumnList<string>)stringColumn[1];
            second.Add("One");
            second.Add(null);
            second.Add("Two");

            Assert.True(second == first);

            second[1] = "NotNull";
            Assert.NotEqual(second.GetHashCode(), first.GetHashCode());
            Assert.False(second == first);

            // Set to null, add to cached copy
            stringColumn[1] = null;
            Assert.True(second.Count == 0);
            second.Add("One");
            Assert.Single(second);
        }
    }
}
