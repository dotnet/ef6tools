// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Model
{
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Linq;

    /// <summary>
    /// Represents an operation to modify a database schema.
    ///
    /// Entity Framework Migrations APIs are not designed to accept input provided by untrusted sources 
    /// (such as the end user of an application). If input is accepted from such sources it should be validated 
    /// before being passed to these APIs to protect against SQL injection attacks etc.
    /// </summary>
    public abstract class MigrationOperation
    {
        private readonly IDictionary<string, object> _anonymousArguments
            = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the MigrationOperation class.
        ///
        /// Entity Framework Migrations APIs are not designed to accept input provided by untrusted sources 
        /// (such as the end user of an application). If input is accepted from such sources it should be validated 
        /// before being passed to these APIs to protect against SQL injection attacks etc.
        /// </summary>
        /// <param name="anonymousArguments">
        /// Use anonymous type syntax to specify arguments e.g. 'new { SampleArgument = "MyValue"
        /// }'.
        /// </param>
        protected MigrationOperation(object anonymousArguments)
        {
            if (anonymousArguments != null)
            {
                anonymousArguments
                    .GetType()
                    .GetNonIndexerProperties()
                    .Each(p => _anonymousArguments.Add(p.Name, p.GetValue(anonymousArguments, null)));
            }
        }

        /// <summary>
        /// Gets additional arguments that may be processed by providers.
        /// 
        /// Entity Framework Migrations APIs are not designed to accept input provided by untrusted sources 
        /// (such as the end user of an application). If input is accepted from such sources it should be validated 
        /// before being passed to these APIs to protect against SQL injection attacks etc.
        /// </summary>
        public IDictionary<string, object> AnonymousArguments
        {
            get { return _anonymousArguments; }
        }

        /// <summary>
        /// Gets an operation that will revert this operation.
        /// </summary>
        public virtual MigrationOperation Inverse
        {
            get { return null; }
        }

        /// <summary>
        /// Gets a value indicating if this operation may result in data loss.
        /// </summary>
        public abstract bool IsDestructiveChange { get; }
    }
}
