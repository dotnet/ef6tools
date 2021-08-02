// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Class representing a primitive type
    /// </summary>
    public class PrimitiveType : SimpleType
    {
        // <summary>
        // Initializes a new instance of PrimitiveType
        // </summary>
        internal PrimitiveType()
        {
            // No initialization of item attributes in here, it's used as a pass thru in the case for delay population
            // of item attributes
        }

        // <summary>
        // The constructor for PrimitiveType.  It takes the required information to identify this type.
        // </summary>
        // <param name="name"> The name of this type </param>
        // <param name="namespaceName"> The namespace name of this type </param>
        // <param name="dataSpace"> dataSpace in which this primitive type belongs to </param>
        // <param name="baseType"> The primitive type that this type is derived from </param>
        // <param name="providerManifest"> The ProviderManifest of the provider of this type </param>
        // <exception cref="System.ArgumentNullException">Thrown if name, namespaceName, version, baseType or providerManifest arguments are null</exception>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal PrimitiveType(
            string name,
            string namespaceName,
            DataSpace dataSpace,
            PrimitiveType baseType,
            DbProviderManifest providerManifest)
            : base(name, namespaceName, dataSpace)
        {
            Check.NotNull(baseType, "baseType");
            Check.NotNull(providerManifest, "providerManifest");

            BaseType = baseType;

            Initialize(this, baseType.PrimitiveTypeKind, providerManifest);
        }

        // <summary>
        // The constructor for PrimitiveType, it takes in a CLR type containing the identity information
        // </summary>
        // <param name="clrType"> The CLR type object for this primitive type </param>
        // <param name="baseType"> The base type for this primitive type </param>
        // <param name="providerManifest"> The ProviderManifest of the provider of this type </param>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal PrimitiveType(
            Type clrType,
            PrimitiveType baseType,
            DbProviderManifest providerManifest)
            : this(Check.NotNull(clrType, "clrType").Name, clrType.NestingNamespace(),
                DataSpace.OSpace, baseType, providerManifest)
        {
            Debug.Assert(clrType == ClrEquivalentType, "not equivalent to ClrEquivalentType");
        }

        private PrimitiveTypeKind _primitiveTypeKind;
        private DbProviderManifest _providerManifest;

        /// <summary>
        /// Gets the built-in type kind for this <see cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Entity.Core.Metadata.Edm.BuiltInTypeKind" /> object that represents the built-in type kind for this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.PrimitiveType; }
        }

        internal override Type ClrType
        {
            get { return ClrEquivalentType; }
        }

        /// <summary>
        /// Gets a <see cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveTypeKind" /> enumeration value that indicates a primitive type of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveTypeKind" /> enumeration value that indicates a primitive type of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        [MetadataProperty(BuiltInTypeKind.PrimitiveTypeKind, false)]
        public virtual PrimitiveTypeKind PrimitiveTypeKind
        {
            get { return _primitiveTypeKind; }
            internal set { _primitiveTypeKind = value; }
        }

        // <summary>
        // Returns the ProviderManifest giving access to the Manifest that this type came from
        // </summary>
        // <returns> The types ProviderManifest value </returns>
        internal DbProviderManifest ProviderManifest
        {
            get
            {
                Debug.Assert(
                    _providerManifest != null, "This primitive type should have been added to a manifest, which should have set this");
                return _providerManifest;
            }
            set
            {
                DebugCheck.NotNull(value);
                _providerManifest = value;
            }
        }

        /// <summary>
        /// Gets the list of facet descriptions for this <see cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />.
        /// </summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> that contains the list of facet descriptions for this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        public virtual ReadOnlyCollection<FacetDescription> FacetDescriptions
        {
            get { return ProviderManifest.GetFacetDescriptions(this); }
        }

        /// <summary>
        /// Returns an equivalent common language runtime (CLR) type of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// . Note that the
        /// <see
        ///     cref="P:System.Data.Entity.Core.Metadata.Edm.PrimitiveType.ClrEquivalentType" />
        /// property always returns a non-nullable type value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Type" /> object that represents an equivalent common language runtime (CLR) type of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        public Type ClrEquivalentType
        {
            get
            {
                switch (PrimitiveTypeKind)
                {
                    case PrimitiveTypeKind.Binary:
                        return typeof(byte[]);
                    case PrimitiveTypeKind.Boolean:
                        return typeof(bool);
                    case PrimitiveTypeKind.Byte:
                        return typeof(byte);
                    case PrimitiveTypeKind.DateTime:
                        return typeof(DateTime);
                    case PrimitiveTypeKind.Time:
                        return typeof(TimeSpan);
                    case PrimitiveTypeKind.DateTimeOffset:
                        return typeof(DateTimeOffset);
                    case PrimitiveTypeKind.Decimal:
                        return typeof(decimal);
                    case PrimitiveTypeKind.Double:
                        return typeof(double);
                    case PrimitiveTypeKind.Geography:
                    case PrimitiveTypeKind.GeographyPoint:
                    case PrimitiveTypeKind.GeographyLineString:
                    case PrimitiveTypeKind.GeographyPolygon:
                    case PrimitiveTypeKind.GeographyMultiPoint:
                    case PrimitiveTypeKind.GeographyMultiLineString:
                    case PrimitiveTypeKind.GeographyMultiPolygon:
                    case PrimitiveTypeKind.GeographyCollection:
                        return typeof(DbGeography);
                    case PrimitiveTypeKind.Geometry:
                    case PrimitiveTypeKind.GeometryPoint:
                    case PrimitiveTypeKind.GeometryLineString:
                    case PrimitiveTypeKind.GeometryPolygon:
                    case PrimitiveTypeKind.GeometryMultiPoint:
                    case PrimitiveTypeKind.GeometryMultiLineString:
                    case PrimitiveTypeKind.GeometryMultiPolygon:
                    case PrimitiveTypeKind.GeometryCollection:
                        return typeof(DbGeometry);
                    case PrimitiveTypeKind.Guid:
                        return typeof(Guid);
                    case PrimitiveTypeKind.Single:
                        return typeof(Single);
                    case PrimitiveTypeKind.SByte:
                        return typeof(sbyte);
                    case PrimitiveTypeKind.Int16:
                        return typeof(short);
                    case PrimitiveTypeKind.Int32:
                        return typeof(int);
                    case PrimitiveTypeKind.Int64:
                        return typeof(long);
                    case PrimitiveTypeKind.String:
                        return typeof(string);
                }

                return null;
            }
        }

        internal override IEnumerable<FacetDescription> GetAssociatedFacetDescriptions()
        {
            // return all general facets and facets associated with this type
            return base.GetAssociatedFacetDescriptions().Concat(FacetDescriptions);
        }

        // <summary>
        // Perform initialization that's common across all constructors
        // </summary>
        // <param name="primitiveType"> The primitive type to initialize </param>
        // <param name="primitiveTypeKind"> The primitive type kind of this primitive type </param>
        // <param name="providerManifest"> The ProviderManifest of the provider of this type </param>
        internal static void Initialize(
            PrimitiveType primitiveType,
            PrimitiveTypeKind primitiveTypeKind,
            DbProviderManifest providerManifest)
        {
            primitiveType._primitiveTypeKind = primitiveTypeKind;
            primitiveType._providerManifest = providerManifest;
        }

        /// <summary>
        /// Returns the equivalent <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmType" /> of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </summary>
        /// <remarks>
        /// For example if this instance is nvarchar and it's
        /// base type is Edm String then the return type is Edm String.
        /// If the type is actually already a model type then the
        /// return type is "this".
        /// </remarks>        
        /// <returns>
        /// An <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmType" /> object that is an equivalent of this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        public EdmType GetEdmPrimitiveType()
        {
            return EdmProviderManifest.GetPrimitiveType(PrimitiveTypeKind);
        }

        /// <summary>Returns the list of primitive types.</summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> that contains the list of primitive types.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static ReadOnlyCollection<PrimitiveType> GetEdmPrimitiveTypes()
        {
            return EdmProviderManifest.GetStoreTypes();
        }

        /// <summary>
        /// Returns the equivalent <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmType" /> of a
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmType" /> object that is an equivalent of a specified
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />
        /// .
        /// </returns>
        /// <param name="primitiveTypeKind">
        /// A value of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.PrimitiveType" />.
        /// </param>
        public static PrimitiveType GetEdmPrimitiveType(PrimitiveTypeKind primitiveTypeKind)
        {
            return EdmProviderManifest.GetPrimitiveType(primitiveTypeKind);
        }
    }
}
