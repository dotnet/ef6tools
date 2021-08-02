﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    using Xunit;

    public class KeyPropertyConfigurationTests
    {
        [Fact]
        public void GetAttributeBody_returns_body()
        {
            var configuration = new KeyPropertyConfiguration();
            var code = new CSharpCodeHelper();

            Assert.Equal("Key", configuration.GetAttributeBody(code));
        }
    }
}
