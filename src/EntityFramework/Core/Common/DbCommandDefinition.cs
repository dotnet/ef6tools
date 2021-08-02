// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common
{
    using System.Data.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// A prepared command definition, can be cached and reused to avoid
    /// repreparing a command.
    /// </summary>
    public class DbCommandDefinition
    {
        private readonly DbCommand _prototype;
        private readonly Func<DbCommand, DbCommand> _cloneMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.Core.Common.DbCommandDefinition" /> class using the supplied
        /// <see
        ///     cref="T:System.Data.Common.DbCommand" />
        /// .
        /// </summary>
        /// <param name="prototype">
        /// The supplied <see cref="T:System.Data.Common.DbCommand" />.
        /// </param>
        /// <param name="cloneMethod"> method used to clone the <see cref="T:System.Data.Common.DbCommand" /> </param>
        protected internal DbCommandDefinition(DbCommand prototype, Func<DbCommand, DbCommand> cloneMethod)
        {
            Check.NotNull(prototype, "prototype");
            Check.NotNull(cloneMethod, "cloneMethod");
            _prototype = prototype;
            _cloneMethod = cloneMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.Core.Common.DbCommandDefinition" /> class.
        /// </summary>
        protected DbCommandDefinition()
        {
        }

        /// <summary>
        /// Creates and returns a <see cref="T:System.Data.Common.DbCommand" /> object that can be executed.
        /// </summary>
        /// <returns>The command for database.</returns>
        public virtual DbCommand CreateCommand()
        {
            return _cloneMethod(_prototype);
        }

        internal static void PopulateParameterFromTypeUsage(DbParameter parameter, TypeUsage type, bool isOutParam)
        {
            DebugCheck.NotNull(parameter);
            DebugCheck.NotNull(type);

            // parameter.IsNullable - from the NullableConstraintAttribute value
            parameter.IsNullable = TypeSemantics.IsNullable(type);

            // parameter.ParameterName - set by the caller;
            // parameter.SourceColumn - not applicable until we have a data adapter;
            // parameter.SourceColumnNullMapping - not applicable until we have a data adapter;
            // parameter.SourceVersion - not applicable until we have a data adapter;
            // parameter.Value - left unset;
            // parameter.DbType - determined by the TypeMapping;
            // parameter.Precision - from the TypeMapping;
            // parameter.Scale - from the TypeMapping;
            // parameter.Size - from the TypeMapping;

            // type.EdmType may not be a primitive type here - e.g. the user specified
            // a complex or entity type when creating an ObjectParameter instance. To keep 
            // the same behavior we had in previous versions we let it through here. We will 
            // throw an exception later when actually invoking the stored procedure where we
            // don't allow parameters that are non-primitive.
            if (Helper.IsPrimitiveType(type.EdmType))
            {
                DbType dbType;
                if (TryGetDbTypeFromPrimitiveType((PrimitiveType)type.EdmType, out dbType))
                {
                    switch (dbType)
                    {
                        case DbType.Binary:
                            PopulateBinaryParameter(parameter, type, dbType, isOutParam);
                            break;
                        case DbType.DateTime:
                        case DbType.Time:
                        case DbType.DateTimeOffset:
                            PopulateDateTimeParameter(parameter, type, dbType);
                            break;
                        case DbType.Decimal:
                            PopulateDecimalParameter(parameter, type, dbType);
                            break;
                        case DbType.String:
                            PopulateStringParameter(parameter, type, isOutParam);
                            break;
                        default:
                            parameter.DbType = dbType;
                            break;
                    }
                }
            }
        }

        internal static bool TryGetDbTypeFromPrimitiveType(PrimitiveType type, out DbType dbType)
        {
            switch (type.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    dbType = DbType.Binary;
                    return true;
                case PrimitiveTypeKind.Boolean:
                    dbType = DbType.Boolean;
                    return true;
                case PrimitiveTypeKind.Byte:
                    dbType = DbType.Byte;
                    return true;
                case PrimitiveTypeKind.DateTime:
                    dbType = DbType.DateTime;
                    return true;
                case PrimitiveTypeKind.Time:
                    dbType = DbType.Time;
                    return true;
                case PrimitiveTypeKind.DateTimeOffset:
                    dbType = DbType.DateTimeOffset;
                    return true;
                case PrimitiveTypeKind.Decimal:
                    dbType = DbType.Decimal;
                    return true;
                case PrimitiveTypeKind.Double:
                    dbType = DbType.Double;
                    return true;
                case PrimitiveTypeKind.Guid:
                    dbType = DbType.Guid;
                    return true;
                case PrimitiveTypeKind.Single:
                    dbType = DbType.Single;
                    return true;
                case PrimitiveTypeKind.SByte:
                    dbType = DbType.SByte;
                    return true;
                case PrimitiveTypeKind.Int16:
                    dbType = DbType.Int16;
                    return true;
                case PrimitiveTypeKind.Int32:
                    dbType = DbType.Int32;
                    return true;
                case PrimitiveTypeKind.Int64:
                    dbType = DbType.Int64;
                    return true;
                case PrimitiveTypeKind.String:
                    dbType = DbType.String;
                    return true;
                default:
                    dbType = default(DbType);
                    return false;
            }
        }

        private static void PopulateBinaryParameter(DbParameter parameter, TypeUsage type, DbType dbType, bool isOutParam)
        {
            parameter.DbType = dbType;

            // For each facet, set the facet value only if we have it, note that it's possible to not have
            // it in the case the facet value is null
            SetParameterSize(parameter, type, isOutParam);
        }

        private static void PopulateDecimalParameter(DbParameter parameter, TypeUsage type, DbType dbType)
        {
            parameter.DbType = dbType;
            IDbDataParameter dataParameter = parameter;

            // For each facet, set the facet value only if we have it, note that it's possible to not have
            // it in the case the facet value is null
            byte precision;
            byte scale;
            if (TypeHelpers.TryGetPrecision(type, out precision))
            {
                dataParameter.Precision = precision;
            }

            if (TypeHelpers.TryGetScale(type, out scale))
            {
                dataParameter.Scale = scale;
            }
        }

        private static void PopulateDateTimeParameter(DbParameter parameter, TypeUsage type, DbType dbType)
        {
            parameter.DbType = dbType;
            IDbDataParameter dataParameter = parameter;

            // For each facet, set the facet value only if we have it, note that it's possible to not have
            // it in the case the facet value is null
            byte precision;
            if (TypeHelpers.TryGetPrecision(type, out precision))
            {
                dataParameter.Precision = precision;
            }
        }

        private static void PopulateStringParameter(DbParameter parameter, TypeUsage type, bool isOutParam)
        {
            // For each facet, set the facet value only if we have it, note that it's possible to not have
            // it in the case the facet value is null
            var unicode = true;
            var fixedLength = false;

            if (!TypeHelpers.TryGetIsFixedLength(type, out fixedLength))
            {
                // If we can't get the fixed length facet value, then default to fixed length = false
                fixedLength = false;
            }

            if (!TypeHelpers.TryGetIsUnicode(type, out unicode))
            {
                // If we can't get the unicode facet value, then default to unicode = true
                unicode = true;
            }

            if (fixedLength)
            {
                parameter.DbType = (unicode ? DbType.StringFixedLength : DbType.AnsiStringFixedLength);
            }
            else
            {
                parameter.DbType = (unicode ? DbType.String : DbType.AnsiString);
            }

            SetParameterSize(parameter, type, isOutParam);
        }

        private static void SetParameterSize(DbParameter parameter, TypeUsage type, bool isOutParam)
        {
            // only set the size if the parameter has a specific size value.
            Facet maxLengthFacet;
            if (type.Facets.TryGetValue(DbProviderManifest.MaxLengthFacetName, true, out maxLengthFacet)
                && maxLengthFacet.Value != null)
            {
                // only set size if there is a specific size
                if (!Helper.IsUnboundedFacetValue(maxLengthFacet))
                {
                    parameter.Size = (int)maxLengthFacet.Value;
                }
                else if (isOutParam)
                {
                    // if it is store procedure parameter and it is unbounded set the size to max
                    parameter.Size = Int32.MaxValue;
                }
            }
        }
    }
}
