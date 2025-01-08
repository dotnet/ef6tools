// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb
{
   using System;
   using System.Collections.Generic;
   using System.Data.Entity.Core.Common;
   using System.Data.Entity.Core.Metadata.Edm;
   using System.Data.Entity.SqlServer;
   using System.Globalization;
   using Xunit;

   public partial class OneToOneMappingBuilderTests
    {
        private static readonly DbProviderManifest ProviderManifest =
            SqlProviderServices.Instance.GetProviderManifest("2008");

        public class BuildTests
        {
            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Build_creates_mapping_context_populated_with_items_created_from_store_model_items()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var storeEntitySet = EntitySet.Create("foo", "bar", null, null, storeEntityType, null);

                //var rowTypeProperty = CreateProperty("p1", PrimitiveTypeKind.Int32);

                //var storeFunction =
                //    EdmFunction.Create(
                //        "f", "bar", DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                IsComposable = true,
                //                IsFunctionImport = false,
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                RowType.Create(new[] { rowTypeProperty }, null).GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        },
                //            }, null);

                //var storeContainer =
                //    EntityContainer.Create("storeContainer", DataSpace.SSpace, new[] { storeEntitySet }, null, null);

                //var storeModel = EdmModel.CreateStoreModel(storeContainer, null, null, 3.0);
                //storeModel.AddItem(storeFunction);

                //var mappingContext =
                //    CreateOneToOneMappingBuilder(containerName: "edmContainer")
                //        .Build(storeModel);

                //Assert.NotNull(mappingContext);
                //Assert.Empty(mappingContext.Errors);
                //Assert.NotNull(mappingContext[storeEntitySet]);
                //Assert.NotNull(mappingContext[storeEntityType]);

                //var modelContainer = mappingContext[storeContainer];
                //Assert.NotNull(modelContainer);
                //Assert.Equal("edmContainer", modelContainer.Name);

                //var entitySet = modelContainer.EntitySets.Single();
                //Assert.Same(entitySet, mappingContext[storeEntitySet]);
                //Assert.Equal("foos", entitySet.Name);

                //var entityType = entitySet.ElementType;
                //Assert.Same(entityType, mappingContext[storeEntityType]);
                //Assert.Equal("foo", entityType.Name);

                //Assert.NotNull(mappingContext[rowTypeProperty]);
                //Assert.Equal("p1", mappingContext[rowTypeProperty].Name);

                //Assert.NotNull(mappingContext[storeFunction]);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Build_creates_mapping_context_with_container_with_function_imports_from_store_model()
            {
                //var storeFunction =
                //    EdmFunction.Create(
                //        "foo",
                //        "bar",
                //        DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32))
                //                                    .GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        }
                //            },
                //        null);

                //var storeFunction1 =
                //    EdmFunction.Create(
                //        "foo",
                //        "bar",
                //        DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32))
                //                                    .GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        }
                //            },
                //        null);

                //var storeModel = EdmModel.CreateStoreModel(
                //    EntityContainer.Create(
                //        "storeContainer",
                //        DataSpace.SSpace,
                //        null,
                //        null,
                //        null),
                //    null,
                //    null);

                //storeModel.AddItem(storeFunction);
                //storeModel.AddItem(storeFunction1);

                //var mappingContext =
                //    CreateOneToOneMappingBuilder(namespaceName: "myModel")
                //        .Build(storeModel);

                //Assert.NotNull(mappingContext);
                //var conceptualContainer = mappingContext[storeModel.Containers.Single()];
                //Assert.Equal(2, conceptualContainer.FunctionImports.Count);
                //Assert.Equal(new[] { "foo", "foo1" }, conceptualContainer.FunctionImports.Select(f => f.Name));
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Build_adds_lazyLoading_metadata_property_to_v2_and_v3_CSpace_containers()
            {
                //Assert.Null(GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version1));

                //var lazyLoadingMetadataProperty =
                //    GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version2);
                //Assert.NotNull(lazyLoadingMetadataProperty);
                //Assert.Equal("true", (string)lazyLoadingMetadataProperty.Value);

                //lazyLoadingMetadataProperty =
                //    GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version3);
                //Assert.NotNull(lazyLoadingMetadataProperty);
                //Assert.Equal("true", (string)lazyLoadingMetadataProperty.Value);
            }

            [Fact(Skip = "API Differences between official dll and locally built one")]
            private static MetadataProperty GetLazyLoadingMetadataProperty(Version targetSchemaVersion)
            {
                //    var storeModel =
                //        new EdmModel(
                //            DataSpace.SSpace, EntityFrameworkVersion.VersionToDouble(targetSchemaVersion));

                //    var mappingContext =
                //        new OneToOneMappingBuilder("ns", "container", null, true)
                //            .Build(storeModel);

                //    return GetAnnotationMetadataProperty(
                //        mappingContext.ConceptualContainers().Single(),
                //        "LazyLoadingEnabled");
                //}
                return null;
            }
        }

        public class GenerateEntitySetTests
        {
            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateEntitySet_creates_model_entity_set_for_store_entity_set()
            {
                //    var storeEntityType =
                //        EntityType.Create(
                //            "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //            new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //    var storeEntitySet = EntitySet.Create("foo", "bar", null, null, storeEntityType, null);

                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntitySet(
                //            mappingContext,
                //            storeEntitySet,
                //            new UniqueIdentifierService(),
                //            new UniqueIdentifierService());

                //    var conceptualModelEntitySet = mappingContext[storeEntitySet];

                //    Assert.Equal("foos", conceptualModelEntitySet.Name);
                //    Assert.Equal("foo", conceptualModelEntitySet.ElementType.Name);

                //    Assert.Same(conceptualModelEntitySet, mappingContext[storeEntitySet]);
                //
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateEntitySet_entity_set_name_sanitized_and_uniquified()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var storeEntitySet = EntitySet.Create("foo$", "bar", null, null, storeEntityType, null);

                //var uniqueEntityContainerNames = new UniqueIdentifierService();
                //uniqueEntityContainerNames.AdjustIdentifier("foo_");

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

                //CreateOneToOneMappingBuilder()
                //    .GenerateEntitySet(
                //        mappingContext,
                //        storeEntitySet,
                //        uniqueEntityContainerNames,
                //        new UniqueIdentifierService());

                //var conceptualModelEntitySet = mappingContext[storeEntitySet];

                //Assert.Equal("foo_1", conceptualModelEntitySet.Name);
                //Assert.Equal("foo", conceptualModelEntitySet.ElementType.Name);
            }
        }

        public class GenerateEntityTypeTests
        {
            [Fact(Skip = "Different API   Visibility between official dll and locally built one")]
            public void GenerateEntityType_creates_CSpace_entity_from_SSpace_entity()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id1", "Id2" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id1", GetStoreEdmType("int")),
                //                EdmProperty.CreatePrimitive("Id2", GetStoreEdmType("int")),
                //                EdmProperty.CreatePrimitive("Name_", GetStoreEdmType("nvarchar")),
                //                EdmProperty.CreatePrimitive("Name$", GetStoreEdmType("nvarchar"))
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder().GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(storeEntityType.Name, conceptualEntityType.Name);
                //Assert.Equal("myModel", conceptualEntityType.NamespaceName);
                //Assert.Equal(
                //    new[] { "Id1", "Id2", "Name_", "Name_1" },
                //    conceptualEntityType.Properties.Select(p => p.Name).ToArray());

                //Assert.Equal(
                //    storeEntityType.KeyMembers.Select(p => p.Name),
                //    conceptualEntityType.KeyMembers.Select(p => p.Name).ToArray());

                //Assert.Same(conceptualEntityType, mappingContext[storeEntityType]);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateEntityType_renames_property_whose_name_is_the_same_as_owning_entity_type()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "foo" },
                //        new[] { EdmProperty.CreatePrimitive("foo", GetStoreEdmType("int")) }, null);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            new UniqueIdentifierService());

                //Assert.Equal(storeEntityType.Name, conceptualEntityType.Name);
                //Assert.Equal(
                //    new[] { "foo1" },
                //    conceptualEntityType.Properties.Select(p => p.Name).ToArray());
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built one")]
            public void GenerateEntityType_entity_type_name_is_sanitized_and_uniquified()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo$", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var uniqueEntityTypeName = new UniqueIdentifierService();
                //uniqueEntityTypeName.AdjustIdentifier("foo_");
                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            uniqueEntityTypeName);

                //Assert.Equal("foo_1", conceptualEntityType.Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built one")]
            public void GenerateEntityType_singularizes_entity_type_name()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "Entities", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            new UniqueIdentifierService());

                //Assert.Equal("Entity", conceptualEntityType.Name);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_added_if_foreign_keys_enabled()
            {
                //var foreignKeyColumn = EdmProperty.CreatePrimitive("ForeignKeyColumn", GetStoreEdmType("int"));

                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")),
                //                foreignKeyColumn,
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(foreignKeyColumn);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: true)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "Id", "ForeignKeyColumn" }, conceptualEntityType.Properties.Select(p => p.Name));
                //Assert.False(storeEntityType.Properties.Any(p => mappingContext[p] == null));
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_not_added_if_property_is_not_key_and_foreign_keys_disabled()
            {
                //var foreignKeyColumn = EdmProperty.CreatePrimitive("ForeignKeyColumn", GetStoreEdmType("int"));

                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")),
                //                foreignKeyColumn,
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(foreignKeyColumn);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: false)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "Id" }, conceptualEntityType.Properties.Select(p => p.Name));

                //// the mapping still should be added to be able to build association type mapping correctly
                //Assert.False(storeEntityType.Properties.Any(p => mappingContext[p] == null));
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_added_if_property_is_key_even_when_foreign_keys_disabled()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "IdPrimaryAndForeignKey" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("IdPrimaryAndForeignKey", GetStoreEdmType("int")),
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(storeEntityType.Properties.Single());

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: false)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "IdPrimaryAndForeignKey" }, conceptualEntityType.Properties.Select(p => p.Name));

                //// the mapping still should be added to be able to build association type mapping correctly
                //Assert.False(storeEntityType.Properties.Any(p => mappingContext[p] == null));
            }
        }

        public class GenerateScalarPropertyTests
        {
            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_creates_CSpace_property_from_SSpace_property()
            {
                //var storeProperty = EdmProperty.CreatePrimitive("p1", GetStoreEdmType("int"));

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(mappingContext, storeProperty, new UniqueIdentifierService());

                //Assert.Equal(conceptualProperty.Name, storeProperty.Name);
                //Assert.Equal(conceptualProperty.TypeUsage.EdmType, PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
                //Assert.Null(GetAnnotationMetadataProperty(conceptualProperty, "StoreGeneratedPattern"));
                //Assert.NotNull(mappingContext[storeProperty]);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_adds_StoreGeneratedPattern_annotation_if_needed()
            {
                //var storeProperty = EdmProperty.CreatePrimitive("p1", GetStoreEdmType("int"));
                //storeProperty.StoreGeneratedPattern = StoreGeneratedPattern.Identity;

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(mappingContext, storeProperty, new UniqueIdentifierService());

                //Assert.Equal(conceptualProperty.Name, storeProperty.Name);
                //Assert.Equal(conceptualProperty.TypeUsage.EdmType, PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
                //var storeGeneratedPatternMetadataProperty =
                //    GetAnnotationMetadataProperty(conceptualProperty, "StoreGeneratedPattern");
                //Assert.NotNull(storeGeneratedPatternMetadataProperty);
                //Assert.Equal("Identity", storeGeneratedPatternMetadataProperty.Value);
                //Assert.NotNull(mappingContext[storeProperty]);
            }

            [Fact(Skip = "Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_converts_and_uniquifies_property_names()
            {
                //var uniquePropertyNameService = new UniqueIdentifierService();
                //uniquePropertyNameService.AdjustIdentifier("p_1");

                //var storeProperty = EdmProperty.CreatePrimitive("p*1", GetStoreEdmType("int"));

                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeProperty,
                //            uniquePropertyNameService);

                //Assert.Equal("p_11", conceptualProperty.Name);
            }
        }
    }
}
