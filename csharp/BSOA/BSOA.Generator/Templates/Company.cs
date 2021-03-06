// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using BSOA.Collections;
using BSOA.IO;
using BSOA.Model;

namespace BSOA.Generator.Templates
{
    /// <summary>
    ///  BSOA GENERATED Root Entity for 'Company'
    /// </summary>
    public partial class Company : IRow<Company>, IEquatable<Company>
    {
        private CompanyTable _table;
        private int _index;

        internal CompanyDatabase Database => _table.Database;
        public IDatabase DB => _table.Database;

        public Company() : this(new CompanyDatabase().Company)
        { }

        public Company(Company other) : this(new CompanyDatabase().Company)
        {
            CopyFrom(other);
        }

        internal Company(CompanyTable table) : this(table, table.Add()._index)
        {
            Init();
        }

        internal Company(CompanyTable table, int index)
        {
            this._table = table;
            this._index = index;
        }

        partial void Init();

        // <ColumnList>
        //   <SimpleColumn>
        public long Id
        {
            get { _table.EnsureCurrent(this); return _table.Id[_index]; }
            set { _table.EnsureCurrent(this); _table.Id[_index] = value; }
        }

        //   </SimpleColumn>
        //   <EnumColumn>
        public SecurityPolicy JoinPolicy
        {
            get { _table.EnsureCurrent(this); return (SecurityPolicy)_table.JoinPolicy[_index]; }
            set { _table.EnsureCurrent(this); _table.JoinPolicy[_index] = (byte)value; }
        }

        //   </EnumColumn>
        //   <RefColumn>
        public Employee Owner
        {
            get { _table.EnsureCurrent(this); return _table.Database.Employee.Get(_table.Owner[_index]); }
            set { _table.EnsureCurrent(this); _table.Owner[_index] = _table.Database.Employee.LocalIndex(value); }
        }

        //   </RefColumn>
        //   <RefListColumn>
        public IList<Employee> Members
        {
            get { _table.EnsureCurrent(this); return TypedList<Employee>.Get(_table.Database.Employee, _table.Members, _index); }
            set { _table.EnsureCurrent(this); TypedList<Employee>.Set(_table.Database.Employee, _table.Members, _index, value); }
        }

        //   </RefListColumn>
        // </ColumnList>

        #region IEquatable<Company>
        public bool Equals(Company other)
        {
            if (other == null) { return false; }

            // <EqualsList>
            //  <Equals>
            if (!object.Equals(this.Id, other.Id)) { return false; }
            //  </Equals>
            if (!object.Equals(this.JoinPolicy, other.JoinPolicy)) { return false; }
            if (!object.Equals(this.Owner, other.Owner)) { return false; }
            if (!object.Equals(this.Members, other.Members)) { return false; }
            // </EqualsList>

            return true;
        }
        #endregion

        #region Object overrides
        public override int GetHashCode()
        {
            int result = 17;

            unchecked
            {
                // <GetHashCodeList>
                //  <GetHashCode>
                if (Id != default(long))
                {
                    result = (result * 31) + Id.GetHashCode();
                }

                //  </GetHashCode>
                if (JoinPolicy != default(SecurityPolicy))
                {
                    result = (result * 31) + JoinPolicy.GetHashCode();
                }

                if (Owner != default(Employee))
                {
                    result = (result * 31) + Owner.GetHashCode();
                }

                if (Members != default(IList<Employee>))
                {
                    result = (result * 31) + Members.GetHashCode();
                }
                // </GetHashCodeList>
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Company);
        }

        public static bool operator ==(Company left, Company right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Company left, Company right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return !object.ReferenceEquals(right, null);
            }

            return !left.Equals(right);
        }
        #endregion

        #region IRow
        ITable IRow.Table => _table;
        int IRow.Index => _index;

        void IRow.Remap(ITable table, int index)
        {
            _table = (CompanyTable)table;
            _index = index;
        }

        public void CopyFrom(Company other)
        {
            // <OtherAssignmentList>
            //  <OtherAssignment>
            Id = other.Id;
            //  </OtherAssignment>
            JoinPolicy = other.JoinPolicy;
            //  <RefOtherAssignment>
            Owner = Employee.Copy(_table.Database, other.Owner);
            //  </RefOtherAssignment>
            //  <RefListOtherAssignment>
            Members = other.Members?.Select((item) => Employee.Copy(_table.Database, item)).ToList();
            //  </RefListOtherAssignment>
            // </OtherAssignmentList>
        }
        #endregion

        #region Easy Serialization
        public void WriteBsoa(System.IO.Stream stream)
        {
            using (BinaryTreeWriter writer = new BinaryTreeWriter(stream))
            {
                DB.Write(writer);
            }
        }

        public void WriteBsoa(string filePath)
        {
            WriteBsoa(System.IO.File.Create(filePath));
        }

        public static Company ReadBsoa(System.IO.Stream stream)
        {
            using (BinaryTreeReader reader = new BinaryTreeReader(stream))
            {
                Company result = new Company();
                result.DB.Read(reader);
                return result;
            }
        }

        public static Company ReadBsoa(string filePath)
        {
            return ReadBsoa(System.IO.File.OpenRead(filePath));
        }

        public static TreeDiagnostics Diagnostics(string filePath)
        {
            return Diagnostics(System.IO.File.OpenRead(filePath));
        }

        public static TreeDiagnostics Diagnostics(System.IO.Stream stream)
        {
            using (BinaryTreeReader btr = new BinaryTreeReader(stream))
            using (TreeDiagnosticsReader reader = new TreeDiagnosticsReader(btr))
            {
                Company result = new Company();
                result.DB.Read(reader);
                return reader.Tree;
            }
        }
        #endregion
    }
}
