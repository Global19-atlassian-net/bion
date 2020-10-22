// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using BSOA.Column;
using BSOA.Model;

using Xunit;

namespace BSOA.Test
{
    public class ColumnFactoryTests
    {
        [Fact]
        public void ColumnFactory_Build()
        {
            AssertBuild<string>(null);
            AssertBuild<Uri>(null);
            AssertBuild<DateTime>(DateTime.UtcNow);
            AssertBuild<bool>(true);

            AssertBuild<byte>((byte)1);
            AssertBuild<sbyte>((sbyte)1);
            AssertBuild<ushort>((ushort)1);
            AssertBuild<short>((short)1);
            AssertBuild<uint>((uint)1);
            AssertBuild<int>((int)1);
            AssertBuild<ulong>((ulong)1);
            AssertBuild<long>((long)1);
            AssertBuild<float>((float)1);
            AssertBuild<double>((double)1);
            AssertBuild<char>((char)1);

            AssertBuild<IList<byte>>(null);
            AssertBuild<IList<sbyte>>(null);
            AssertBuild<IList<ushort>>(null);
            AssertBuild<IList<short>>(null);
            AssertBuild<IList<uint>>(null);
            AssertBuild<IList<int>>(null);
            AssertBuild<IList<ulong>>(null);
            AssertBuild<IList<long>>(null);
            AssertBuild<IList<float>>(null);
            AssertBuild<IList<double>>(null);
            AssertBuild<IList<char>>(null);

            IColumn<IList<string>> listColumn = (IColumn<IList<string>>)ColumnFactory.Build(typeof(IList<string>), null);
            Assert.NotNull(listColumn);

            IColumn<IDictionary<string, string>> dictionaryColumn = (IColumn<IDictionary<string, string>>)(ColumnFactory.Build(typeof(IDictionary<string, string>), null));
            Assert.NotNull(dictionaryColumn);

            // Verify collections are null by default if null passed as default
            Assert.Null(listColumn[0]);
            Assert.Null(dictionaryColumn[0]);

            // Verify collections not null by default if non-null passed as default
            listColumn = (IColumn<IList<string>>)ColumnFactory.Build(typeof(IList<string>), new object());
            dictionaryColumn = (IColumn<IDictionary<string, string>>)(ColumnFactory.Build(typeof(IDictionary<string, string>), new object()));
            Assert.NotNull(listColumn[0]);
            Assert.NotNull(dictionaryColumn[0]);

            if (!Debugger.IsAttached)
            {
                Assert.Throws<NotImplementedException>(() => ColumnFactory.Build(typeof(Decimal)));
                Assert.Throws<NotImplementedException>(() => ColumnFactory.Build(typeof(ISet<int>)));
                Assert.Throws<NotSupportedException>(() => ColumnFactory.Build(typeof(DayOfWeek), DayOfWeek.Sunday));
            }
        }

        private void AssertBuild<T>(object defaultValue)
        {
            IColumn column = ColumnFactory.BuildTyped<T>((T)defaultValue);
            Assert.NotNull(column);
            Assert.True(column is IColumn<T>);

            if (defaultValue != null)
            {
                column = ColumnFactory.Build(typeof(T), null);
                Assert.NotNull(column);
                Assert.True(column is IColumn<T>);

                column = ColumnFactory.Build(typeof(T));
                Assert.NotNull(column);
                Assert.True(column is IColumn<T>);
            }
        }
    }
}
