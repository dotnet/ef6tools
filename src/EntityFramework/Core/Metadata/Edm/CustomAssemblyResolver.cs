﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Reflection;

    internal class CustomAssemblyResolver : MetadataArtifactAssemblyResolver
    {
        private readonly Func<AssemblyName, Assembly> _referenceResolver;
        private readonly Func<IEnumerable<Assembly>> _wildcardAssemblyEnumerator;

        internal CustomAssemblyResolver(
            Func<IEnumerable<Assembly>> wildcardAssemblyEnumerator, Func<AssemblyName, Assembly> referenceResolver)
        {
            DebugCheck.NotNull(wildcardAssemblyEnumerator);
            DebugCheck.NotNull(referenceResolver);
            _wildcardAssemblyEnumerator = wildcardAssemblyEnumerator;
            _referenceResolver = referenceResolver;
        }

        internal override bool TryResolveAssemblyReference(AssemblyName refernceName, out Assembly assembly)
        {
            assembly = _referenceResolver(refernceName);
            return assembly != null;
        }

        internal override IEnumerable<Assembly> GetWildcardAssemblies()
        {
            var wildcardAssemblies = _wildcardAssemblyEnumerator();
            if (wildcardAssemblies == null)
            {
                throw new InvalidOperationException(Strings.WildcardEnumeratorReturnedNull);
            }
            return wildcardAssemblies;
        }
    }
}
