﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.ModelConfiguration.Configuration.Types;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Reflection;

    internal class ConfigurationTypeActivator
    {
        public virtual TStructuralTypeConfiguration Activate<TStructuralTypeConfiguration>(Type type)
            where TStructuralTypeConfiguration : StructuralTypeConfiguration
        {
            DebugCheck.NotNull(type);

            if (type.GetDeclaredConstructor() == null)
            {
                throw new InvalidOperationException(Strings.CreateConfigurationType_NoParameterlessConstructor(type.Name));
            }

            return (TStructuralTypeConfiguration)typeof(StructuralTypeConfiguration<>)
                                                     .MakeGenericType(type.TryGetElementType(typeof(StructuralTypeConfiguration<>)))
                                                     .GetDeclaredProperty("Configuration")
                                                     .GetValue(Activator.CreateInstance(type, nonPublic: true), null);
        }
    }
}
