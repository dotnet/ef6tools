// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class InterceptableDbCommand : DbCommand
    {
        private readonly DbCommand _command;
        private readonly DbInterceptionContext _interceptionContext;
        private readonly DbDispatchers _dispatchers;

        public InterceptableDbCommand(DbCommand command, DbInterceptionContext context, DbDispatchers dispatchers = null)
        {
            DebugCheck.NotNull(command);
            DebugCheck.NotNull(context);

            GC.SuppressFinalize(this);

            _command = command;

            _interceptionContext = context;

            _dispatchers = dispatchers ?? DbInterception.Dispatch;
        }

        public DbInterceptionContext InterceptionContext
        {
            get { return _interceptionContext; }
        }

        public override void Prepare()
        {
            _command.Prepare();
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public override string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }

        protected override DbConnection DbConnection
        {
            get { return _command.Connection; }
            set { _command.Connection = value; }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _command.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get { return _command.Transaction; }
            set { _command.Transaction = value; }
        }

        public override bool DesignTimeVisible
        {
            get { return _command.DesignTimeVisible; }
            set { _command.DesignTimeVisible = value; }
        }

        public override void Cancel()
        {
            _command.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _command.CreateParameter();
        }

        public override int ExecuteNonQuery()
        {
            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return 1;
            }

            return _dispatchers.Command.NonQuery(_command, new DbCommandInterceptionContext(_interceptionContext));
        }

        public override object ExecuteScalar()
        {
            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return null;
            }

            return _dispatchers.Command.Scalar(_command, new DbCommandInterceptionContext(_interceptionContext));
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return new NullDataReader();
            }

            var interceptionContext = new DbCommandInterceptionContext(_interceptionContext);
            if (behavior != CommandBehavior.Default)
            {
                interceptionContext = interceptionContext.WithCommandBehavior(behavior);
            }

            return _dispatchers.Command.Reader(_command, interceptionContext);
        }

#if !NET40
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return new Task<int>(() => 1);
            }

            return _dispatchers.Command.NonQueryAsync(_command, new DbCommandInterceptionContext(_interceptionContext), cancellationToken);
        }

        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return new Task<object>(() => null);
            }

            return _dispatchers.Command.ScalarAsync(_command, new DbCommandInterceptionContext(_interceptionContext), cancellationToken);
        }

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_dispatchers.CancelableCommand.Executing(_command, _interceptionContext))
            {
                return new Task<DbDataReader>(() => new NullDataReader());
            }

            var interceptionContext = new DbCommandInterceptionContext(_interceptionContext);
            if (behavior != CommandBehavior.Default)
            {
                interceptionContext = interceptionContext.WithCommandBehavior(behavior);
            }

            return _dispatchers.Command.ReaderAsync(_command, interceptionContext, cancellationToken);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing
                && (_command != null))
            {
                _command.Dispose();
            }

            base.Dispose(disposing);
        }

        private class NullDataReader : DbDataReader
        {
            private int _resultCount;
            private int _readCount;

            public override void Close()
            {
            }

            public override bool NextResult()
            {
                return _resultCount++ == 0;
            }

            public override bool Read()
            {
                return _readCount++ == 0;
            }

            public override bool IsClosed
            {
                get { return false; }
            }

            public override int FieldCount
            {
                get { return 0; }
            }

            public override int GetOrdinal(string name)
            {
                // Sentinal value used to short-circuit server value
                // propagation in FunctionUpdateCommand.Execute

                return -1;
            }

            public override object GetValue(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override DataTable GetSchemaTable()
            {
                throw new NotImplementedException();
            }

            public override int Depth
            {
                get { throw new NotImplementedException(); }
            }

            public override int RecordsAffected
            {
                get { return 0; }
            }

            public override bool GetBoolean(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override byte GetByte(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            {
                throw new NotImplementedException();
            }

            public override char GetChar(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            {
                throw new NotImplementedException();
            }

            public override Guid GetGuid(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override short GetInt16(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override int GetInt32(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override long GetInt64(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override DateTime GetDateTime(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override string GetString(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override decimal GetDecimal(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override double GetDouble(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override float GetFloat(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override string GetName(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override int GetValues(object[] values)
            {
                return 0;
            }

            public override bool IsDBNull(int ordinal)
            {
                return true;
            }

            public override object this[int ordinal]
            {
                get { throw new NotImplementedException(); }
            }

            public override object this[string name]
            {
                get { throw new NotImplementedException(); }
            }

            public override bool HasRows
            {
                get { throw new NotImplementedException(); }
            }

            public override string GetDataTypeName(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override Type GetFieldType(int ordinal)
            {
                throw new NotImplementedException();
            }

            public override IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
