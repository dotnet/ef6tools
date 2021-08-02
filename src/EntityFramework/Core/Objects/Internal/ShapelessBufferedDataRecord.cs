// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.Internal
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
#if !NET40
    using System.Threading;
    using System.Threading.Tasks;
#endif

    internal class ShapelessBufferedDataRecord : BufferedDataRecord
    {
        private object[] _currentRow;
        private List<object[]> _resultSet;
        private DbSpatialDataReader _spatialDataReader;
        private bool[] _geographyColumns;
        private bool[] _geometryColumns;

        protected ShapelessBufferedDataRecord()
        {
        }

        internal static ShapelessBufferedDataRecord Initialize(
            string providerManifestToken, DbProviderServices providerSerivces, DbDataReader reader)
        {
            var record = new ShapelessBufferedDataRecord();
            record.ReadMetadata(providerManifestToken, providerSerivces, reader);

            var fieldCount = record.FieldCount;
            var resultSet = new List<object[]>();
            if (record._spatialDataReader != null)
            {
                while (reader.Read())
                {
                    var row = new object[fieldCount];
                    for (var i = 0; i < fieldCount; i++)
                    {
                        if (reader.IsDBNull(i))
                        {
                            row[i] = DBNull.Value;
                        }
                        else if (record._geographyColumns[i])
                        {
                            row[i] = record._spatialDataReader.GetGeography(i);
                        }
                        else if (record._geometryColumns[i])
                        {
                            row[i] = record._spatialDataReader.GetGeometry(i);
                        }
                        else
                        {
                            row[i] = reader.GetValue(i);
                        }
                    }
                    resultSet.Add(row);
                }
            }
            else
            {
                while (reader.Read())
                {
                    var row = new object[fieldCount];
                    reader.GetValues(row);
                    resultSet.Add(row);
                }
            }

            record._rowCount = resultSet.Count;
            record._resultSet = resultSet;
            return record;
        }

#if !NET40

        internal static async Task<ShapelessBufferedDataRecord> InitializeAsync(
            string providerManifestToken, DbProviderServices providerSerivces, DbDataReader reader, CancellationToken cancellationToken)
        {
            var record = new ShapelessBufferedDataRecord();
            record.ReadMetadata(providerManifestToken, providerSerivces, reader);

            var fieldCount = record.FieldCount;
            var resultSet = new List<object[]>();
            while (await reader.ReadAsync(cancellationToken).WithCurrentCulture())
            {
                var row = new object[fieldCount];
                for (var i = 0; i < fieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i, cancellationToken).WithCurrentCulture())
                    {
                        row[i] = DBNull.Value;
                    }
                    else if (record._spatialDataReader != null
                             && record._geographyColumns[i])
                    {
                        row[i] = await record._spatialDataReader.GetGeographyAsync(i, cancellationToken)
                                           .WithCurrentCulture();
                    }
                    else if (record._spatialDataReader != null
                             && record._geometryColumns[i])
                    {
                        row[i] = await record._spatialDataReader.GetGeometryAsync(i, cancellationToken)
                                           .WithCurrentCulture();
                    }
                    else
                    {
                        row[i] = await reader.GetFieldValueAsync<object>(i, cancellationToken)
                                           .WithCurrentCulture();
                    }
                }
                resultSet.Add(row);
            }

            record._rowCount = resultSet.Count;
            record._resultSet = resultSet;
            return record;
        }

#endif

        protected override void ReadMetadata(string providerManifestToken, DbProviderServices providerServices, DbDataReader reader)
        {
            base.ReadMetadata(providerManifestToken, providerServices, reader);

            var fieldCount = FieldCount;
            var hasSpatialColumns = false;
            DbSpatialDataReader spatialDataReader = null;
            if (fieldCount > 0)
            {
                // FieldCount == 0 indicates NullDataReader
                spatialDataReader = providerServices.GetSpatialDataReader(reader, providerManifestToken);
            }

            if (spatialDataReader != null)
            {
                _geographyColumns = new bool[fieldCount];
                _geometryColumns = new bool[fieldCount];

                for (var i = 0; i < fieldCount; i++)
                {
                    _geographyColumns[i] = spatialDataReader.IsGeographyColumn(i);
                    _geometryColumns[i] = spatialDataReader.IsGeometryColumn(i);
                    hasSpatialColumns = hasSpatialColumns || _geographyColumns[i] || _geometryColumns[i];
                    Debug.Assert(!_geographyColumns[i] || !_geometryColumns[i]);
                }
            }

            _spatialDataReader = hasSpatialColumns ? spatialDataReader : null;
        }

        public override bool GetBoolean(int ordinal)
        {
            return GetFieldValue<bool>(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return GetFieldValue<byte>(ordinal);
        }

        public override char GetChar(int ordinal)
        {
            return GetFieldValue<char>(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return GetFieldValue<DateTime>(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return GetFieldValue<decimal>(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return GetFieldValue<double>(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return GetFieldValue<float>(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return GetFieldValue<Guid>(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return GetFieldValue<short>(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return GetFieldValue<int>(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return GetFieldValue<long>(ordinal);
        }

        public override string GetString(int ordinal)
        {
            return GetFieldValue<string>(ordinal);
        }

        public override T GetFieldValue<T>(int ordinal)
        {
            return (T)_currentRow[ordinal];
        }

#if !NET40

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "cancellationToken")]
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
        {
            return Task.FromResult((T)_currentRow[ordinal]);
        }

#endif

        public override object GetValue(int ordinal)
        {
            return GetFieldValue<object>(ordinal);
        }

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; ++i)
            {
                values[i] = GetValue(i);
            }
            return count;
        }

        public override bool IsDBNull(int ordinal)
        {
            if (_currentRow.Length == 0)
            {
                // Reader is being intercepted
                return true;
            }

            return DBNull.Value == _currentRow[ordinal];
        }

#if !NET40

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "cancellationToken")]
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsDBNull(ordinal));
        }

#endif

        public override bool Read()
        {
            if (++_currentRowNumber < _rowCount)
            {
                _currentRow = _resultSet[_currentRowNumber];
                IsDataReady = true;
            }
            else
            {
                _currentRow = null;
                IsDataReady = false;
            }

            return IsDataReady;
        }

#if !NET40

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "cancellationToken")]
        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Read());
        }

#endif
    }
}
