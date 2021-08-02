// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServerCompact
{
    internal class ProviderManifest
    {
        // <summary>
        // Value to pass to GetInformation to get the StoreSchemaDefinition
        // </summary>
        public const string StoreSchemaDefinition = "StoreSchemaDefinition";

        // <summary>
        // Value to pass to GetInformation to get the StoreSchemaMapping
        // </summary>
        public const string StoreSchemaMapping = "StoreSchemaMapping";

        // <summary>
        // Value to pass to GetInformation to get the ConceptualSchemaDefinition
        // </summary>
        public const string ConceptualSchemaDefinition = "ConceptualSchemaDefinition";

        // System Facet Info
        // <summary>
        // Name of the MaxLength Facet
        // </summary>
        internal const string MaxLengthFacetName = "MaxLength";

        // <summary>
        // Name of the Unicode Facet
        // </summary>
        internal const string UnicodeFacetName = "Unicode";

        // <summary>
        // Name of the FixedLength Facet
        // </summary>
        internal const string FixedLengthFacetName = "FixedLength";

        // <summary>
        // Name of the Precision Facet
        // </summary>
        internal const string PrecisionFacetName = "Precision";

        // <summary>
        // Name of the Scale Facet
        // </summary>
        internal const string ScaleFacetName = "Scale";

        // <summary>
        // Name of the Nullable Facet
        // </summary>
        internal const string NullableFacetName = "Nullable";

        // <summary>
        // Name of the DefaultValue Facet
        // </summary>
        internal const string DefaultValueFacetName = "DefaultValue";

        // <summary>
        // Name of the Collation Facet
        // </summary>
        internal const string CollationFacetName = "Collation";
    }
}
