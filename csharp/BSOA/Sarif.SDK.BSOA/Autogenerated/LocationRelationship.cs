// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using BSOA.Model;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    ///  GENERATED: BSOA Entity for 'LocationRelationship'
    /// </summary>
    [GeneratedCode("BSOA.Generator", "0.5.0")]
    public partial class LocationRelationship : PropertyBagHolder, ISarifNode, IRow
    {
        private LocationRelationshipTable _table;
        private int _index;

        public LocationRelationship() : this(SarifLogDatabase.Current.LocationRelationship)
        { }

        public LocationRelationship(SarifLog root) : this(root.Database.LocationRelationship)
        { }

        internal LocationRelationship(LocationRelationshipTable table) : this(table, table.Count)
        {
            table.Add();
            Init();
        }

        internal LocationRelationship(LocationRelationshipTable table, int index)
        {
            this._table = table;
            this._index = index;
        }

        public LocationRelationship(
            int target,
            IList<string> kinds,
            Message description,
            IDictionary<string, SerializedPropertyInfo> properties
        ) 
            : this(SarifLogDatabase.Current.LocationRelationship)
        {
            Target = target;
            Kinds = kinds;
            Description = description;
            Properties = properties;
        }

        public LocationRelationship(LocationRelationship other) 
            : this(SarifLogDatabase.Current.LocationRelationship)
        {
            Target = other.Target;
            Kinds = other.Kinds;
            Description = other.Description;
            Properties = other.Properties;
        }

        partial void Init();

        public int Target
        {
            get => _table.Target[_index];
            set => _table.Target[_index] = value;
        }

        public IList<string> Kinds
        {
            get => _table.Kinds[_index];
            set => _table.Kinds[_index] = value;
        }

        public Message Description
        {
            get => _table.Database.Message.Get(_table.Description[_index]);
            set => _table.Description[_index] = _table.Database.Message.LocalIndex(value);
        }

        internal override IDictionary<string, SerializedPropertyInfo> Properties
        {
            get => _table.Properties[_index];
            set => _table.Properties[_index] = value;
        }

        #region IEquatable<LocationRelationship>
        public bool Equals(LocationRelationship other)
        {
            if (other == null) { return false; }

            if (this.Target != other.Target) { return false; }
            if (this.Kinds != other.Kinds) { return false; }
            if (this.Description != other.Description) { return false; }
            if (this.Properties != other.Properties) { return false; }

            return true;
        }
        #endregion

        #region Object overrides
        public override int GetHashCode()
        {
            int result = 17;

            unchecked
            {
                if (Target != default(int))
                {
                    result = (result * 31) + Target.GetHashCode();
                }

                if (Kinds != default(IList<string>))
                {
                    result = (result * 31) + Kinds.GetHashCode();
                }

                if (Description != default(Message))
                {
                    result = (result * 31) + Description.GetHashCode();
                }

                if (Properties != default(IDictionary<string, SerializedPropertyInfo>))
                {
                    result = (result * 31) + Properties.GetHashCode();
                }
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LocationRelationship);
        }

        public static bool operator ==(LocationRelationship left, LocationRelationship right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(LocationRelationship left, LocationRelationship right)
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

        void IRow.Next()
        {
            _index++;
        }
        #endregion

        #region ISarifNode
        public SarifNodeKind SarifNodeKind => SarifNodeKind.LocationRelationship;

        ISarifNode ISarifNode.DeepClone()
        {
            return DeepCloneCore();
        }

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        public LocationRelationship DeepClone()
        {
            return (LocationRelationship)DeepCloneCore();
        }

        private ISarifNode DeepCloneCore()
        {
            return new LocationRelationship(this);
        }
        #endregion

        public static IEqualityComparer<LocationRelationship> ValueComparer => EqualityComparer<LocationRelationship>.Default;
        public bool ValueEquals(LocationRelationship other) => Equals(other);
        public int ValueGetHashCode() => GetHashCode();
    }
}
