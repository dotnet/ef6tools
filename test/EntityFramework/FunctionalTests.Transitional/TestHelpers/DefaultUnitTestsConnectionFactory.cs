// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    /// <summary>
    /// This connection factory is set in the <see cref="FunctionalTestsConfiguration" /> but is then
    /// replaced in the Loaded event handler of that class.
    /// </summary>
    public class DefaultUnitTestsConnectionFactory : DefaultFunctionalTestsConnectionFactory
    {
    }
}
