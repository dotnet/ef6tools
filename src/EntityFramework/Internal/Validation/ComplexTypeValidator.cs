// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Validation
{
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Data.Entity.Validation;
    using System.Diagnostics;

    // <summary>
    // Validator used to validate a property of a given EDM ComplexType.
    // </summary>
    // <remarks>
    // This is a composite validator.
    // </remarks>
    internal class ComplexTypeValidator : TypeValidator
    {
        // <summary>
        // Creates an instance <see cref="EntityValidator" /> for a given EDM complex type.
        // </summary>
        // <param name="propertyValidators"> Property validators. </param>
        // <param name="typeLevelValidators"> Type level validators. </param>
        public ComplexTypeValidator(
            IEnumerable<PropertyValidator> propertyValidators, IEnumerable<IValidator> typeLevelValidators)
            :
                base(propertyValidators, typeLevelValidators)
        {
        }

        // <summary>
        // Validates an instance.
        // </summary>
        // <param name="entityValidationContext"> Entity validation context. Must not be null. </param>
        // <param name="property"> The entry for the complex property. Null if validating an entity. </param>
        // <returns>
        // <see cref="DbEntityValidationResult" /> instance. Never null.
        // </returns>
        public new IEnumerable<DbValidationError> Validate(
            EntityValidationContext entityValidationContext, InternalPropertyEntry property)
        {
            return base.Validate(entityValidationContext, property);
        }

        // <summary>
        // Validates type properties. Any validation errors will be added to <paramref name="validationErrors" />
        // collection.
        // </summary>
        // <param name="entityValidationContext"> Validation context. Must not be null. </param>
        // <param name="parentProperty"> The entry for the complex property. Null if validating an entity. </param>
        // <param name="validationErrors"> Collection of validation errors. Any validation errors will be added to it. </param>
        // <remarks>
        // Note that <paramref name="validationErrors" /> will be modified by this method. Errors should be only added,
        // never removed or changed. Taking a collection as a modifiable parameter saves a couple of memory allocations
        // and a merge of validation error lists per entity.
        // </remarks>
        protected override void ValidateProperties(
            EntityValidationContext entityValidationContext, InternalPropertyEntry parentProperty,
            List<DbValidationError> validationErrors)
        {
            DebugCheck.NotNull(entityValidationContext);
            DebugCheck.NotNull(parentProperty);
            DebugCheck.NotNull(validationErrors);

            Debug.Assert(parentProperty.EntryMetadata.IsComplex, "A complex type expected.");
            Debug.Assert(parentProperty.CurrentValue != null);

            foreach (var validator in PropertyValidators)
            {
                var complexProperty = parentProperty.Property(validator.PropertyName);
                validationErrors.AddRange(validator.Validate(entityValidationContext, complexProperty));
            }
        }
    }
}
