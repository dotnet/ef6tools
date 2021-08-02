// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Data.Common;
    
    public class GenericConnection<T> : DbConnection
        where T : DbProviderFactory
    {
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Open()
        {
            throw new NotImplementedException();
        }

        public override string ConnectionString
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override string DataSource
        {
            get { throw new NotImplementedException(); }
        }

        public override string Database
        {
            get { throw new NotImplementedException(); }
        }

        public override ConnectionState State
        {
            get { throw new NotImplementedException(); }
        }

        public override string ServerVersion
        {
            get { throw new NotImplementedException(); }
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotImplementedException();
        }

        protected override DbProviderFactory DbProviderFactory
        {
            get { return GenericProviderFactory<T>.Instance; }
        }
    }
}
