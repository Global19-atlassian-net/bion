// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using BSOA.Model;

namespace BSOA.Column
{
    public class ColumnFactory
    {
        private static Dictionary<Type, Func<object, IColumn>> Builders = new Dictionary<Type, Func<object, IColumn>>()
        {
            // Use available typed columns
            [typeof(string)] = (defaultValue) => new StringColumn(),
            [typeof(bool)] = (defaultValue) => new BooleanColumn((bool)(defaultValue ?? default(bool))),
            [typeof(Uri)] = (defaultValue) => new UriColumn(),
            [typeof(DateTime)] = (defaultValue) => new DateTimeColumn((DateTime)(defaultValue ?? default(DateTime))),

            // Use NumberColumn for all numeric types
            [typeof(byte)] = (defaultValue) => new NumberColumn<byte>((byte)(defaultValue ?? default(byte))),
            [typeof(sbyte)] = (defaultValue) => new NumberColumn<sbyte>((sbyte)(defaultValue ?? default(sbyte))),
            [typeof(short)] = (defaultValue) => new NumberColumn<short>((short)(defaultValue ?? default(short))),
            [typeof(ushort)] = (defaultValue) => new NumberColumn<ushort>((ushort)(defaultValue ?? default(ushort))),
            [typeof(int)] = (defaultValue) => new NumberColumn<int>((int)(defaultValue ?? default(int))),
            [typeof(uint)] = (defaultValue) => new NumberColumn<uint>((uint)(defaultValue ?? default(uint))),
            [typeof(long)] = (defaultValue) => new NumberColumn<long>((long)(defaultValue ?? default(long))),
            [typeof(ulong)] = (defaultValue) => new NumberColumn<ulong>((ulong)(defaultValue ?? default(ulong))),
            [typeof(float)] = (defaultValue) => new NumberColumn<float>((float)(defaultValue ?? default(float))),
            [typeof(double)] = (defaultValue) => new NumberColumn<double>((double)(defaultValue ?? default(double))),
            [typeof(char)] = (defaultValue) => new NumberColumn<char>((char)(defaultValue ?? default(char))),

            // Use GenericNumberListColumn (rather than ListColumn) for IList<number>
            [typeof(IList<byte>)] = (defaultValue) => new GenericNumberListColumn<byte>(),
            [typeof(IList<sbyte>)] = (defaultValue) => new GenericNumberListColumn<sbyte>(),
            [typeof(IList<short>)] = (defaultValue) => new GenericNumberListColumn<short>(),
            [typeof(IList<ushort>)] = (defaultValue) => new GenericNumberListColumn<ushort>(),
            [typeof(IList<int>)] = (defaultValue) => new GenericNumberListColumn<int>(),
            [typeof(IList<uint>)] = (defaultValue) => new GenericNumberListColumn<uint>(),
            [typeof(IList<long>)] = (defaultValue) => new GenericNumberListColumn<long>(),
            [typeof(IList<ulong>)] = (defaultValue) => new GenericNumberListColumn<ulong>(),
            [typeof(IList<float>)] = (defaultValue) => new GenericNumberListColumn<float>(),
            [typeof(IList<double>)] = (defaultValue) => new GenericNumberListColumn<double>(),
            [typeof(IList<char>)] = (defaultValue) => new GenericNumberListColumn<char>(),
        };

        public static IColumn<T> BuildTyped<T>(T defaultValue = default, Func<Type, object, IColumn> recurseTo = null)
        {
            return (IColumn<T>)Build(typeof(T), defaultValue, recurseTo);
        }

        public static IColumn Build(Type type)
        {
            return Build(type, defaultValue: null, recurseTo: null);
        }

        public static IColumn Build(Type type, object defaultValue)
        {
            return Build(type, defaultValue: defaultValue, recurseTo: null);
        }

        public static IColumn Build(Type type, object defaultValue, Func<Type, object, IColumn> recurseTo)
        {
            if (recurseTo == null) { recurseTo = Build; }

            if (Builders.TryGetValue(type, out Func<object, IColumn> creator))
            {
                return creator(defaultValue);
            }

            if (type.IsEnum)
            {
                throw new NotSupportedException($"Enums should use a NumberColumn of the underlying type and be casted to and from the enum type in the property getter/setter.");
            }

            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();
                Type[] typeArguments = type.GetGenericArguments();

                if (genericType == typeof(IList<>))
                {
                    return BuildList(typeArguments[0], defaultValue, recurseTo);
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    return BuildDictionary(typeArguments[0], typeArguments[1], defaultValue, recurseTo);
                }
            }

            throw new NotImplementedException($"ColumnFactory doesn't know how to build an IColumn<{type.Name}>.");
        }

        private static IColumn BuildList(Type itemType, object defaultValue, Func<Type, object, IColumn> recurseTo)
        {
            IColumn innerColumn = recurseTo(itemType, null);
            return (IColumn)(typeof(ListColumn<>).MakeGenericType(itemType).GetConstructor(new[] { typeof(IColumn), typeof(object) }).Invoke(new object[] { innerColumn, defaultValue }));
        }

        private static IColumn BuildDictionary(Type keyType, Type valueType, object defaultValue, Func<Type, object, IColumn> recurseTo)
        {
            IColumn keyColumn = WrapInDistinct(recurseTo(keyType, null), null);
            IColumn valueColumn = recurseTo(valueType, null);
            return (IColumn)(typeof(DictionaryColumn<,>).MakeGenericType(keyType, valueType).GetConstructor(new[] { typeof(IColumn), typeof(IColumn), typeof(object) }).Invoke(new object[] { keyColumn, valueColumn, defaultValue }));
        }

        private static IColumn WrapInDistinct(IColumn inner, object defaultValue)
        {
            return (IColumn)(typeof(DistinctColumn<>).MakeGenericType(inner.Type).GetConstructor(new[] { typeof(IColumn), typeof(object) }).Invoke(new object[] { inner, defaultValue }));
        }
    }
}
