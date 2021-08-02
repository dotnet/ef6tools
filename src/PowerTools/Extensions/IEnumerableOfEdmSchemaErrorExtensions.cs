﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
namespace Microsoft.DbContextPackage.Extensions
{
    using System.Collections.Generic;
    using System.Data.Metadata.Edm;
    using System.Linq;
    using Microsoft.DbContextPackage.Utilities;

    internal static class IEnumerableOfEdmSchemaErrorExtensions
    {
        public static void HandleErrors(this IEnumerable<EdmSchemaError> errors, string message)
        {
            DebugCheck.NotNull(errors);

            if (errors.HasErrors())
            {
                throw new EdmSchemaErrorException(message, errors);
            }
        }

        private static bool HasErrors(this IEnumerable<EdmSchemaError> errors)
        {
            DebugCheck.NotNull(errors);

            return errors.Any(e => e.Severity == EdmSchemaErrorSeverity.Error);
        }
    }
}
