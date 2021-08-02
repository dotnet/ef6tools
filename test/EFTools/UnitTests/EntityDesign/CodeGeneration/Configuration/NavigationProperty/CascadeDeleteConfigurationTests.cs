// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class CascadeDeleteConfigurationTests
    {
        [Fact]
        public void GetMethodChain_returns_chain_when_cascade()
        {
            var configuration = new CascadeDeleteConfiguration { DeleteBehavior = OperationAction.Cascade };
            var code = new CSharpCodeHelper();

            Assert.Equal(".WillCascadeOnDelete()", configuration.GetMethodChain(code));
        }

        [Fact]
        public void GetMethodChain_returns_chain_when_none()
        {
            var configuration = new CascadeDeleteConfiguration { DeleteBehavior = OperationAction.None };
            var code = new CSharpCodeHelper();

            Assert.Equal(".WillCascadeOnDelete(false)", configuration.GetMethodChain(code));
        }
    }
}
