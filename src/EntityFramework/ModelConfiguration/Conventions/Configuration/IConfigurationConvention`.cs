// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Data.Entity.ModelConfiguration.Configuration;
    using System.Reflection;

    internal interface IConfigurationConvention<TMemberInfo> : IConvention
        where TMemberInfo : MemberInfo
    {
        void Apply(TMemberInfo memberInfo, ModelConfiguration modelConfiguration);
    }
}
