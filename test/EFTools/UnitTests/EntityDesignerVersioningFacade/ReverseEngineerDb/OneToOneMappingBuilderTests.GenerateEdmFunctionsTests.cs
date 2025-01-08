// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb
{
   using System;
   using System.Data.Entity.Core.Metadata.Edm;
   using System.Data.Entity.Infrastructure.Pluralization;
   using System.Linq;
   using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
   using Xunit;

   public partial class OneToOneMappingBuilderTests
   {
      public class GenerateEdmFunctionsTests
      {
         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void CreateFunctionImportParameters_creates_function_import_parameters_from_store_function_parameters() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("stringParam", PrimitiveTypeKind.String, ParameterMode.In)
            //        };

            //var returnParameters =
            //    new[]
            //        {
            //            FunctionParameter.Create(
            //                "ReturnType",
            //                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                ParameterMode.ReturnValue)
            //        };

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = returnParameters
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImportParameters =
            //    OneToOneMappingBuilder.CreateFunctionImportParameters(mappingContext, storeFunction);

            //Assert.NotNull(functionImportParameters);
            //Assert.Equal(storeFunctionParameters.Select(p => p.Name), functionImportParameters.Select(p => p.Name));
            //Assert.Equal(
            //    new[] { "Edm.Int32", "Edm.String" },
            //    functionImportParameters.Select(p => p.TypeUsage.EdmType.FullName));
            //Assert.True(functionImportParameters.All(p => p.Mode == ParameterMode.In));
            //Assert.Empty(mappingContext.Errors);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void CreateFunctionImportParameters_returns_null_if_store_function_parameters_had_to_be_renamed() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam_", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("numberParam*", PrimitiveTypeKind.Int32, ParameterMode.In)
            //        };

            //var returnParameters =
            //    new[]
            //        {
            //            FunctionParameter.Create(
            //                "ReturnType",
            //                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                ParameterMode.ReturnValue)
            //        };

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = returnParameters
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImportParameters =
            //    OneToOneMappingBuilder.CreateFunctionImportParameters(mappingContext, storeFunction);

            //Assert.Null(functionImportParameters);
            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    String.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.UnableToGenerateFunctionImportParameterName,
            //        "numberParam*",
            //        "foo"),
            //    mappingContext.Errors.Single().Message);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportParameterName,
            //    mappingContext.Errors.Single().ErrorCode);
            //Assert.Equal(EdmSchemaErrorSeverity.Warning, mappingContext.Errors.Single().Severity);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_has_no_return_type() {
            //var storeFunction = EdmFunction.Create("foo", "bar", DataSpace.SSpace, new EdmFunctionPayload(), null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Null(OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction));

            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_return_type_is_not_collection() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        ProviderManifest.GetStoreTypes().First(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Null(OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction));
            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_return_type_is_not_rowType_collection() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        ProviderManifest.GetStoreTypes().First().GetCollectionType(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Null(OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction));
            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_function_return_edm_type_if_return_type_valid() {
            //var rowType = RowType.Create(new EdmProperty[0], null);

            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        rowType.GetCollectionType(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Same(
            //    rowType,
            //    OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction));
            //Assert.Empty(mappingContext.Errors);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_creates_CSpace_ComplexType_from_valid_SSpace_RowType() {
            //var rowType =
            //    CreateRowType(
            //        CreateProperty("number", PrimitiveTypeKind.Int32),
            //        CreateProperty("string", PrimitiveTypeKind.String));

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(mappingContext, rowType, "foo");

            //Assert.NotNull(complexType);
            //Assert.Equal("bar.foo", complexType.FullName);
            //Assert.Equal(new[] { "number", "string" }, complexType.Properties.Select(p => p.Name));
            //Assert.True(complexType.Properties.All(p => p.TypeUsage.EdmType.NamespaceName == "Edm"));
            //Assert.Equal(complexType.Properties, rowType.Properties.Select(p => mappingContext[p]));
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_uniquifies_property_names() {
            //var rowType =
            //    CreateRowType(
            //        CreateProperty("number_", PrimitiveTypeKind.Int32),
            //        CreateProperty("number*", PrimitiveTypeKind.Int32));

            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(
            //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
            //            rowType,
            //            "foo");

            //Assert.NotNull(complexType);
            //Assert.Equal(new[] { "number_", "number_1" }, complexType.Properties.Select(p => p.Name));
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_does_not_create_properties_with_names_of_owning_type() {
            //var rowType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32));

            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(
            //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
            //            rowType,
            //            "foo");

            //Assert.NotNull(complexType);
            //Assert.Equal("foo1", complexType.Properties.Select(p => p.Name).Single());
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_null_if_return_type_is_not_valid_TVF_return_type() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Null(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(
            //            mappingContext, storeFunction, new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));

            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    String.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.UnableToGenerateFunctionImportReturnType,
            //        "foo"),
            //    mappingContext.Errors.Single().Message);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
            //Assert.Equal(EdmSchemaErrorSeverity.Warning, mappingContext.Errors.Single().Severity);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void
             GenerateFunction_returns_null_if_function_import_parameter_name_different_from_corresponding_store_function_parameter_name
             () {
            //var parameter = CreatePrimitiveParameter("numberParam*", PrimitiveTypeKind.Int32, ParameterMode.In);
            //var storeReturnType =
            //    CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);
            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = new[] { parameter },
            //                ReturnParameters = new[] { returnParameter }
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(
            //            mappingContext,
            //            storeFunction,
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService());

            //Assert.Null(functionImport);
            //Assert.Equal(1, mappingContext.Errors.Count);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportParameterName,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_import_for_supported_store_function() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("stringParam", PrimitiveTypeKind.String, ParameterMode.In)
            //        };
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = new[] { returnParameter }
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, new UniqueIdentifierService(), new UniqueIdentifierService());

            //Assert.Empty(mappingContext.Errors);

            //Assert.NotNull(functionImport);
            //Assert.Equal("myModel.foo", functionImport.FullName);

            //Assert.Equal(storeFunctionParameters.Select(p => p.Name), functionImport.Parameters.Select(p => p.Name));

            //Assert.Equal("ReturnType", functionImport.ReturnParameter.Name);
            //var returnType = ((CollectionType)functionImport.ReturnParameter.TypeUsage.EdmType);
            //Assert.IsType<ComplexType>(returnType.TypeUsage.EdmType);
            //Assert.Equal("myModel.foo_Result", returnType.TypeUsage.EdmType.FullName);
            //Assert.True(functionImport.IsComposableAttribute);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_with_unique_name() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var uniqueContainerNames = new UniqueIdentifierService();
            //uniqueContainerNames.AdjustIdentifier("foo_");

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, uniqueContainerNames, new UniqueIdentifierService());

            //Assert.NotNull(functionImport);
            //Assert.Equal("myModel.foo_1", functionImport.FullName);
            //Assert.Empty(mappingContext.Errors);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_with_unique_return_type_name() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var uniqueContainerNames = new UniqueIdentifierService();
            //uniqueContainerNames.AdjustIdentifier("foo_");

            //var globallyUniqueTypeNames = new UniqueIdentifierService();
            //globallyUniqueTypeNames.AdjustIdentifier("foo_1_Result");

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, uniqueContainerNames, globallyUniqueTypeNames);

            //Assert.NotNull(functionImport);
            //Assert.Equal(
            //    "myModel.foo_1_Result1",
            //    ((CollectionType)functionImport.ReturnParameter.TypeUsage.EdmType).TypeUsage.EdmType.FullName);
            //Assert.Empty(mappingContext.Errors);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_functions_for_non_v3_schema_versions() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Empty(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(EntityFrameworkVersion.Version1, storeFunction),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));
            //Assert.Empty(mappingContext.MappedStoreFunctions());
            //Assert.Empty(mappingContext.Errors);

            //Assert.Empty(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(EntityFrameworkVersion.Version2, storeFunction),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));
            //Assert.Empty(mappingContext.MappedStoreFunctions());
            //Assert.Empty(mappingContext.Errors);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_functions_for_aggregate_or_non_composable_store_function() {
            //var testCases =
            //    new[]
            //        {
            //            new { IsComposable = true, IsAggregate = true },
            //            new { IsComposable = false, IsAggregate = false },
            //            new { IsComposable = false, IsAggregate = true }
            //        };

            //foreach (var testCase in testCases)
            //{
            //    var storeReturnType =
            //        CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //    var returnParameter =
            //        FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload
            //                {
            //                    ReturnParameters = new[] { returnParameter },
            //                    IsComposable = testCase.IsComposable,
            //                    IsAggregate = testCase.IsAggregate
            //                },
            //            null);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                CreateStoreModel(storeFunction),
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));

            //    Assert.Empty(mappingContext.Errors);
            //}
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_function_imports_if_store_function_has_non_IN_parameter() {
            //foreach (var parameterMode in new[] { ParameterMode.InOut, ParameterMode.Out })
            //{
            //    var parameters =
            //        new[]
            //            {
            //                CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, parameterMode),
            //                CreatePrimitiveParameter("numberParam1", PrimitiveTypeKind.Int32, ParameterMode.In)
            //            };

            //    var storeReturnType =
            //        CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //    var returnParameter =
            //        FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload
            //                {
            //                    Parameters = parameters,
            //                    ReturnParameters = new[] { returnParameter },
            //                    IsComposable = true,
            //                    IsAggregate = false
            //                },
            //            null);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                CreateStoreModel(storeFunction),
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));
            //    Assert.Empty(mappingContext.Errors);
            //}
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_generates_functions_for_valid_store_functions() {
            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters =
            //                    new[]
            //                        {
            //                            CreatePrimitiveParameter(
            //                                "numberParam", PrimitiveTypeKind.Int32, ParameterMode.In)
            //                        },
            //                ReturnParameters =
            //                    new[]
            //                        {
            //                            FunctionParameter.Create(
            //                                "ReturnType",
            //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                                ParameterMode.ReturnValue)
            //                        },
            //                IsComposable = true,
            //                IsAggregate = false
            //            },
            //        null);

            //var storeFunction1 =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters =
            //                    new[]
            //                        {
            //                            CreatePrimitiveParameter(
            //                                "numberParam", PrimitiveTypeKind.Int32, ParameterMode.In)
            //                        },
            //                ReturnParameters =
            //                    new[]
            //                        {
            //                            FunctionParameter.Create(
            //                                "ReturnType",
            //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                                ParameterMode.ReturnValue)
            //                        },
            //                IsComposable = true,
            //                IsAggregate = false
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

            //Assert.Equal(
            //    2,
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(storeFunction, storeFunction1),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService())
            //        .Count());
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_add_invalid_functions_to_mapping_context() {
            //    var returnParameter =
            //        FunctionParameter.Create(
            //            "ReturnType",
            //            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32),
            //            ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //            null);

            //    var storeModel = new EdmModel(DataSpace.SSpace);
            //    storeModel.AddItem(storeFunction);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                storeModel,
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));

            //    Assert.Equal(1, mappingContext.Errors.Count);
            //    Assert.Empty(mappingContext.MappedStoreFunctions());
            //}
         }

         private static OneToOneMappingBuilder CreateOneToOneMappingBuilder(
             string namespaceName = "myModel",
             string containerName = "myContainer",
             bool generateForeignKeyProperties = true) {
            return new OneToOneMappingBuilder(
                namespaceName,
                containerName,
                new EnglishPluralizationService(),
                generateForeignKeyProperties);
         }

         [Fact(Skip = "Different API Visibility between official dll and locally built")]
         // When in doubt, use targetSchemaVersion == EntityFrameworkVersion.Version3
         private static EdmModel CreateStoreModel(Version targetSchemaVersion, params EdmFunction[] functions) {
            //var storeModel = new EdmModel(
            //    DataSpace.SSpace,
            //    EntityFrameworkVersion.VersionToDouble(targetSchemaVersion));

            //foreach (var function in functions)
            //{
            //    storeModel.AddItem(function);
            //}
            //return storeModel;

            return null;
         }

         private static PrimitiveType GetStoreEdmType(string typeName) {
            return ProviderManifest.GetStoreTypes().Single(t => t.Name == typeName);
         }

         private static EdmProperty CreateProperty(string propertyName, PrimitiveTypeKind propertyType) {
            return EdmProperty.Create(
                propertyName,
                ProviderManifest.GetStoreType(
                    TypeUsage.CreateDefaultTypeUsage(
                        PrimitiveType.GetEdmPrimitiveType(propertyType))));
         }

         private static RowType CreateRowType(params EdmProperty[] properties) {
            return RowType.Create(properties, null);
         }

         private static FunctionParameter CreatePrimitiveParameter(string name, PrimitiveTypeKind type, ParameterMode mode) {
            return
                FunctionParameter.Create(
                    name,
                    ProviderManifest.GetStoreType(
                        TypeUsage.CreateDefaultTypeUsage(
                            PrimitiveType.GetEdmPrimitiveType(type))).EdmType,
                    mode);
         }

         public class AssociationSetsTests
         {
            private static EdmModel BuildStoreModel(
                TableDetailsRow[] tableDetails,
                RelationshipDetailsRow[] relationshipDetails) {
               var storeSchemaDetails = new StoreSchemaDetails(
                   tableDetails,
                   new TableDetailsRow[0],
                   relationshipDetails,
                   new FunctionDetailsRowView[0],
                   new TableDetailsRow[0]);

               var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder();

               return storeModelBuilder.Build(storeSchemaDetails);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public static void CreateCollapsibleItems_creates_collapsible_item() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(1, collapsibleItems.Count());
               //Assert.Equal(0, associationSetsFromNonCollapsibleItems.Count());

               //var item = collapsibleItems.FirstOrDefault();
               //Assert.Equal("C", item.EntitySet.Name);
               //Assert.Equal(2, item.AssociationSets.Count);
               //Assert.Equal("R1", item.AssociationSets[0].Name);
               //Assert.Equal("R2", item.AssociationSets[1].Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_not_IsEntityDependentSideOfBothAssociations() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public static void
            CreateCollapsibleItems_does_not_create_collapsible_item_if_not_IsAtLeastOneColumnOfBothDependentRelationshipColumnSetsNonNullable
            () {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "CId", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: false),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, true, "int", isIdentity: false, isPrimaryKey: false)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]

            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_not_AreAllEntityColumnsMappedAsToColumns() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col2", 2, false, "int", isIdentity: false, isPrimaryKey: false)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]

            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_IsAtLeastOneColumnFkInBothAssociations() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col2", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col2", 2, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 1, false, "catalog", "schema", "A", "Col1", "catalog", "schema", "C", "Col1"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 1, false, "catalog", "schema", "B", "Col2", "catalog", "schema", "C", "Col2")
               //    };

               //var storeSchemaDetails = new StoreSchemaDetails(
               //    tableDetails,
               //    new TableDetailsRow[0],
               //    relationshipDetails,
               //    new FunctionDetailsRowView[0],
               //    new TableDetailsRow[0]);

               //var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder(
               //    "System.Data.SqlClient",
               //    "2008",
               //    null,
               //    "myModel",
               //    generateForeignKeyProperties: true);

               //var storeModel = storeModelBuilder.Build(storeSchemaDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]

            public static void GenerateAssociationSets_from_store_association_sets_creates_expected_mappings() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               Assert.Equal(1, mappingContext.StoreAssociationTypes().Count());
               Assert.Equal(1, mappingContext.StoreAssociationSets().Count());
               Assert.Equal(2, mappingContext.StoreAssociationEndMembers().Count());
               Assert.Equal(2, mappingContext.StoreAssociationSetEnds().Count());

               var storeAssociationType = mappingContext.StoreAssociationTypes().ElementAt(0);
               var storeAssociationSet = mappingContext.StoreAssociationSets().ElementAt(0);
               var storeAssociationEndMember0 = mappingContext.StoreAssociationEndMembers().ElementAt(0);
               var storeAssociationEndMember1 = mappingContext.StoreAssociationEndMembers().ElementAt(1);
               var storeAssociationSetEnd0 = mappingContext.StoreAssociationSetEnds().ElementAt(0);
               var storeAssociationSetEnd1 = mappingContext.StoreAssociationSetEnds().ElementAt(1);

               Assert.Same(mappingContext[storeAssociationType], mappingContext.ConceptualAssociationTypes().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationSet], mappingContext.ConceptualAssociationSets().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationEndMember0], mappingContext.ConceptualAssociationEndMembers().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationEndMember1], mappingContext.ConceptualAssociationEndMembers().ElementAt(1));
               Assert.Same(mappingContext[storeAssociationSetEnd0], mappingContext.ConceptualAssociationSetEnds().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationSetEnd1], mappingContext.ConceptualAssociationSetEnds().ElementAt(1));
               Assert.Equal(
                   new[] { storeAssociationType.ReferentialConstraints[0].ToProperties[0] },
                   mappingContext.StoreForeignKeyProperties);
            }

            [Fact]
            public static void GenerateAssociationSets_from_collapsible_items_creates_expected_mappings() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               Assert.Equal(0, mappingContext.StoreAssociationTypes().Count());
               Assert.Equal(0, mappingContext.StoreAssociationSets().Count());
               Assert.Equal(2, mappingContext.StoreAssociationEndMembers().Count());
               Assert.Equal(2, mappingContext.StoreAssociationSetEnds().Count());

               var storeAssociationEndMember0 = mappingContext.StoreAssociationEndMembers().ElementAt(0);
               var storeAssociationEndMember1 = mappingContext.StoreAssociationEndMembers().ElementAt(1);
               var storeAssociationSetEnd0 = mappingContext.StoreAssociationSetEnds().ElementAt(0);
               var storeAssociationSetEnd1 = mappingContext.StoreAssociationSetEnds().ElementAt(1);

               Assert.Same(mappingContext[storeAssociationEndMember0], mappingContext.ConceptualAssociationEndMembers().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationEndMember1], mappingContext.ConceptualAssociationEndMembers().ElementAt(1));
               Assert.Same(mappingContext[storeAssociationSetEnd0], mappingContext.ConceptualAssociationSetEnds().ElementAt(0));
               Assert.Same(mappingContext[storeAssociationSetEnd1], mappingContext.ConceptualAssociationSetEnds().ElementAt(1));

               // the mapping for the collapsed entity set should be replaced with a mapping to a conceptual association set
               Assert.True(mappingContext.ConceptualEntitySets().All(e => e.Name != "C"));
               Assert.Equal(1, mappingContext.ConceptualAssociationSets().Count());

               Assert.Equal(new[] { "Id", "Col1" }, mappingContext.StoreForeignKeyProperties.Select(p => p.Name));
            }

            [Fact]
            public static void GenerateAssociationSets_from_store_association_sets_creates_expected_instances() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               Assert.Equal(1, mappingContext.ConceptualAssociationTypes().Count());
               Assert.Equal(1, mappingContext.ConceptualAssociationSets().Count());
               Assert.Equal(2, mappingContext.ConceptualAssociationEndMembers().Count());
               Assert.Equal(2, mappingContext.ConceptualAssociationSetEnds().Count());

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);
               var associationSet = mappingContext.ConceptualAssociationSets().ElementAt(0);
               var associationEndMember0 = mappingContext.ConceptualAssociationEndMembers().ElementAt(0);
               var associationEndMember1 = mappingContext.ConceptualAssociationEndMembers().ElementAt(1);
               var associationSetEnd0 = mappingContext.ConceptualAssociationSetEnds().ElementAt(0);
               var associationSetEnd1 = mappingContext.ConceptualAssociationSetEnds().ElementAt(1);

               Assert.Equal("R1", associationType.Name);
               Assert.True(associationType.IsForeignKey);
               Assert.Equal(1, associationType.ReferentialConstraints.Count);
               Assert.Equal(2, associationType.AssociationEndMembers.Count);

               var constraint = associationType.ReferentialConstraints[0];
               Assert.Equal(associationEndMember0, constraint.FromRole);
               Assert.Equal(associationEndMember1, constraint.ToRole);

               Assert.Equal("A", associationEndMember0.Name);
               Assert.Equal(RelationshipMultiplicity.One, associationEndMember0.RelationshipMultiplicity);
               Assert.Equal(OperationAction.None, associationEndMember0.DeleteBehavior);
               Assert.Same(associationEndMember0, associationType.AssociationEndMembers[0]);

               Assert.Equal("B", associationEndMember1.Name);
               Assert.Equal(RelationshipMultiplicity.ZeroOrOne, associationEndMember1.RelationshipMultiplicity);
               Assert.Equal(OperationAction.None, associationEndMember1.DeleteBehavior);
               Assert.Same(associationEndMember1, associationType.AssociationEndMembers[1]);

               Assert.Equal("R1", associationSet.Name);
               Assert.Equal(2, associationSet.AssociationSetEnds.Count);

               Assert.Equal("A", associationSetEnd0.Name);
               Assert.Same(associationSetEnd0, associationSet.AssociationSetEnds[0]);
               Assert.Equal("B", associationSetEnd1.Name);
               Assert.Same(associationSetEnd1, associationSet.AssociationSetEnds[1]);

               Assert.Equal(
                   new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] },
                   mappingContext.StoreForeignKeyProperties);
            }

            [Fact]
            public static void GenerateAssociationSets_from_collapsible_items_creates_expected_instances() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               Assert.Equal(1, mappingContext.ConceptualContainers().Count());
               Assert.Equal(0, mappingContext.ConceptualAssociationTypes().Count());
               Assert.Equal(0, mappingContext.ConceptualAssociationTypes().Count());
               Assert.Equal(1, mappingContext.ConceptualAssociationSets().Count());
               Assert.Equal(2, mappingContext.ConceptualAssociationEndMembers().Count());
               Assert.Equal(2, mappingContext.ConceptualAssociationSetEnds().Count());

               var container = mappingContext.ConceptualContainers().ElementAt(0);
               Assert.Equal(1, container.AssociationSets.Count());

               var associationSet = container.AssociationSets.ElementAt(0);
               var associationType = associationSet.ElementType;
               var associationEndMember0 = mappingContext.ConceptualAssociationEndMembers().ElementAt(0);
               var associationEndMember1 = mappingContext.ConceptualAssociationEndMembers().ElementAt(1);
               var associationSetEnd0 = mappingContext.ConceptualAssociationSetEnds().ElementAt(0);
               var associationSetEnd1 = mappingContext.ConceptualAssociationSetEnds().ElementAt(1);

               Assert.Equal("C", associationType.Name);
               Assert.False(associationType.IsForeignKey);
               Assert.Equal(0, associationType.ReferentialConstraints.Count);
               Assert.Equal(2, associationType.AssociationEndMembers.Count);

               Assert.Equal("A", associationEndMember0.Name);
               Assert.Equal(RelationshipMultiplicity.Many, associationEndMember0.RelationshipMultiplicity);
               Assert.Equal(OperationAction.None, associationEndMember0.DeleteBehavior);
               Assert.Same(associationEndMember0, associationType.AssociationEndMembers[0]);

               Assert.Equal("B", associationEndMember1.Name);
               Assert.Equal(RelationshipMultiplicity.Many, associationEndMember1.RelationshipMultiplicity);
               Assert.Equal(OperationAction.None, associationEndMember1.DeleteBehavior);
               Assert.Same(associationEndMember1, associationType.AssociationEndMembers[1]);

               Assert.Equal("C", associationSet.Name);
               Assert.Equal(2, associationSet.AssociationSetEnds.Count);

               Assert.Equal("A", associationSetEnd0.Name);
               Assert.Same(associationSetEnd0, associationSet.AssociationSetEnds[0]);
               Assert.Equal("B", associationSetEnd1.Name);
               Assert.Same(associationSetEnd1, associationSet.AssociationSetEnds[1]);

               Assert.Equal(new[] { "Id", "Col1" }, mappingContext.StoreForeignKeyProperties.Select(p => p.Name));
            }

            [Fact]
            public static void GenerateAssociationType_from_store_association_type_creates_non_FK_association_if_EF_version1() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

               var storeSchemaDetails = new StoreSchemaDetails(
                   tableDetails,
                   new TableDetailsRow[0],
                   relationshipDetails,
                   new FunctionDetailsRowView[0],
                   new TableDetailsRow[0]);

               var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder(
                   "System.Data.SqlClient",
                   "2008",
                   EntityFrameworkVersion.Version1);

               var storeModel = storeModelBuilder.Build(storeSchemaDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               Assert.Equal(1, mappingContext.ConceptualAssociationTypes().Count());

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);

               Assert.False(associationType.IsForeignKey);
               Assert.Equal(1, associationType.ReferentialConstraints.Count);

               Assert.Equal(
                   new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] },
                   mappingContext.StoreForeignKeyProperties);
            }

            [Fact]
            public static void
                GenerateAssociationType_from_store_association_type_creates_non_FK_association_if_does_not_require_referential_constraint() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: false)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder(generateForeignKeyProperties: false).Build(storeModel);

               Assert.Equal(1, mappingContext.ConceptualAssociationTypes().Count());

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);

               Assert.False(associationType.IsForeignKey);
               Assert.Equal(0, associationType.ReferentialConstraints.Count);

               Assert.Equal(
                   new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] },
                   mappingContext.StoreForeignKeyProperties);
            }
         }

         private static MetadataProperty GetAnnotationMetadataProperty(MetadataItem item, string metadataPropertyName) {
            return item
                .MetadataProperties
                .SingleOrDefault(
                    p => p.Name == "http://schemas.microsoft.com/ado/2009/02/edm/annotation:" + metadataPropertyName);
         }
      }
   }
}
