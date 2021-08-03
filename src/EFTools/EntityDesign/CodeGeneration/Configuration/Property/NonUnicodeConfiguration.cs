// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    /// <summary>
    /// Represents a model configuration to mark a string property as non-Unicode.
    /// </summary>
    public class NonUnicodeConfiguration : IFluentConfiguration
    {
        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            return ".IsUnicode(" + code.Literal(false) + ")";
        }
    }
}
