﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Entity.Utilities;

    /// <summary>
    /// A key used for resolving <see cref="Func{IExecutionStrategy}" />. It consists of the ADO.NET provider invariant name
    /// and the database server name as specified in the connection string.
    /// </summary>
    public class ExecutionStrategyKey
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExecutionStrategyKey" />
        /// </summary>
        /// <param name="providerInvariantName">
        /// The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this execution strategy will be used.
        /// </param>
        /// <param name="serverName"> A string that will be matched against the server name in the connection string. </param>
        public ExecutionStrategyKey(string providerInvariantName, string serverName)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");

            ProviderInvariantName = providerInvariantName;
            ServerName = serverName;
        }

        /// <summary>
        /// The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this execution strategy will be used.
        /// </summary>
        public string ProviderInvariantName { get; private set; }

        /// <summary>
        /// A string that will be matched against the server name in the connection string.
        /// </summary>
        public string ServerName { get; private set; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var otherKey = obj as ExecutionStrategyKey;
            if (ReferenceEquals(otherKey, null))
            {
                return false;
            }

            return ProviderInvariantName.Equals(otherKey.ProviderInvariantName, StringComparison.Ordinal)
                   && ((ServerName == null && otherKey.ServerName == null) ||
                       (ServerName != null && ServerName.Equals(otherKey.ServerName, StringComparison.Ordinal)));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (ServerName != null)
            {
                return ProviderInvariantName.GetHashCode() ^ ServerName.GetHashCode();
            }

            return ProviderInvariantName.GetHashCode();
        }
    }
}
