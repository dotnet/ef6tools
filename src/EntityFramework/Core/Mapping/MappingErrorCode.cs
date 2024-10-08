// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    // This file contains an enum for the errors generated by StorageMappingItemCollection

    // There is almost a one-to-one correspondence between these error codes
    // and the resource strings - so if you need more insight into what the
    // error code means, please see the code that uses the particular enum
    // AND the corresponding resource string

    // error numbers end up being hard coded in test cases; they can be removed, but should not be changed.
    // reusing error numbers is probably OK, but not recommended.
    //
    // The acceptable range for this enum is
    // 2000 - 2999
    //
    // The Range 10,000-15,000 is reserved for tools
    //
    internal enum MappingErrorCode
    {
        // <summary>
        // StorageMappingErrorBase
        // </summary>
        Value = 2000,

        // <summary>
        // Invalid Content
        // </summary>
        InvalidContent = Value + 1,

        // <summary>
        // Unresolvable Entity Container Name
        // </summary>
        InvalidEntityContainer = Value + 2,

        // <summary>
        // Unresolvable Entity Set Name
        // </summary>
        InvalidEntitySet = Value + 3,

        // <summary>
        // Unresolvable Entity Type Name
        // </summary>
        InvalidEntityType = Value + 4,

        // <summary>
        // Unresolvable Association Set Name
        // </summary>
        InvalidAssociationSet = Value + 5,

        // <summary>
        // Unresolvable Association Type Name
        // </summary>
        InvalidAssociationType = Value + 6,

        // <summary>
        // Unresolvable Table Name
        // </summary>
        InvalidTable = Value + 7,

        // <summary>
        // Unresolvable Complex Type Name
        // </summary>
        InvalidComplexType = Value + 8,

        // <summary>
        // Unresolvable Edm Member Name
        // </summary>
        InvalidEdmMember = Value + 9,

        // <summary>
        // Unresolvable Storage Member Name
        // </summary>
        InvalidStorageMember = Value + 10,

        // <summary>
        // TableMappingFragment element expected
        // </summary>
        TableMappingFragmentExpected = Value + 11,

        // <summary>
        // SetMappingFragment element expected
        // </summary>
        SetMappingExpected = Value + 12,
        // Unused: 13
        // <summary>
        // Duplicate Set Map
        // </summary>
        DuplicateSetMapping = Value + 14,

        // <summary>
        // Duplicate Type Map
        // </summary>
        DuplicateTypeMapping = Value + 15,

        // <summary>
        // Condition Error
        // </summary>
        ConditionError = Value + 16,
        // Unused: 17
        // <summary>
        // Root Mapping Element missing
        // </summary>
        RootMappingElementMissing = Value + 18,

        // <summary>
        // Incompatible member map
        // </summary>
        IncompatibleMemberMapping = Value + 19,
        // Unused: 20
        // Unused: 21
        // Unused: 22
        // <summary>
        // Invalid Enum Value
        // </summary>
        InvalidEnumValue = Value + 23,

        // <summary>
        // Xml Schema Validation error
        // </summary>
        XmlSchemaParsingError = Value + 24,

        // <summary>
        // Xml Schema Validation error
        // </summary>
        XmlSchemaValidationError = Value + 25,

        // <summary>
        // Ambiguous Modification Function Mapping For AssociationSet
        // </summary>
        AmbiguousModificationFunctionMappingForAssociationSet = Value + 26,

        // <summary>
        // Missing Set Closure In Modification Function Mapping
        // </summary>
        MissingSetClosureInModificationFunctionMapping = Value + 27,

        // <summary>
        // Missing Modification Function Mapping For Entity Type
        // </summary>
        MissingModificationFunctionMappingForEntityType = Value + 28,

        // <summary>
        // Invalid Table Name Attribute With Modification Function Mapping
        // </summary>
        InvalidTableNameAttributeWithModificationFunctionMapping = Value + 29,

        // <summary>
        // Invalid Modification Function Mapping For Multiple Types
        // </summary>
        InvalidModificationFunctionMappingForMultipleTypes = Value + 30,

        // <summary>
        // Ambiguous Result Binding In Modification Function Mapping
        // </summary>
        AmbiguousResultBindingInModificationFunctionMapping = Value + 31,

        // <summary>
        // Invalid Association Set Role In Modification Function Mapping
        // </summary>
        InvalidAssociationSetRoleInModificationFunctionMapping = Value + 32,

        // <summary>
        // Invalid Association Set Cardinality In Modification Function Mapping
        // </summary>
        InvalidAssociationSetCardinalityInModificationFunctionMapping = Value + 33,

        // <summary>
        // Redundant Entity Type Mapping In Modification Function Mapping
        // </summary>
        RedundantEntityTypeMappingInModificationFunctionMapping = Value + 34,

        // <summary>
        // Missing Version In Modification Function Mapping
        // </summary>
        MissingVersionInModificationFunctionMapping = Value + 35,

        // <summary>
        // Invalid Version In Modification Function Mapping
        // </summary>
        InvalidVersionInModificationFunctionMapping = Value + 36,

        // <summary>
        // Invalid Parameter In Modification Function Mapping
        // </summary>
        InvalidParameterInModificationFunctionMapping = Value + 37,

        // <summary>
        // Parameter Bound Twice In Modification Function Mapping
        // </summary>
        ParameterBoundTwiceInModificationFunctionMapping = Value + 38,

        // <summary>
        // Same CSpace member mapped to multiple SSpace members with different types
        // </summary>
        CSpaceMemberMappedToMultipleSSpaceMemberWithDifferentTypes = Value + 39,

        // <summary>
        // No store type found for the given CSpace type (these error message is for primitive type with no facets)
        // </summary>
        NoEquivalentStorePrimitiveTypeFound = Value + 40,

        // <summary>
        // No Store type found for the given CSpace type with the given set of facets
        // </summary>
        NoEquivalentStorePrimitiveTypeWithFacetsFound = Value + 41,

        // <summary>
        // While mapping functions, if the property type is not compatible with the function parameter
        // </summary>
        InvalidModificationFunctionMappingPropertyParameterTypeMismatch = Value + 42,

        // <summary>
        // While mapping functions, if more than one end of association is mapped
        // </summary>
        InvalidModificationFunctionMappingMultipleEndsOfAssociationMapped = Value + 43,

        // <summary>
        // While mapping functions, if we find an unknown function
        // </summary>
        InvalidModificationFunctionMappingUnknownFunction = Value + 44,

        // <summary>
        // While mapping functions, if we find an ambiguous function
        // </summary>
        InvalidModificationFunctionMappingAmbiguousFunction = Value + 45,

        // <summary>
        // While mapping functions, if we find an invalid function
        // </summary>
        InvalidModificationFunctionMappingNotValidFunction = Value + 46,

        // <summary>
        // While mapping functions, if we find an invalid function parameter
        // </summary>
        InvalidModificationFunctionMappingNotValidFunctionParameter = Value + 47,

        // <summary>
        // Association set function mappings are not consistently defined for different operations
        // </summary>
        InvalidModificationFunctionMappingAssociationSetNotMappedForOperation = Value + 48,

        // <summary>
        // Entity type function mapping includes association end but the type is not part of the association
        // </summary>
        InvalidModificationFunctionMappingAssociationEndMappingInvalidForEntityType = Value + 49,

        // <summary>
        // Function import mapping references non-existent store function
        // </summary>
        MappingFunctionImportStoreFunctionDoesNotExist = Value + 50,

        // <summary>
        // Function import mapping references store function with overloads (overload resolution is not possible)
        // </summary>
        MappingFunctionImportStoreFunctionAmbiguous = Value + 51,

        // <summary>
        // Function import mapping reference non-existent import
        // </summary>
        MappingFunctionImportFunctionImportDoesNotExist = Value + 52,

        // <summary>
        // Function import mapping is mapped in several locations
        // </summary>
        MappingFunctionImportFunctionImportMappedMultipleTimes = Value + 53,

        // <summary>
        // Attempting to map non-composable function import to a composable function.
        // </summary>
        MappingFunctionImportTargetFunctionMustBeNonComposable = Value + 54,

        // <summary>
        // No parameter on import side corresponding to target parameter
        // </summary>
        MappingFunctionImportTargetParameterHasNoCorrespondingImportParameter = Value + 55,

        // <summary>
        // No parameter on target side corresponding to import parameter
        // </summary>
        MappingFunctionImportImportParameterHasNoCorrespondingTargetParameter = Value + 56,

        // <summary>
        // Parameter directions are different
        // </summary>
        MappingFunctionImportIncompatibleParameterMode = Value + 57,

        // <summary>
        // Parameter types are different
        // </summary>
        MappingFunctionImportIncompatibleParameterType = Value + 58,

        // <summary>
        // Rows affected parameter does not exist on mapped function
        // </summary>
        MappingFunctionImportRowsAffectedParameterDoesNotExist = Value + 59,

        // <summary>
        // Rows affected parameter does not Int32
        // </summary>
        MappingFunctionImportRowsAffectedParameterHasWrongType = Value + 60,

        // <summary>
        // Rows affected does not have 'out' mode
        // </summary>
        MappingFunctionImportRowsAffectedParameterHasWrongMode = Value + 61,

        // <summary>
        // Empty Container Mapping
        // </summary>
        EmptyContainerMapping = Value + 62,

        // <summary>
        // Empty Set Mapping
        // </summary>
        EmptySetMapping = Value + 63,

        // <summary>
        // Both TableName Attribute on Set Mapping and QueryView specified
        // </summary>
        TableNameAttributeWithQueryView = Value + 64,

        // <summary>
        // Empty Query View
        // </summary>
        EmptyQueryView = Value + 65,

        // <summary>
        // Both Query View and Property Maps specified for EntitySet
        // </summary>
        PropertyMapsWithQueryView = Value + 66,

        // <summary>
        // Some sets in the graph missing Query Views
        // </summary>
        MissingSetClosureInQueryViews = Value + 67,

        // <summary>
        // Invalid Query View
        // </summary>
        InvalidQueryView = Value + 68,

        // <summary>
        // Invalid result type  for query view
        // </summary>
        InvalidQueryViewResultType = Value + 69,

        // <summary>
        // Item with same name exists both in CSpace and SSpace
        // </summary>
        ItemWithSameNameExistsBothInCSpaceAndSSpace = Value + 70,

        // <summary>
        // Unsupported expression kind in query view
        // </summary>
        MappingUnsupportedExpressionKindQueryView = Value + 71,

        // <summary>
        // Non S-space target in query view
        // </summary>
        MappingUnsupportedScanTargetQueryView = Value + 72,

        // <summary>
        // Non structural property referenced in query view
        // </summary>
        MappingUnsupportedPropertyKindQueryView = Value + 73,

        // <summary>
        // Initialization non-target type in query view
        // </summary>
        MappingUnsupportedInitializationQueryView = Value + 74,

        // <summary>
        // EntityType mapping for non-entity set function
        // </summary>
        MappingFunctionImportEntityTypeMappingForFunctionNotReturningEntitySet = Value + 75,

        // <summary>
        // FunctionImport ambiguous type mappings
        // </summary>
        MappingFunctionImportAmbiguousTypeConditions = Value + 76,
        // MappingFunctionMultipleTypeConditionsForOneColumn = Value + 77,
        // <summary>
        // Abstract type being mapped explicitly  - not supported.
        // </summary>
        MappingOfAbstractType = Value + 78,

        // <summary>
        // Storage EntityContainer Name mismatch while specifying partial mapping
        // </summary>
        StorageEntityContainerNameMismatchWhileSpecifyingPartialMapping = Value + 79,

        // <summary>
        // TypeName attribute specified for First QueryView
        // </summary>
        TypeNameForFirstQueryView = Value + 80,

        // <summary>
        // No TypeName attribute is specified for type-specific QueryViews
        // </summary>
        NoTypeNameForTypeSpecificQueryView = Value + 81,

        // <summary>
        // Multiple (optype/oftypeonly) QueryViews have been defined for the same EntitySet/EntityType
        // </summary>
        QueryViewExistsForEntitySetAndType = Value + 82,

        // <summary>
        // TypeName Contains Multiple Types For QueryView
        // </summary>
        TypeNameContainsMultipleTypesForQueryView = Value + 83,

        // <summary>
        // IsTypeOf QueryView is specified for base type
        // </summary>
        IsTypeOfQueryViewForBaseType = Value + 84,

        // <summary>
        // ScalarProperty Element contains invalid type
        // </summary>
        InvalidTypeInScalarProperty = Value + 85,

        // <summary>
        // Already Mapped Storage Container
        // </summary>
        AlreadyMappedStorageEntityContainer = Value + 86,

        // <summary>
        // No query view is allowed at compile time in EntityContainerMapping
        // </summary>
        UnsupportedQueryViewInEntityContainerMapping = Value + 87,

        // <summary>
        // EntityContainerMapping only contains query view
        // </summary>
        MappingAllQueryViewAtCompileTime = Value + 88,

        // <summary>
        // No views can be generated since all of the EntityContainerMapping contain query view
        // </summary>
        MappingNoViewsCanBeGenerated = Value + 89,

        // <summary>
        // The store provider returns null EdmType for the given targetParameter's type
        // </summary>
        MappingStoreProviderReturnsNullEdmType = Value + 90,
        // MappingFunctionImportInvalidMemberName = Value + 91,
        // <summary>
        // Multiple mappings of the same Member or Property inside the same mapping fragment.
        // </summary>
        DuplicateMemberMapping = Value + 92,

        // <summary>
        // Entity type mapping for a function import that does not return a collection of entity type.
        // </summary>
        MappingFunctionImportUnexpectedEntityTypeMapping = Value + 93,

        // <summary>
        // Complex type mapping for a function import that does not return a collection of complex type.
        // </summary>
        MappingFunctionImportUnexpectedComplexTypeMapping = Value + 94,

        // <summary>
        // Distinct flag can only be placed in a container that is not read-write
        // </summary>
        DistinctFragmentInReadWriteContainer = Value + 96,

        // <summary>
        // The EntitySet used in creating the Ref and the EntitySet declared in AssociationSetEnd do not match
        // </summary>
        EntitySetMismatchOnAssociationSetEnd = Value + 97,

        // <summary>
        // FKs not permitted for function association ends.
        // </summary>
        InvalidModificationFunctionMappingAssociationEndForeignKey = Value + 98,
        // EdmItemCollectionVersionIncompatible = Value + 98,
        // StoreItemCollectionVersionIncompatible = Value + 99,
        // <summary>
        // Cannot load different version of schemas in the same ItemCollection
        // </summary>
        CannotLoadDifferentVersionOfSchemaInTheSameItemCollection = Value + 100,
        MappingDifferentMappingEdmStoreVersion = Value + 101,
        MappingDifferentEdmStoreVersion = Value + 102,

        // <summary>
        // All function imports must be mapped.
        // </summary>
        UnmappedFunctionImport = Value + 103,

        // <summary>
        // Invalid function import result mapping: return type property not mapped.
        // </summary>
        MappingFunctionImportReturnTypePropertyNotMapped = Value + 104,
        // AmbiguousFunction = Value + 105,
        // <summary>
        // Unresolvable Type Name
        // </summary>
        InvalidType = Value + 106,
        // FunctionResultMappingTypeMismatch = Value + 107,
        // <summary>
        // TVF expected on the store side.
        // </summary>
        MappingFunctionImportTVFExpected = Value + 108,

        // <summary>
        // Collection(Scalar) function import return type is not compatible with the TVF column type.
        // </summary>
        MappingFunctionImportScalarMappingTypeMismatch = Value + 109,

        // <summary>
        // Collection(Scalar) function import must be mapped to a TVF returning a single column.
        // </summary>
        MappingFunctionImportScalarMappingToMulticolumnTVF = Value + 110,

        // <summary>
        // Attempting to map composable function import to a non-composable function.
        // </summary>
        MappingFunctionImportTargetFunctionMustBeComposable = Value + 111,

        // <summary>
        // Non-s-space function call in query view.
        // </summary>
        UnsupportedFunctionCallInQueryView = Value + 112,

        // <summary>
        // Invalid function result mapping: result mapping count doesn't match result type count.
        // </summary>
        FunctionResultMappingCountMismatch = Value + 113,

        // <summary>
        // The key properties of all entity types returned by the function import must be mapped to the same non-nullable columns returned by the storage function.
        // </summary>
        MappingFunctionImportCannotInferTargetFunctionKeys = Value + 114,
    }
}