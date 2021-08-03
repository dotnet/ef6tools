// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Represents a model configuration to set the cascade delete option of an association.
    /// </summary>
    public class CascadeDeleteConfiguration : IFluentConfiguration
    {
        /// <summary>
        /// Gets or sets the cascade delete option.
        /// </summary>
        public OperationAction DeleteBehavior { get; set; }

        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            var builder = new StringBuilder();

            builder.Append(".WillCascadeOnDelete(");

            if (DeleteBehavior != OperationAction.Cascade)
            {
                Debug.Assert(DeleteBehavior == OperationAction.None, "DeleteBehavior is not None.");

                builder.Append(code.Literal(false));
            }

            builder.Append(")");

            return builder.ToString();
        }
    }
}
