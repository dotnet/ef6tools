﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if EF_FUNCTIONALS
namespace System.Data.Entity.Functionals.Utilities
#else
namespace System.Data.Entity.Utilities
#endif
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class AssemblyExtensions
    {
        public static string GetInformationalVersion(this Assembly assembly)
        {
            DebugCheck.NotNull(assembly);

            return assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .Single()
                .InformationalVersion;
        }

        public static IEnumerable<Type> GetAccessibleTypes(this Assembly assembly)
        {
            try
            {
#if NET40
                return assembly.GetTypes();
#else
                return assembly.DefinedTypes.Select(t => t.AsType());
#endif
            }
            catch (ReflectionTypeLoadException ex)
            {
                // The exception is thrown if some types cannot be loaded in partial trust.
                // For our purposes we just want to get the types that are loaded, which are
                // provided in the Types property of the exception.
                return ex.Types.Where(t => t != null);
            }
        }

#if NET40
        public static IEnumerable<T> GetCustomAttributes<T>(this Assembly assembly) where T : Attribute
        {
            DebugCheck.NotNull(assembly);

            return assembly.GetCustomAttributes(typeof(T), inherit: false).OfType<T>();
        }
#endif
    }
}
