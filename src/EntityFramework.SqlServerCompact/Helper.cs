// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServerCompact.SqlGen
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;

    internal static class Helper
    {
        // <summary>
        // Searches for Facet Description with the name specified.
        // </summary>
        // <param name="facetCollection"> Collection of facet description </param>
        // <param name="facetName"> name of the facet </param>
        internal static FacetDescription GetFacet(IEnumerable<FacetDescription> facetCollection, string facetName)
        {
            foreach (var facetDescription in facetCollection)
            {
                if (facetDescription.FacetName == facetName)
                {
                    return facetDescription;
                }
            }

            return null;
        }

        internal static bool IsUnboundedFacetValue(Facet facet)
        {
            return (null == facet.Value || facet.IsUnbounded);
        }
    }
}
