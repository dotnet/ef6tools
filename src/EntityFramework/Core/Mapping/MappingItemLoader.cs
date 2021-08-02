// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.SchemaObjectModel;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.XPath;
    using EntityContainer = System.Data.Entity.Core.Metadata.Edm.EntityContainer;
    using Triple =
        System.Data.Entity.Core.Common.Utils.Pair<Metadata.Edm.EntitySetBase, Common.Utils.Pair<Metadata.Edm.EntityTypeBase, bool>>;

    // <summary>
    // The class loads an MSL file into memory and exposes CSMappingMetadata interfaces.
    // The primary consumers of the interfaces are view genration and tools.
    // </summary>
    // <example>
    // For Example if conceptually you could represent the CS MSL file as following
    // --Mapping
    // --EntityContainerMapping ( CNorthwind-->SNorthwind )
    // --EntitySetMapping
    // --EntityTypeMapping
    // --TableMappingFragment
    // --EntityKey
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --DiscriminatorProperyMap ( constant value-->SMemberMetadata )
    // --EntityTypeMapping
    // --TableMappingFragment
    // --EntityKey
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ComplexPropertyMap
    // --ComplexTypeMap
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ScalarProperyMap ( CMemberMetadata-->SMemberMetadata )
    // --DiscriminatorProperyMap ( constant value-->SMemberMetadata )
    // --AssociationSetMapping
    // --AssociationTypeMapping
    // --TableMappingFragment
    // --EndPropertyMap
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ScalarProperyMap ( CMemberMetadata-->SMemberMetadata )
    // --EndPropertyMap
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --EntityContainerMapping ( CMyDatabase-->SMyDatabase )
    // --CompositionSetMapping
    // --CompositionTypeMapping
    // --TableMappingFragment
    // --ParentEntityKey
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --EntityKey
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --ScalarPropertyMap ( CMemberMetadata-->Constant value )
    // --ComplexPropertyMap
    // --ComplexTypeMap
    // --ScalarPropertyMap ( CMemberMetadata-->SMemberMetadata )
    // --DiscriminatorProperyMap ( constant value-->SMemberMetadata )
    // --ScalarPropertyMap ( CMemberMetadata-->Constant value )
    // The CCMappingSchemaLoader loads an Xml file that has a conceptual structure
    // equivalent to the above example into in-memory data structure in a
    // top-dwon approach.
    // </example>
    // <remarks>
    // The loader uses XPathNavigator to parse the XML. The advantage of using XPathNavigator
    // over DOM is that it exposes the line number of the current xml content.
    // This is really helpful when throwing exceptions. Another advantage is
    // </remarks>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal class MappingItemLoader
    {
        // <summary>
        // Public constructor.
        // For Beta2 we wont support delay loading Mapping information and we would also support
        // only one mapping file for workspace.
        // </summary>
        // <param name="scalarMemberMappings"> Dictionary to keep the list of all scalar member mappings </param>
        internal MappingItemLoader(
            XmlReader reader, StorageMappingItemCollection storageMappingItemCollection, string fileName,
            Dictionary<EdmMember, KeyValuePair<TypeUsage, TypeUsage>> scalarMemberMappings)
        {
            DebugCheck.NotNull(storageMappingItemCollection);
            DebugCheck.NotNull(scalarMemberMappings);

            m_storageMappingItemCollection = storageMappingItemCollection;
            m_alias = new Dictionary<string, string>(StringComparer.Ordinal);
            //The fileName field in this class will always have absolute path since
            //StorageMappingItemCollection would have already done it while
            //preparing the filePaths
            if (fileName != null)
            {
                m_sourceLocation = fileName;
            }
            else
            {
                m_sourceLocation = null;
            }
            m_parsingErrors = new List<EdmSchemaError>();
            m_scalarMemberMappings = scalarMemberMappings;
            m_containerMapping = LoadMappingItems(reader);
            if (m_currentNamespaceUri != null)
            {
                if (m_currentNamespaceUri == MslConstructs.NamespaceUriV1)
                {
                    m_version = MslConstructs.MappingVersionV1;
                }
                else if (m_currentNamespaceUri == MslConstructs.NamespaceUriV2)
                {
                    m_version = MslConstructs.MappingVersionV2;
                }
                else
                {
                    Debug.Assert(m_currentNamespaceUri == MslConstructs.NamespaceUriV3, "Did you add a new Namespace?");
                    m_version = MslConstructs.MappingVersionV3;
                }
            }
        }

        private readonly Dictionary<string, string> m_alias; //To support the aliasing mechanism provided by MSL.
        private readonly StorageMappingItemCollection m_storageMappingItemCollection; //StorageMappingItemCollection
        private readonly string m_sourceLocation; //location identifier for the MSL file.
        private readonly List<EdmSchemaError> m_parsingErrors;

        private readonly Dictionary<EdmMember, KeyValuePair<TypeUsage, TypeUsage>> m_scalarMemberMappings;
        // dictionary of all the scalar member mappings - this is to validate that no property is mapped to different store types across mappings.

        private bool m_hasQueryViews; //set to true if any of the SetMaps have a query view so that 
        private string m_currentNamespaceUri;
        private readonly EntityContainerMapping m_containerMapping;
        private readonly double m_version;

        // cached xsd schema
        private static XmlSchemaSet s_mappingXmlSchema;

        internal double MappingVersion
        {
            get { return m_version; }
        }

        internal IList<EdmSchemaError> ParsingErrors
        {
            get { return m_parsingErrors; }
        }

        internal bool HasQueryViews
        {
            get { return m_hasQueryViews; }
        }

        internal EntityContainerMapping ContainerMapping
        {
            get { return m_containerMapping; }
        }

        private EdmItemCollection EdmItemCollection
        {
            get { return m_storageMappingItemCollection.EdmItemCollection; }
        }

        private StoreItemCollection StoreItemCollection
        {
            get { return m_storageMappingItemCollection.StoreItemCollection; }
        }

        // <summary>
        // The LoadMappingSchema method loads the mapping file and initializes the
        // MappingSchema that represents this mapping file.
        // For Beta2 atleast, we will support only one EntityContainerMapping per mapping file.
        // </summary>
        private EntityContainerMapping LoadMappingItems(XmlReader innerReader)
        {
            // Using XPathDocument to load the xml file into memory.
            var reader = GetSchemaValidatingReader(innerReader);

            try
            {
                var doc = new XPathDocument(reader);
                // If there were any xsd validation errors, we would have caught these while creatring xpath document.
                if (m_parsingErrors.Count != 0)
                {
                    // If the errors were only warnings continue, otherwise return the errors without loading the mapping.
                    if (!MetadataHelper.CheckIfAllErrorsAreWarnings(m_parsingErrors))
                    {
                        return null;
                    }
                }

                // Create an XPathNavigator to navigate the document in a forward only manner.
                // The XPathNavigator can also be used to run quries through the document while still maintaining
                // the current position. This will be helpful in running validation rules that are not part of Schema.
                var nav = doc.CreateNavigator();
                return LoadMappingItems(nav.Clone());
            }
            catch (XmlException xmlException)
            {
                // There must have been a xml parsing exception. Add the exception information to the error list.
                var error = new EdmSchemaError(
                    Strings.Mapping_InvalidMappingSchema_Parsing(xmlException.Message)
                    , (int)MappingErrorCode.XmlSchemaParsingError, EdmSchemaErrorSeverity.Error, m_sourceLocation,
                    xmlException.LineNumber, xmlException.LinePosition);
                m_parsingErrors.Add(error);
            }

            // Do not close the wrapping reader here, as doing so will close the inner reader. See SQLBUDT 522950 for details.

            return null;
        }

        private EntityContainerMapping LoadMappingItems(XPathNavigator nav)
        {
            // XSD validation is not validating missing Root element.
            if (!MoveToRootElement(nav)
                || (nav.NodeType != XPathNodeType.Element))
            {
                AddToSchemaErrors(
                    Strings.Mapping_Invalid_CSRootElementMissing(
                        MslConstructs.NamespaceUriV1,
                        MslConstructs.NamespaceUriV2,
                        MslConstructs.NamespaceUriV3),
                    MappingErrorCode.RootMappingElementMissing,
                    m_sourceLocation,
                    (IXmlLineInfo)nav, m_parsingErrors);
                // There is no point in going forward if the required root element is not found.
                return null;
            }
            var entityContainerMap = LoadMappingChildNodes(nav.Clone());
            // If there were any parsing errors, invalidate the entity container map and return null.
            if (m_parsingErrors.Count != 0)
            {
                // If all the schema errors are warnings, don't return null.
                if (!MetadataHelper.CheckIfAllErrorsAreWarnings(m_parsingErrors))
                {
                    entityContainerMap = null;
                }
            }
            return entityContainerMap;
        }

        private bool MoveToRootElement(XPathNavigator nav)
        {
            if (nav.MoveToChild(MslConstructs.MappingElement, MslConstructs.NamespaceUriV3))
            {
                // found v3 schema
                m_currentNamespaceUri = MslConstructs.NamespaceUriV3;
                return true;
            }
            else if (nav.MoveToChild(MslConstructs.MappingElement, MslConstructs.NamespaceUriV2))
            {
                // found v2 schema
                m_currentNamespaceUri = MslConstructs.NamespaceUriV2;
                return true;
            }
            else if (nav.MoveToChild(MslConstructs.MappingElement, MslConstructs.NamespaceUriV1))
            {
                m_currentNamespaceUri = MslConstructs.NamespaceUriV1;
                return true;
            }
            //the xml namespace corresponds to neither v1 namespace nor v2 namespace
            return false;
        }

        // <summary>
        // The method loads the child nodes for the root Mapping node
        // into the internal datastructures.
        // </summary>
        private EntityContainerMapping LoadMappingChildNodes(XPathNavigator nav)
        {
            bool hasContainerMapping;
            // If there are any Alias elements in the document, they should be the first ones.
            // This method can only move to the Alias element since comments, PIS etc wont have any Namespace
            // though they could have same name as Alias element.
            if (nav.MoveToChild(MslConstructs.AliasElement, m_currentNamespaceUri))
            {
                // Collect all the alias elements.
                do
                {
                    m_alias.Add(
                        GetAttributeValue(nav.Clone(), MslConstructs.AliasKeyAttribute),
                        GetAttributeValue(nav.Clone(), MslConstructs.AliasValueAttribute));
                }
                while (nav.MoveToNext(MslConstructs.AliasElement, m_currentNamespaceUri));
                // Now move on to the Next element that will be "EntityContainer" element.
                hasContainerMapping = nav.MoveToNext(XPathNodeType.Element);
            }
            else
            {
                // Since there was no Alias element, move on to the Container element.
                hasContainerMapping = nav.MoveToChild(XPathNodeType.Element);
            }

            // Load entity container mapping if any.
            var containerMapping = hasContainerMapping ? LoadEntityContainerMapping(nav.Clone()) : null;
            return containerMapping;
        }

        // <summary>
        // The method loads and returns the EntityContainer Mapping node.
        // </summary>
        private EntityContainerMapping LoadEntityContainerMapping(XPathNavigator nav)
        {
            var navLineInfo = (IXmlLineInfo)nav;

            // The element name can only be EntityContainerMapping element name since XSD validation should have guarneteed this.
            Debug.Assert(nav.LocalName == MslConstructs.EntityContainerMappingElement);
            var entityContainerName = GetAttributeValue(nav.Clone(), MslConstructs.CdmEntityContainerAttribute);
            var storageEntityContainerName = GetAttributeValue(nav.Clone(), MslConstructs.StorageEntityContainerAttribute);

            var generateUpdateViews = GetBoolAttributeValue(
                nav.Clone(), MslConstructs.GenerateUpdateViews, true /* default is true */);

            EntityContainerMapping entityContainerMapping;
            EntityContainer entityContainerType;
            EntityContainer storageEntityContainerType;

            // Now that we support partial mapping, we should first check if the entity container mapping is
            // already present. If its already present, we should add the new child nodes to the existing entity container mapping
            if (m_storageMappingItemCollection.TryGetItem(
                entityContainerName, out entityContainerMapping))
            {
                entityContainerType = entityContainerMapping.EdmEntityContainer;
                storageEntityContainerType = entityContainerMapping.StorageEntityContainer;

                // The only thing we need to make sure is that the storage entity container mapping is the same.
                if (storageEntityContainerName != storageEntityContainerType.Name)
                {
                    AddToSchemaErrors(
                        Strings.StorageEntityContainerNameMismatchWhileSpecifyingPartialMapping(
                            storageEntityContainerName, storageEntityContainerType.Name, entityContainerType.Name),
                        MappingErrorCode.StorageEntityContainerNameMismatchWhileSpecifyingPartialMapping,
                        m_sourceLocation, navLineInfo, m_parsingErrors);

                    return null;
                }
            }
            else
            {
                // At this point we know that the EdmEntityContainer has not been mapped already.
                // If we do find that StorageEntityContainer has already been mapped, return null.
                if (m_storageMappingItemCollection.ContainsStorageEntityContainer(storageEntityContainerName))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_AlreadyMapped_StorageEntityContainer, storageEntityContainerName,
                        MappingErrorCode.AlreadyMappedStorageEntityContainer, m_sourceLocation, navLineInfo, m_parsingErrors);
                    return null;
                }

                // Get the CDM EntityContainer by this name from the metadata workspace.
                EdmItemCollection.TryGetEntityContainer(entityContainerName, out entityContainerType);
                if (entityContainerType == null)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_EntityContainer,
                        entityContainerName, MappingErrorCode.InvalidEntityContainer, m_sourceLocation,
                        navLineInfo, m_parsingErrors);
                }

                StoreItemCollection.TryGetEntityContainer(storageEntityContainerName, out storageEntityContainerType);
                if (storageEntityContainerType == null)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_StorageEntityContainer, storageEntityContainerName,
                        MappingErrorCode.InvalidEntityContainer, m_sourceLocation, navLineInfo, m_parsingErrors);
                }

                // If the EntityContainerTypes are not found, there is no point in continuing with the parsing.
                if ((entityContainerType == null)
                    || (storageEntityContainerType == null))
                {
                    return null;
                }

                // Create an EntityContainerMapping object to hold the mapping information for this EntityContainer.
                // Create a MappingKey and pass it in.
                entityContainerMapping = new EntityContainerMapping(
                    entityContainerType, storageEntityContainerType,
                    m_storageMappingItemCollection, generateUpdateViews /* make validate same as generateUpdateView*/, generateUpdateViews);
                entityContainerMapping.StartLineNumber = navLineInfo.LineNumber;
                entityContainerMapping.StartLinePosition = navLineInfo.LinePosition;
            }

            // Load the child nodes for the created EntityContainerMapping.
            LoadEntityContainerMappingChildNodes(nav.Clone(), entityContainerMapping, storageEntityContainerType);
            return entityContainerMapping;
        }

        // <summary>
        // The method loads the child nodes for the EntityContainer Mapping node
        // into the internal datastructures.
        // </summary>
        private void LoadEntityContainerMappingChildNodes(
            XPathNavigator nav, EntityContainerMapping entityContainerMapping, EntityContainer storageEntityContainerType)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;
            var anyEntitySetMapped = false;

            //If there is no child node for the EntityContainerMapping Element, return.
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                //The valid child nodes for EntityContainerMapping node are various SetMappings( EntitySet, AssociationSet etc ).
                //Loop through the child nodes and lod them as children of the EntityContainerMapping object.
                do
                {
                    switch (nav.LocalName)
                    {
                        case MslConstructs.EntitySetMappingElement:
                            {
                                LoadEntitySetMapping(nav.Clone(), entityContainerMapping, storageEntityContainerType);
                                anyEntitySetMapped = true;
                                break;
                            }
                        case MslConstructs.AssociationSetMappingElement:
                            {
                                LoadAssociationSetMapping(nav.Clone(), entityContainerMapping, storageEntityContainerType);
                                break;
                            }
                        case MslConstructs.FunctionImportMappingElement:
                            {
                                LoadFunctionImportMapping(nav.Clone(), entityContainerMapping);
                                break;
                            }
                        default:
                            AddToSchemaErrors(
                                Strings.Mapping_InvalidContent_Container_SubElement,
                                MappingErrorCode.SetMappingExpected, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                            break;
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            //If the EntityContainer contains entity sets but they are not mapped then we should add an error
            if (entityContainerMapping.EdmEntityContainer.BaseEntitySets.Count != 0
                && !anyEntitySetMapped)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.ViewGen_Missing_Sets_Mapping,
                    entityContainerMapping.EdmEntityContainer.Name, MappingErrorCode.EmptyContainerMapping,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return;
            }

            ValidateFunctionAssociationFunctionMappingUnique(nav.Clone(), entityContainerMapping);
            ValidateModificationFunctionMappingConsistentForAssociations(nav.Clone(), entityContainerMapping);
            ValidateQueryViewsClosure(nav.Clone(), entityContainerMapping);
            ValidateEntitySetFunctionMappingClosure(nav.Clone(), entityContainerMapping);
            // The fileName field in this class will always have absolute path since StorageMappingItemCollection would have already done it while
            // preparing the filePaths.
            entityContainerMapping.SourceLocation = m_sourceLocation;
        }

        // <summary>
        // Validates that collocated association sets are consistently mapped for each entity set (all operations or none). In the case
        // of relationships between sub-types of an entity set, ensures the relationship mapping is legal.
        // </summary>
        private void ValidateModificationFunctionMappingConsistentForAssociations(
            XPathNavigator nav, EntityContainerMapping entityContainerMapping)
        {
            foreach (EntitySetMapping entitySetMapping in entityContainerMapping.EntitySetMaps)
            {
                if (entitySetMapping.ModificationFunctionMappings.Count > 0)
                {
                    // determine the set of association sets that should be mapped for every operation
                    var expectedEnds = new Set<AssociationSetEnd>(
                        entitySetMapping.ImplicitlyMappedAssociationSetEnds).MakeReadOnly();

                    // check that each operation covers each association set
                    foreach (var entityTypeMapping in entitySetMapping.ModificationFunctionMappings)
                    {
                        if (null != entityTypeMapping.DeleteFunctionMapping)
                        {
                            ValidateModificationFunctionMappingConsistentForAssociations(
                                nav, entitySetMapping, entityTypeMapping,
                                entityTypeMapping.DeleteFunctionMapping,
                                expectedEnds, MslConstructs.DeleteFunctionElement);
                        }
                        if (null != entityTypeMapping.InsertFunctionMapping)
                        {
                            ValidateModificationFunctionMappingConsistentForAssociations(
                                nav, entitySetMapping, entityTypeMapping,
                                entityTypeMapping.InsertFunctionMapping,
                                expectedEnds, MslConstructs.InsertFunctionElement);
                        }
                        if (null != entityTypeMapping.UpdateFunctionMapping)
                        {
                            ValidateModificationFunctionMappingConsistentForAssociations(
                                nav, entitySetMapping, entityTypeMapping,
                                entityTypeMapping.UpdateFunctionMapping,
                                expectedEnds, MslConstructs.UpdateFunctionElement);
                        }
                    }
                }
            }
        }

        private void ValidateModificationFunctionMappingConsistentForAssociations(
            XPathNavigator nav,
            EntitySetMapping entitySetMapping,
            EntityTypeModificationFunctionMapping entityTypeMapping,
            ModificationFunctionMapping functionMapping,
            Set<AssociationSetEnd> expectedEnds, string elementName)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            // check that all expected association sets are mapped for in this function mapping
            var actualEnds = new Set<AssociationSetEnd>(functionMapping.CollocatedAssociationSetEnds);
            actualEnds.MakeReadOnly();

            // check that all required ends are present
            foreach (var expectedEnd in expectedEnds)
            {
                // check that the association set is required based on the entity type
                if (MetadataHelper.IsAssociationValidForEntityType(expectedEnd, entityTypeMapping.EntityType))
                {
                    if (!actualEnds.Contains(expectedEnd))
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_ModificationFunction_AssociationSetNotMappedForOperation(
                                entitySetMapping.Set.Name,
                                expectedEnd.ParentAssociationSet.Name,
                                elementName,
                                entityTypeMapping.EntityType.FullName),
                            MappingErrorCode.InvalidModificationFunctionMappingAssociationSetNotMappedForOperation,
                            m_sourceLocation,
                            xmlLineInfoNav,
                            m_parsingErrors);
                    }
                }
            }

            // check that no ends with invalid types are included
            foreach (var actualEnd in actualEnds)
            {
                if (!MetadataHelper.IsAssociationValidForEntityType(actualEnd, entityTypeMapping.EntityType))
                {
                    AddToSchemaErrorWithMessage(
                        Strings.Mapping_ModificationFunction_AssociationEndMappingInvalidForEntityType(
                            entityTypeMapping.EntityType.FullName,
                            actualEnd.ParentAssociationSet.Name,
                            MetadataHelper.GetEntityTypeForEnd(MetadataHelper.GetOppositeEnd(actualEnd).CorrespondingAssociationEndMember).
                                           FullName),
                        MappingErrorCode.InvalidModificationFunctionMappingAssociationEndMappingInvalidForEntityType,
                        m_sourceLocation,
                        xmlLineInfoNav,
                        m_parsingErrors);
                }
            }
        }

        // <summary>
        // Validates that association sets are only mapped once.
        // </summary>
        // <param name="entityContainerMapping"> Container to validate </param>
        private void ValidateFunctionAssociationFunctionMappingUnique(
            XPathNavigator nav, EntityContainerMapping entityContainerMapping)
        {
            var mappingCounts = new Dictionary<EntitySetBase, int>();

            // Walk through all entity set mappings
            foreach (EntitySetMapping entitySetMapping in entityContainerMapping.EntitySetMaps)
            {
                if (entitySetMapping.ModificationFunctionMappings.Count > 0)
                {
                    // Get set of association sets implicitly mapped associations to avoid double counting
                    var associationSets = new Set<EntitySetBase>();
                    foreach (var end in entitySetMapping.ImplicitlyMappedAssociationSetEnds)
                    {
                        associationSets.Add(end.ParentAssociationSet);
                    }

                    foreach (var associationSet in associationSets)
                    {
                        IncrementCount(mappingCounts, associationSet);
                    }
                }
            }

            // Walk through all association set mappings
            foreach (AssociationSetMapping associationSetMapping in entityContainerMapping.RelationshipSetMaps)
            {
                if (null != associationSetMapping.ModificationFunctionMapping)
                {
                    IncrementCount(mappingCounts, associationSetMapping.Set);
                }
            }

            // Check for redundantly mapped association sets
            var violationNames = new List<string>();
            foreach (var mappingCount in mappingCounts)
            {
                if (mappingCount.Value > 1)
                {
                    violationNames.Add(mappingCount.Key.Name);
                }
            }

            if (0 < violationNames.Count)
            {
                // Warn the user that association sets are mapped multiple times                
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_ModificationFunction_AssociationSetAmbiguous,
                    StringUtil.ToCommaSeparatedString(violationNames),
                    MappingErrorCode.AmbiguousModificationFunctionMappingForAssociationSet,
                    m_sourceLocation, (IXmlLineInfo)nav, m_parsingErrors);
            }
        }

        private static void IncrementCount<T>(Dictionary<T, int> counts, T key)
        {
            int count;
            if (counts.TryGetValue(key, out count))
            {
                count++;
            }
            else
            {
                count = 1;
            }
            counts[key] = count;
        }

        // <summary>
        // Validates that all or no related extents have function mappings. If an EntitySet or an AssociationSet has a function mapping,
        // then all the sets that touched the same store tableSet must also have function mappings.
        // </summary>
        // <param name="entityContainerMapping"> Container to validate. </param>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void ValidateEntitySetFunctionMappingClosure(XPathNavigator nav, EntityContainerMapping entityContainerMapping)
        {
            // here we build a mapping between the tables and the sets,
            // setmapping => typemapping => mappingfragments, foreach mappingfragments we have one Tableset,
            // then add the tableset with setmapping to the dictionary

            var setMappingPerTable =
                new KeyToListMap<EntitySet, EntitySetBaseMapping>(EqualityComparer<EntitySet>.Default);

            // Walk through all set mappings
            foreach (var setMapping in entityContainerMapping.AllSetMaps)
            {
                foreach (var typeMapping in setMapping.TypeMappings)
                {
                    foreach (var fragment in typeMapping.MappingFragments)
                    {
                        setMappingPerTable.Add(fragment.TableSet, setMapping);
                    }
                }
            }

            // Get set of association sets implicitly mapped associations to avoid double counting
            var implicitMappedAssociationSets = new Set<EntitySetBase>();

            // Walk through all entity set mappings
            foreach (EntitySetMapping entitySetMapping in entityContainerMapping.EntitySetMaps)
            {
                if (entitySetMapping.ModificationFunctionMappings.Count > 0)
                {
                    foreach (var end in entitySetMapping.ImplicitlyMappedAssociationSetEnds)
                    {
                        implicitMappedAssociationSets.Add(end.ParentAssociationSet);
                    }
                }
            }

            foreach (var table in setMappingPerTable.Keys)
            {
                // if any of the sets who touches the same table has modification function, 
                // then all the sets that touches the same table should have modification function
                if (
                    setMappingPerTable.ListForKey(table).Any(
                        s => s.HasModificationFunctionMapping || implicitMappedAssociationSets.Any(aset => aset == s.Set))
                    &&
                    setMappingPerTable.ListForKey(table).Any(
                        s => !s.HasModificationFunctionMapping && !implicitMappedAssociationSets.Any(aset => aset == s.Set)))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_MissingSetClosure,
                        StringUtil.ToCommaSeparatedString(
                            setMappingPerTable.ListForKey(table)
                                              .Where(s => !s.HasModificationFunctionMapping).Select(s => s.Set.Name)),
                        MappingErrorCode.MissingSetClosureInModificationFunctionMapping, m_sourceLocation, (IXmlLineInfo)nav
                        , m_parsingErrors);
                }
            }
        }

        private static void ValidateClosureAmongSets(
            EntityContainerMapping entityContainerMapping, Set<EntitySetBase> sets, Set<EntitySetBase> additionalSetsInClosure)
        {
            bool nodeFound;
            do
            {
                nodeFound = false;
                var newNodes = new List<EntitySetBase>();

                // Register entity sets dependencies for association sets
                foreach (var entitySetBase in additionalSetsInClosure)
                {
                    var associationSet = entitySetBase as AssociationSet;
                    //Foreign Key Associations do not add to the dependancies
                    if (associationSet != null
                        && !associationSet.ElementType.IsForeignKey)
                    {
                        // add the entity sets bound to the end roles to the required list
                        foreach (var end in associationSet.AssociationSetEnds)
                        {
                            if (!additionalSetsInClosure.Contains(end.EntitySet))
                            {
                                newNodes.Add(end.EntitySet);
                            }
                        }
                    }
                }

                // Register all association sets referencing known entity sets
                foreach (var entitySetBase in entityContainerMapping.EdmEntityContainer.BaseEntitySets)
                {
                    var associationSet = entitySetBase as AssociationSet;
                    //Foreign Key Associations do not add to the dependancies
                    if (associationSet != null
                        && !associationSet.ElementType.IsForeignKey)
                    {
                        // check that this association set isn't already in the required set
                        if (!additionalSetsInClosure.Contains(associationSet))
                        {
                            foreach (var end in associationSet.AssociationSetEnds)
                            {
                                if (additionalSetsInClosure.Contains(end.EntitySet))
                                {
                                    // this association set must be added to the required list if
                                    // any of its ends are in that list
                                    newNodes.Add(associationSet);
                                    break; // no point adding the association set twice
                                }
                            }
                        }
                    }
                }

                if (0 < newNodes.Count)
                {
                    nodeFound = true;
                    additionalSetsInClosure.AddRange(newNodes);
                }
            }
            while (nodeFound);

            additionalSetsInClosure.Subtract(sets);
        }

        // <summary>
        // Validates that all or no related extents have query views defined. If an extent has a query view defined, then
        // all related extents must also have query views.
        // </summary>
        // <param name="entityContainerMapping"> Container to validate. </param>
        private void ValidateQueryViewsClosure(XPathNavigator nav, EntityContainerMapping entityContainerMapping)
        {
            //If there is no query view defined, no need to validate
            if (!m_hasQueryViews)
            {
                return;
            }
            // Check that query views apply to complete subgraph by tracking which extents have query
            // mappings and which extents must include query views
            var setsWithQueryViews = new Set<EntitySetBase>();
            var setsRequiringQueryViews = new Set<EntitySetBase>();

            // Walk through all set mappings
            foreach (var setMapping in entityContainerMapping.AllSetMaps)
            {
                if (setMapping.QueryView != null)
                {
                    // a function mapping exists for this entity set
                    setsWithQueryViews.Add(setMapping.Set);
                }
            }

            // Initialize sets requiring function mapping with the sets that are actually function mapped
            setsRequiringQueryViews.AddRange(setsWithQueryViews);

            ValidateClosureAmongSets(entityContainerMapping, setsWithQueryViews, setsRequiringQueryViews);

            // Check that no required entity or association sets are missing
            if (0 < setsRequiringQueryViews.Count)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_Invalid_Query_Views_MissingSetClosure,
                    StringUtil.ToCommaSeparatedString(setsRequiringQueryViews),
                    MappingErrorCode.MissingSetClosureInQueryViews, m_sourceLocation, (IXmlLineInfo)nav
                    , m_parsingErrors);
            }
        }

        // <summary>
        // The method loads the child nodes for the EntitySet Mapping node
        // into the internal datastructures.
        // </summary>
        private void LoadEntitySetMapping(
            XPathNavigator nav, EntityContainerMapping entityContainerMapping, EntityContainer storageEntityContainerType)
        {
            //Get the EntitySet name 
            var entitySetName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingNameAttribute);
            //Get the EntityType name, need to parse it if the mapping information is being specified for multiple types 
            var entityTypeName = GetAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingTypeNameAttribute);
            //Get the table name. This might be emptystring since the user can have a TableMappingFragment instead of this.
            var tableName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingStoreEntitySetAttribute);

            var distinctFlag = GetBoolAttributeValue(
                nav.Clone(), MslConstructs.MappingFragmentMakeColumnsDistinctAttribute, false /*default value*/);

            EntitySet entitySet;

            // First check to see if the Entity Set Mapping is already specified. It can be specified, in the same schema file later on
            // on a totally different file. Since we support partial mapping, we should just add mapping fragments or entity type
            // mappings to the existing entity set mapping
            var setMapping = (EntitySetMapping)entityContainerMapping.GetEntitySetMapping(entitySetName);

            // Update the info about the schema element
            var navLineInfo = (IXmlLineInfo)nav;

            if (setMapping == null)
            {
                //Try to find the EntitySet with the given name in the EntityContainer.
                if (!entityContainerMapping.EdmEntityContainer.TryGetEntitySetByName(entitySetName, /*ignoreCase*/ false, out entitySet))
                {
                    //If no EntitySet with the given name exists, than add a schema error and return
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_Entity_Set, entitySetName,
                        MappingErrorCode.InvalidEntitySet, m_sourceLocation, navLineInfo, m_parsingErrors);
                    //There is no point in continuing the loding of this EntitySetMapping if the EntitySet is not found
                    return;
                }
                //Create the EntitySet Mapping which contains the mapping information for EntitySetMap.
                setMapping = new EntitySetMapping(entitySet, entityContainerMapping);
            }
            else
            {
                entitySet = (EntitySet)setMapping.Set;
            }

            //Set the Start Line Information on Fragment
            setMapping.StartLineNumber = navLineInfo.LineNumber;
            setMapping.StartLinePosition = navLineInfo.LinePosition;
            entityContainerMapping.AddSetMapping(setMapping);

            //If the TypeName was not specified as an attribute, than an EntityTypeMapping element should be present 
            if (String.IsNullOrEmpty(entityTypeName))
            {
                if (nav.MoveToChild(XPathNodeType.Element))
                {
                    do
                    {
                        switch (nav.LocalName)
                        {
                            case MslConstructs.EntityTypeMappingElement:
                                {
                                    //TableName could also be specified on EntityTypeMapping element
                                    tableName = GetAliasResolvedAttributeValue(
                                        nav.Clone(), MslConstructs.EntityTypeMappingStoreEntitySetAttribute);
                                    //Load the EntityTypeMapping into memory.
                                    LoadEntityTypeMapping(
                                        nav.Clone(), setMapping, tableName, storageEntityContainerType, false /*No distinct flag so far*/,
                                        entityContainerMapping.GenerateUpdateViews);
                                    break;
                                }
                            case MslConstructs.QueryViewElement:
                                {
                                    if (!(String.IsNullOrEmpty(tableName)))
                                    {
                                        AddToSchemaErrorsWithMemberInfo(
                                            Strings.Mapping_TableName_QueryView, entitySetName,
                                            MappingErrorCode.TableNameAttributeWithQueryView, m_sourceLocation, navLineInfo,
                                            m_parsingErrors);
                                        return;
                                    }
                                    //Load the Query View into the set mapping,
                                    //if you get an error, return immediately since 
                                    //you go on, you could be giving lot of dubious errors
                                    if (!LoadQueryView(nav.Clone(), setMapping))
                                    {
                                        return;
                                    }
                                    break;
                                }
                            default:
                                AddToSchemaErrors(
                                    Strings.Mapping_InvalidContent_TypeMapping_QueryView,
                                    MappingErrorCode.InvalidContent, m_sourceLocation, navLineInfo, m_parsingErrors);
                                break;
                        }
                    }
                    while (nav.MoveToNext(XPathNodeType.Element));
                }
            }
            else
            {
                //Load the EntityTypeMapping into memory.
                LoadEntityTypeMapping(
                    nav.Clone(), setMapping, tableName, storageEntityContainerType, distinctFlag, entityContainerMapping.GenerateUpdateViews);
            }
            ValidateAllEntityTypesHaveFunctionMapping(nav.Clone(), setMapping);
            //Add a schema error if the set mapping has no content
            if (setMapping.HasNoContent)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Emtpty_SetMap, entitySet.Name,
                    MappingErrorCode.EmptySetMapping, m_sourceLocation, navLineInfo, m_parsingErrors);
            }
        }

        // Ensure if any type has a function mapping, all types have function mappings
        private void ValidateAllEntityTypesHaveFunctionMapping(XPathNavigator nav, EntitySetMapping setMapping)
        {
            var functionMappedTypes = new Set<EdmType>();
            foreach (var modificationFunctionMapping in setMapping.ModificationFunctionMappings)
            {
                functionMappedTypes.Add(modificationFunctionMapping.EntityType);
            }
            if (0 < functionMappedTypes.Count)
            {
                var unmappedTypes =
                    new Set<EdmType>(
                        MetadataHelper.GetTypeAndSubtypesOf(setMapping.Set.ElementType, EdmItemCollection, false /*includeAbstractTypes*/));
                unmappedTypes.Subtract(functionMappedTypes);

                // Remove abstract types
                var abstractTypes = new Set<EdmType>();
                foreach (EntityType unmappedType in unmappedTypes)
                {
                    if (unmappedType.Abstract)
                    {
                        abstractTypes.Add(unmappedType);
                    }
                }
                unmappedTypes.Subtract(abstractTypes);

                // See if there are any remaining entity types requiring function mapping
                if (0 < unmappedTypes.Count)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_MissingEntityType,
                        StringUtil.ToCommaSeparatedString(unmappedTypes),
                        MappingErrorCode.MissingModificationFunctionMappingForEntityType, m_sourceLocation, (IXmlLineInfo)nav
                        , m_parsingErrors);
                }
            }
        }

        private bool TryParseEntityTypeAttribute(
            XPathNavigator nav,
            EntityType rootEntityType,
            Func<EntityType, string> typeNotAssignableMessage,
            out Set<EntityType> isOfTypeEntityTypes,
            out Set<EntityType> entityTypes)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;
            var entityTypeAttribute = GetAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingTypeNameAttribute);

            isOfTypeEntityTypes = new Set<EntityType>();
            entityTypes = new Set<EntityType>();

            // get components of type declaration
            var entityTypeNames = entityTypeAttribute.Split(MslConstructs.TypeNameSperator).Select(s => s.Trim());

            // figure out each component
            foreach (var name in entityTypeNames)
            {
                var isTypeOf = name.StartsWith(MslConstructs.IsTypeOf, StringComparison.Ordinal);
                string entityTypeName;
                if (isTypeOf)
                {
                    // get entityTypeName of OfType(entityTypeName)
                    if (!name.EndsWith(MslConstructs.IsTypeOfTerminal, StringComparison.Ordinal))
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_InvalidContent_IsTypeOfNotTerminated,
                            MappingErrorCode.InvalidEntityType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                        // No point in continuing with an error in the entitytype name
                        return false;
                    }
                    entityTypeName = name.Substring(MslConstructs.IsTypeOf.Length);
                    entityTypeName =
                        entityTypeName.Substring(0, entityTypeName.Length - MslConstructs.IsTypeOfTerminal.Length).Trim();
                }
                else
                {
                    entityTypeName = name;
                }

                // resolve aliases
                entityTypeName = GetAliasResolvedValue(entityTypeName);

                EntityType entityType;
                if (!EdmItemCollection.TryGetItem(entityTypeName, out entityType))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_Entity_Type, entityTypeName,
                        MappingErrorCode.InvalidEntityType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                    // No point in continuing with an error in the entitytype name
                    return false;
                }
                if (!(Helper.IsAssignableFrom(rootEntityType, entityType)))
                {
                    AddToSchemaErrorWithMessage(
                        typeNotAssignableMessage(entityType),
                        MappingErrorCode.InvalidEntityType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                    //no point in continuing with an error in the entitytype name
                    return false;
                }

                // Using TypeOf construct on an abstract type that does not have
                // any concrete descendants is not allowed
                if (entityType.Abstract)
                {
                    if (isTypeOf)
                    {
                        var typeAndSubTypes = MetadataHelper.GetTypeAndSubtypesOf(
                            entityType, EdmItemCollection, false /*includeAbstractTypes*/);
                        if (!typeAndSubTypes.GetEnumerator().MoveNext())
                        {
                            AddToSchemaErrorsWithMemberInfo(
                                Strings.Mapping_InvalidContent_AbstractEntity_IsOfType, entityType.FullName,
                                MappingErrorCode.MappingOfAbstractType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                            return false;
                        }
                    }
                    else
                    {
                        AddToSchemaErrorsWithMemberInfo(
                            Strings.Mapping_InvalidContent_AbstractEntity_Type, entityType.FullName,
                            MappingErrorCode.MappingOfAbstractType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                        return false;
                    }
                }

                // Add type to set
                if (isTypeOf)
                {
                    isOfTypeEntityTypes.Add(entityType);
                }
                else
                {
                    entityTypes.Add(entityType);
                }
            }

            // No failures
            return true;
        }

        // <summary>
        // The method loads the child nodes for the EntityType Mapping node
        // into the internal datastructures.
        // </summary>
        private void LoadEntityTypeMapping(
            XPathNavigator nav, EntitySetMapping entitySetMapping, string tableName, EntityContainer storageEntityContainerType,
            bool distinctFlagAboveType, bool generateUpdateViews)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            //Create an EntityTypeMapping to hold the information for EntityType mapping.
            var entityTypeMapping = new EntityTypeMapping(entitySetMapping);

            //Get entity types
            Set<EntityType> entityTypes;
            Set<EntityType> isOfTypeEntityTypes;
            var rootEntityType = (EntityType)entitySetMapping.Set.ElementType;
            if (!TryParseEntityTypeAttribute(
                nav.Clone(), rootEntityType,
                e =>
                Strings.Mapping_InvalidContent_Entity_Type_For_Entity_Set(e.FullName, rootEntityType.FullName, entitySetMapping.Set.Name),
                out isOfTypeEntityTypes,
                out entityTypes))
            {
                // Return if we cannot parse entity types
                return;
            }

            // Register all mapped types
            foreach (var entityType in entityTypes)
            {
                entityTypeMapping.AddType(entityType);
            }
            foreach (var isOfTypeEntityType in isOfTypeEntityTypes)
            {
                entityTypeMapping.AddIsOfType(isOfTypeEntityType);
            }

            //If the table name was not specified on the EntitySetMapping element nor the EntityTypeMapping element
            //than a table mapping fragment element should be present
            //Loop through the TableMappingFragment elements and add them to EntityTypeMappings
            if (String.IsNullOrEmpty(tableName))
            {
                if (!nav.MoveToChild(XPathNodeType.Element))
                {
                    return;
                }
                do
                {
                    if (nav.LocalName
                        == MslConstructs.ModificationFunctionMappingElement)
                    {
                        entitySetMapping.HasModificationFunctionMapping = true;
                        LoadEntityTypeModificationFunctionMapping(nav.Clone(), entitySetMapping, entityTypeMapping);
                    }
                    else if (nav.LocalName
                             != MslConstructs.MappingFragmentElement)
                    {
                        AddToSchemaErrors(
                            Strings.Mapping_InvalidContent_Table_Expected,
                            MappingErrorCode.TableMappingFragmentExpected, m_sourceLocation, xmlLineInfoNav
                            , m_parsingErrors);
                    }
                    else
                    {
                        var distinctFlag = GetBoolAttributeValue(
                            nav.Clone(), MslConstructs.MappingFragmentMakeColumnsDistinctAttribute, false /*default value*/);

                        if (generateUpdateViews && distinctFlag)
                        {
                            AddToSchemaErrors(
                                Strings.Mapping_DistinctFlagInReadWriteContainer,
                                MappingErrorCode.DistinctFragmentInReadWriteContainer, m_sourceLocation, xmlLineInfoNav,
                                m_parsingErrors);
                        }

                        tableName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.MappingFragmentStoreEntitySetAttribute);
                        var fragment = LoadMappingFragment(
                            nav.Clone(), entityTypeMapping, tableName, storageEntityContainerType, distinctFlag);
                        //The fragment can be null in the cases of validation errors.
                        if (fragment != null)
                        {
                            entityTypeMapping.AddFragment(fragment);
                        }
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }
            else
            {
                if (nav.LocalName
                    == MslConstructs.ModificationFunctionMappingElement)
                {
                    // function mappings cannot exist in the context of a table mapping
                    AddToSchemaErrors(
                        Strings.Mapping_ModificationFunction_In_Table_Context,
                        MappingErrorCode.InvalidTableNameAttributeWithModificationFunctionMapping,
                        m_sourceLocation, xmlLineInfoNav
                        , m_parsingErrors);
                }

                if (generateUpdateViews && distinctFlagAboveType)
                {
                    AddToSchemaErrors(
                        Strings.Mapping_DistinctFlagInReadWriteContainer,
                        MappingErrorCode.DistinctFragmentInReadWriteContainer, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                }

                var fragment = LoadMappingFragment(
                    nav.Clone(), entityTypeMapping, tableName,
                    storageEntityContainerType, distinctFlagAboveType);
                //The fragment can be null in the cases of validation errors.
                if (fragment != null)
                {
                    entityTypeMapping.AddFragment(fragment);
                }
            }
            entitySetMapping.AddTypeMapping(entityTypeMapping);
        }

        // <summary>
        // Loads modification function mappings for entity type.
        // </summary>
        private void LoadEntityTypeModificationFunctionMapping(
            XPathNavigator nav,
            EntitySetMapping entitySetMapping,
            EntityTypeMapping entityTypeMapping)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            // Function mappings can apply only to a single type.
            if (entityTypeMapping.IsOfTypes.Count != 0
                || entityTypeMapping.Types.Count != 1)
            {
                AddToSchemaErrors(
                    Strings.Mapping_ModificationFunction_Multiple_Types,
                    MappingErrorCode.InvalidModificationFunctionMappingForMultipleTypes,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return;
            }
            var entityType = (EntityType)entityTypeMapping.Types[0];
            //Function Mapping is not allowed to be defined for Abstract Types
            if (entityType.Abstract)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_AbstractEntity_FunctionMapping, entityType.FullName,
                    MappingErrorCode.MappingOfAbstractType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return;
            }

            // check that no mapping exists for this entity type already
            foreach (var existingMapping in entitySetMapping.ModificationFunctionMappings)
            {
                if (existingMapping.EntityType.Equals(entityType))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_RedundantEntityTypeMapping,
                        entityType.Name, MappingErrorCode.RedundantEntityTypeMappingInModificationFunctionMapping, m_sourceLocation,
                        xmlLineInfoNav
                        , m_parsingErrors);
                    return;
                }
            }

            // create function loader
            var functionLoader = new ModificationFunctionMappingLoader(this, entitySetMapping.Set);

            // Load all function definitions (for insert, delete and update)
            ModificationFunctionMapping deleteFunctionMapping = null;
            ModificationFunctionMapping insertFunctionMapping = null;
            ModificationFunctionMapping updateFunctionMapping = null;
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    switch (nav.LocalName)
                    {
                        case MslConstructs.DeleteFunctionElement:
                            deleteFunctionMapping = functionLoader.LoadEntityTypeModificationFunctionMapping(
                                nav.Clone(), entitySetMapping.Set, false, true, entityType);
                            break;
                        case MslConstructs.InsertFunctionElement:
                            insertFunctionMapping = functionLoader.LoadEntityTypeModificationFunctionMapping(
                                nav.Clone(), entitySetMapping.Set, true, false, entityType);
                            break;
                        case MslConstructs.UpdateFunctionElement:
                            updateFunctionMapping = functionLoader.LoadEntityTypeModificationFunctionMapping(
                                nav.Clone(), entitySetMapping.Set, true, true, entityType);
                            break;
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            // Ensure that assocation set end mappings bind to the same end (e.g., in Person Manages Person
            // self-association, ensure that the manager end or the report end is mapped but not both)
            IEnumerable<ModificationFunctionParameterBinding> parameterList = new List<ModificationFunctionParameterBinding>();
            if (null != deleteFunctionMapping)
            {
                parameterList = Helper.Concat(parameterList, deleteFunctionMapping.ParameterBindings);
            }
            if (null != insertFunctionMapping)
            {
                parameterList = Helper.Concat(parameterList, insertFunctionMapping.ParameterBindings);
            }
            if (null != updateFunctionMapping)
            {
                parameterList = Helper.Concat(parameterList, updateFunctionMapping.ParameterBindings);
            }

            var associationEnds = new Dictionary<AssociationSet, AssociationEndMember>();
            foreach (var parameterBinding in parameterList)
            {
                if (null != parameterBinding.MemberPath.AssociationSetEnd)
                {
                    var associationSet = parameterBinding.MemberPath.AssociationSetEnd.ParentAssociationSet;
                    // the "end" corresponds to the second member in the path, e.g.
                    // ID<-Manager where Manager is the end
                    var currentEnd = parameterBinding.MemberPath.AssociationSetEnd.CorrespondingAssociationEndMember;

                    AssociationEndMember existingEnd;
                    if (associationEnds.TryGetValue(associationSet, out existingEnd)
                        &&
                        existingEnd != currentEnd)
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_ModificationFunction_MultipleEndsOfAssociationMapped(
                                currentEnd.Name, existingEnd.Name, associationSet.Name),
                            MappingErrorCode.InvalidModificationFunctionMappingMultipleEndsOfAssociationMapped, m_sourceLocation,
                            xmlLineInfoNav, m_parsingErrors);
                        return;
                    }
                    else
                    {
                        associationEnds[associationSet] = currentEnd;
                    }
                }
            }

            // Register the function mapping on the entity set mapping
            var mapping = new EntityTypeModificationFunctionMapping(
                entityType, deleteFunctionMapping, insertFunctionMapping, updateFunctionMapping);

            entitySetMapping.AddModificationFunctionMapping(mapping);
        }

        // <summary>
        // The method loads the query view for the Set Mapping node
        // into the internal datastructures.
        // </summary>
        private bool LoadQueryView(XPathNavigator nav, EntitySetBaseMapping setMapping)
        {
            Debug.Assert(nav.LocalName == MslConstructs.QueryViewElement);

            var queryView = nav.Value;
            var includeSubtypes = false;

            var typeNameString = GetAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingTypeNameAttribute);
            if (typeNameString != null)
            {
                typeNameString = typeNameString.Trim();
            }

            var xmlLineInfo = nav as IXmlLineInfo;
            if (setMapping.QueryView == null)
            {
                // QV must be the special-case first view.
                if (typeNameString != null)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        val => Strings.Mapping_TypeName_For_First_QueryView,
                        setMapping.Set.Name, MappingErrorCode.TypeNameForFirstQueryView,
                        m_sourceLocation, xmlLineInfo, m_parsingErrors);
                    return false;
                }

                if (String.IsNullOrEmpty(queryView))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_Empty_QueryView,
                        setMapping.Set.Name, MappingErrorCode.EmptyQueryView,
                        m_sourceLocation, xmlLineInfo, m_parsingErrors);
                    return false;
                }
                setMapping.QueryView = queryView;
                m_hasQueryViews = true;
                return true;
            }
            else
            {
                //QV must be typeof or typeofonly view
                if (typeNameString == null
                    || typeNameString.Trim().Length == 0)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_QueryView_TypeName_Not_Defined,
                        setMapping.Set.Name, MappingErrorCode.NoTypeNameForTypeSpecificQueryView,
                        m_sourceLocation, xmlLineInfo, m_parsingErrors);
                    return false;
                }

                //Get entity types
                Set<EntityType> entityTypes;
                Set<EntityType> isOfTypeEntityTypes;
                var rootEntityType = (EntityType)setMapping.Set.ElementType;
                if (!TryParseEntityTypeAttribute(
                    nav.Clone(), rootEntityType,
                    e => Strings.Mapping_InvalidContent_Entity_Type_For_Entity_Set(e.FullName, rootEntityType.FullName, setMapping.Set.Name),
                    out isOfTypeEntityTypes,
                    out entityTypes))
                {
                    // Return if we cannot parse entity types
                    return false;
                }
                Debug.Assert(isOfTypeEntityTypes.Count > 0 || entityTypes.Count > 0);
                Debug.Assert(!(isOfTypeEntityTypes.Count > 0 && entityTypes.Count > 0));

                EntityType entityType;
                if (isOfTypeEntityTypes.Count == 1)
                {
                    //OfType View
                    entityType = isOfTypeEntityTypes.First();
                    includeSubtypes = true;
                }
                else if (entityTypes.Count == 1)
                {
                    //OfTypeOnly View
                    entityType = entityTypes.First();
                    includeSubtypes = false;
                }
                else
                {
                    //More than one type
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_QueryViewMultipleTypeInTypeName, setMapping.Set.ToString(),
                        MappingErrorCode.TypeNameContainsMultipleTypesForQueryView, m_sourceLocation, xmlLineInfo, m_parsingErrors);
                    return false;
                }

                //Check if IsTypeOf(A) and A is the base type
                if (includeSubtypes && setMapping.Set.ElementType.EdmEquals(entityType))
                {
                    //Don't allow TypeOFOnly(a) if a is a base type. 
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_QueryView_For_Base_Type, entityType.ToString(), setMapping.Set.ToString(),
                        MappingErrorCode.IsTypeOfQueryViewForBaseType, m_sourceLocation, xmlLineInfo, m_parsingErrors);
                    return false;
                }

                if (String.IsNullOrEmpty(queryView))
                {
                    if (includeSubtypes)
                    {
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_Empty_QueryView_OfType,
                            entityType.Name, setMapping.Set.Name, MappingErrorCode.EmptyQueryView,
                            m_sourceLocation, xmlLineInfo, m_parsingErrors);
                        return false;
                    }
                    else
                    {
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_Empty_QueryView_OfTypeOnly,
                            setMapping.Set.Name, entityType.Name, MappingErrorCode.EmptyQueryView,
                            m_sourceLocation, xmlLineInfo, m_parsingErrors);
                        return false;
                    }
                }

                //Add it to the QV cache
                var key = new Triple(setMapping.Set, new Pair<EntityTypeBase, bool>(entityType, includeSubtypes));

                if (setMapping.ContainsTypeSpecificQueryView(key))
                {
                    //two QVs for the same type 

                    EdmSchemaError error = null;
                    if (includeSubtypes)
                    {
                        error =
                            new EdmSchemaError(
                                Strings.Mapping_QueryView_Duplicate_OfType(setMapping.Set, entityType),
                                (int)MappingErrorCode.QueryViewExistsForEntitySetAndType, EdmSchemaErrorSeverity.Error,
                                m_sourceLocation,
                                xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    }
                    else
                    {
                        error =
                            new EdmSchemaError(
                                Strings.Mapping_QueryView_Duplicate_OfTypeOnly(setMapping.Set, entityType),
                                (int)MappingErrorCode.QueryViewExistsForEntitySetAndType, EdmSchemaErrorSeverity.Error,
                                m_sourceLocation,
                                xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    }

                    m_parsingErrors.Add(error);
                    return false;
                }

                setMapping.AddTypeSpecificQueryView(key, queryView);
                return true;
            }
        }

        // <summary>
        // The method loads the child nodes for the AssociationSet Mapping node
        // into the internal datastructures.
        // </summary>
        private void LoadAssociationSetMapping(
            XPathNavigator nav, EntityContainerMapping entityContainerMapping, EntityContainer storageEntityContainerType)
        {
            var navLineInfo = (IXmlLineInfo)nav;

            //Get the AssociationSet name 
            var associationSetName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.AssociationSetMappingNameAttribute);
            //Get the AssociationType name, need to parse it if the mapping information is being specified for multiple types 
            var associationTypeName = GetAliasResolvedAttributeValue(
                nav.Clone(), MslConstructs.AssociationSetMappingTypeNameAttribute);
            //Get the table name. This might be emptystring since the user can have a TableMappingFragment instead of this.
            var tableName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingStoreEntitySetAttribute);
            //Try to find the AssociationSet with the given name in the EntityContainer.
            RelationshipSet relationshipSet;
            entityContainerMapping.EdmEntityContainer.TryGetRelationshipSetByName(
                associationSetName, false /*ignoreCase*/, out relationshipSet);
            var associationSet = relationshipSet as AssociationSet;
            //If no AssociationSet with the given name exists, than Add a schema error and return
            if (associationSet == null)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Association_Set, associationSetName,
                    MappingErrorCode.InvalidAssociationSet, m_sourceLocation, navLineInfo, m_parsingErrors);
                //There is no point in continuing the loading of association set map if the AssociationSetName has a problem
                return;
            }

            if (associationSet.ElementType.IsForeignKey)
            {
                var constraint = associationSet.ElementType.ReferentialConstraints.Single();
                IEnumerable<EdmMember> dependentKeys =
                    MetadataHelper.GetEntityTypeForEnd((AssociationEndMember)constraint.ToRole).KeyMembers;
                if (associationSet.ElementType.ReferentialConstraints.Single().ToProperties.All(p => dependentKeys.Contains(p)))
                {
                    var error = AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_ForeignKey_Association_Set_PKtoPK, associationSetName,
                        MappingErrorCode.InvalidAssociationSet, m_sourceLocation, navLineInfo, m_parsingErrors);
                    //Downgrade to a warning if the foreign key constraint is between keys (for back-compat reasons)
                    error.Severity = EdmSchemaErrorSeverity.Warning;
                }
                else
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_ForeignKey_Association_Set, associationSetName,
                        MappingErrorCode.InvalidAssociationSet, m_sourceLocation, navLineInfo, m_parsingErrors);
                }
                return;
            }

            if (entityContainerMapping.ContainsAssociationSetMapping(associationSet))
            {
                //Can not add this set mapping since our storage dictionary won't allow
                //duplicate maps
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_Duplicate_CdmAssociationSet_StorageMap, associationSetName,
                    MappingErrorCode.DuplicateSetMapping, m_sourceLocation, navLineInfo, m_parsingErrors);
                return;
            }
            //Create the AssociationSet Mapping which contains the mapping information for association set.
            var setMapping = new AssociationSetMapping(associationSet, entityContainerMapping);

            //Set the Start Line Information on Fragment
            setMapping.StartLineNumber = navLineInfo.LineNumber;
            setMapping.StartLinePosition = navLineInfo.LinePosition;

            if (!nav.MoveToChild(XPathNodeType.Element))
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Emtpty_SetMap, associationSet.Name,
                    MappingErrorCode.EmptySetMapping, m_sourceLocation, navLineInfo, m_parsingErrors);
                return;
            }

            entityContainerMapping.AddSetMapping(setMapping);

            //If there is a query view it has to be the first element
            if (nav.LocalName
                == MslConstructs.QueryViewElement)
            {
                if (!(String.IsNullOrEmpty(tableName)))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_TableName_QueryView, associationSetName,
                        MappingErrorCode.TableNameAttributeWithQueryView, m_sourceLocation, navLineInfo, m_parsingErrors);
                    return;
                }
                //Load the Query View into the set mapping,
                //if you get an error, return immediately since 
                //you go on, you could be giving lot of dubious errors
                if (!LoadQueryView(nav.Clone(), setMapping))
                {
                    return;
                }
                //If there are no more elements just return
                if (!nav.MoveToNext(XPathNodeType.Element))
                {
                    return;
                }
            }

            if ((nav.LocalName == MslConstructs.EndPropertyMappingElement)
                ||
                (nav.LocalName == MslConstructs.ModificationFunctionMappingElement))
            {
                if ((String.IsNullOrEmpty(associationTypeName)))
                {
                    AddToSchemaErrors(
                        Strings.Mapping_InvalidContent_Association_Type_Empty,
                        MappingErrorCode.InvalidAssociationType, m_sourceLocation, navLineInfo, m_parsingErrors);
                    return;
                }
                //Load the AssociationTypeMapping into memory.
                LoadAssociationTypeMapping(nav.Clone(), setMapping, associationTypeName, tableName, storageEntityContainerType);
            }
            else if (nav.LocalName
                     == MslConstructs.ConditionElement)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_AssociationSet_Condition, associationSetName,
                    MappingErrorCode.InvalidContent, m_sourceLocation, navLineInfo, m_parsingErrors);
                return;
            }
            else
            {
                Debug.Assert(false, "XSD validation should ensure this");
            }
        }

        // <summary>
        // The method loads a function import mapping element
        // </summary>
        private void LoadFunctionImportMapping(XPathNavigator nav, EntityContainerMapping entityContainerMapping)
        {
            var lineInfo = (IXmlLineInfo)(nav.Clone());

            // Get target (store) function
            EdmFunction targetFunction;
            if (!TryGetFunctionImportStoreFunction(nav, out targetFunction))
            {
                return;
            }

            // Get source (model) function
            EdmFunction functionImport;
            if (!TryGetFunctionImportModelFunction(nav, entityContainerMapping, out functionImport))
            {
                return;
            }

            // Validate composability alignment of function import and target function.
            if (!functionImport.IsComposableAttribute
                && targetFunction.IsComposableAttribute)
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_TargetFunctionMustBeNonComposable(functionImport.FullName, targetFunction.FullName),
                    MappingErrorCode.MappingFunctionImportTargetFunctionMustBeNonComposable,
                    m_sourceLocation, lineInfo, m_parsingErrors);
                return;
            }
            else if (functionImport.IsComposableAttribute
                     && !targetFunction.IsComposableAttribute)
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_TargetFunctionMustBeComposable(functionImport.FullName, targetFunction.FullName),
                    MappingErrorCode.MappingFunctionImportTargetFunctionMustBeComposable,
                    m_sourceLocation, lineInfo, m_parsingErrors);
                return;
            }

            // Validate parameters are compatible between the store and model functions
            ValidateFunctionImportMappingParameters(nav, targetFunction, functionImport);

            // Process type mapping information
            var typeMappingsList = new List<List<FunctionImportStructuralTypeMapping>>();
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                var resultSetIndex = 0;
                do
                {
                    if (nav.LocalName
                        == MslConstructs.FunctionImportMappingResultMapping)
                    {
                        var typeMappings = GetFunctionImportMappingResultMapping(nav.Clone(), lineInfo, functionImport, resultSetIndex);
                        typeMappingsList.Add(typeMappings);
                    }
                    resultSetIndex++;
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            // Verify that there are the right number of result mappings
            if (typeMappingsList.Count > 0
                && typeMappingsList.Count != functionImport.ReturnParameters.Count)
            {
                AddToSchemaErrors(
                    Strings.Mapping_FunctionImport_ResultMappingCountDoesNotMatchResultCount(functionImport.Identity),
                    MappingErrorCode.FunctionResultMappingCountMismatch, m_sourceLocation, lineInfo, m_parsingErrors);
                return;
            }

            if (functionImport.IsComposableAttribute)
            {
                //
                // Add composable function import mapping to the list.
                //

                // Function mapping is allowed only for TVFs on the s-space.
                var cTypeTargetFunction = StoreItemCollection.ConvertToCTypeFunction(targetFunction);
                var cTypeTvfElementType = TypeHelpers.GetTvfReturnType(cTypeTargetFunction);
                var sTypeTvfElementType = TypeHelpers.GetTvfReturnType(targetFunction);
                if (cTypeTvfElementType == null)
                {
                    Debug.Assert(sTypeTvfElementType == null, "sTypeTvfElementType == null");
                    AddToSchemaErrors(
                        Strings.Mapping_FunctionImport_ResultMapping_InvalidSType(functionImport.Identity),
                        MappingErrorCode.MappingFunctionImportTVFExpected, m_sourceLocation, lineInfo, m_parsingErrors);
                    return;
                }

                Debug.Assert(
                    functionImport.ReturnParameters.Count == 1,
                    "functionImport.ReturnParameters.Count == 1 for a composable function import.");
                var typeMappings = typeMappingsList.Count > 0 ? typeMappingsList[0] : new List<FunctionImportStructuralTypeMapping>();

                FunctionImportMappingComposable mapping = null;
                EdmType resultType;
                if (MetadataHelper.TryGetFunctionImportReturnType(functionImport, 0, out resultType))
                {
                    var functionImportHelper = new FunctionImportMappingComposableHelper(
                        entityContainerMapping,
                        m_sourceLocation,
                        m_parsingErrors);

                    if (Helper.IsStructuralType(resultType))
                    {
                        if (!functionImportHelper.TryCreateFunctionImportMappingComposableWithStructuralResult(
                            functionImport,
                            cTypeTargetFunction,
                            typeMappings,
                            cTypeTvfElementType,
                            sTypeTvfElementType,
                            lineInfo,
                            out mapping))
                        {
                            return;
                        }
                    }
                    else
                    {
                        Debug.Assert(TypeSemantics.IsScalarType(resultType), "TypeSemantics.IsScalarType(resultType)");
                        Debug.Assert(typeMappings.Count == 0, "typeMappings.Count == 0");

                        if (!functionImportHelper.TryCreateFunctionImportMappingComposableWithScalarResult(
                            functionImport,
                            cTypeTargetFunction,
                            targetFunction,
                            resultType,
                            cTypeTvfElementType,
                            lineInfo,
                            out mapping))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    Debug.Fail("Composable function import must have return type.");
                }
                Debug.Assert(mapping != null, "mapping != null");

                entityContainerMapping.AddFunctionImportMapping(mapping);
            }
            else
            {
                //
                // Add non-composable function import mapping to the list.
                //

                var mapping = new FunctionImportMappingNonComposable(functionImport, targetFunction, typeMappingsList, EdmItemCollection);

                // Verify that all entity types can be produced.
                foreach (var resultMapping in mapping.InternalResultMappings)
                {
                    resultMapping.ValidateTypeConditions( /*validateAmbiguity: */false, m_parsingErrors, m_sourceLocation);
                }

                // Verify that function imports returning abstract types include explicit mappings
                for (var i = 0; i < mapping.InternalResultMappings.Count; i++)
                {
                    EntityType returnEntityType;
                    if (MetadataHelper.TryGetFunctionImportReturnType(functionImport, i, out returnEntityType)
                        &&
                        returnEntityType.Abstract
                        &&
                        mapping.GetResultMapping(i).NormalizedEntityTypeMappings.Count == 0)
                    {
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_FunctionImport_ImplicitMappingForAbstractReturnType, returnEntityType.FullName,
                            functionImport.Identity, MappingErrorCode.MappingOfAbstractType, m_sourceLocation, lineInfo,
                            m_parsingErrors);
                    }
                }

                entityContainerMapping.AddFunctionImportMapping(mapping);
            }
        }

        private bool TryGetFunctionImportStoreFunction(XPathNavigator nav, out EdmFunction targetFunction)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;
            targetFunction = null;

            // Get the function name
            var functionName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.FunctionImportMappingFunctionNameAttribute);

            // Try to find the function definition
            var functionOverloads = StoreItemCollection.GetFunctions(functionName);

            if (functionOverloads.Count == 0)
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_StoreFunctionDoesNotExist(functionName),
                    MappingErrorCode.MappingFunctionImportStoreFunctionDoesNotExist,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }
            else if (functionOverloads.Count > 1)
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_FunctionAmbiguous(functionName),
                    MappingErrorCode.MappingFunctionImportStoreFunctionAmbiguous,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }

            targetFunction = functionOverloads.Single();

            return true;
        }

        private bool TryGetFunctionImportModelFunction(
            XPathNavigator nav,
            EntityContainerMapping entityContainerMapping,
            out EdmFunction functionImport)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            // Get the function import name
            var functionImportName = GetAliasResolvedAttributeValue(
                nav.Clone(), MslConstructs.FunctionImportMappingFunctionImportNameAttribute);

            // Try to find the function import
            var modelContainer = entityContainerMapping.EdmEntityContainer;
            functionImport = null;
            foreach (var functionImportCandidate in modelContainer.FunctionImports)
            {
                if (functionImportCandidate.Name == functionImportName)
                {
                    functionImport = functionImportCandidate;
                    break;
                }
            }
            if (null == functionImport)
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_FunctionImportDoesNotExist(
                        functionImportName, entityContainerMapping.EdmEntityContainer.Name),
                    MappingErrorCode.MappingFunctionImportFunctionImportDoesNotExist,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }

            // check that no existing mapping exists for this function import
            FunctionImportMapping targetFunctionCollision;
            if (entityContainerMapping.TryGetFunctionImportMapping(functionImport, out targetFunctionCollision))
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_FunctionImportMappedMultipleTimes(functionImportName),
                    MappingErrorCode.MappingFunctionImportFunctionImportMappedMultipleTimes,
                    m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }
            return true;
        }

        private void ValidateFunctionImportMappingParameters(XPathNavigator nav, EdmFunction targetFunction, EdmFunction functionImport)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            foreach (var targetParameter in targetFunction.Parameters)
            {
                // find corresponding import parameter
                FunctionParameter importParameter;
                if (!functionImport.Parameters.TryGetValue(targetParameter.Name, false, out importParameter))
                {
                    AddToSchemaErrorWithMessage(
                        Strings.Mapping_FunctionImport_TargetParameterHasNoCorrespondingImportParameter(targetParameter.Name),
                        MappingErrorCode.MappingFunctionImportTargetParameterHasNoCorrespondingImportParameter,
                        m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                }
                else
                {
                    // parameters must have the same direction (in|out)
                    if (targetParameter.Mode
                        != importParameter.Mode)
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_FunctionImport_IncompatibleParameterMode(
                                targetParameter.Name, targetParameter.Mode, importParameter.Mode),
                            MappingErrorCode.MappingFunctionImportIncompatibleParameterMode,
                            m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                    }

                    var importType = Helper.AsPrimitive(importParameter.TypeUsage.EdmType);
                    Debug.Assert(importType != null, "Function import parameters must be primitive.");

                    if (Helper.IsSpatialType(importType))
                    {
                        importType = Helper.GetSpatialNormalizedPrimitiveType(importType);
                    }

                    var cspaceTargetType =
                        (PrimitiveType)StoreItemCollection.ProviderManifest.GetEdmType(targetParameter.TypeUsage).EdmType;
                    if (cspaceTargetType == null)
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_ProviderReturnsNullType(targetParameter.Name),
                            MappingErrorCode.MappingStoreProviderReturnsNullEdmType,
                            m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                        return;
                    }

                    // there are no type facets declared for function parameter types;
                    // we simply verify the primitive type kind is equivalent. 
                    // for enums we just use the underlying enum type.
                    if (cspaceTargetType.PrimitiveTypeKind
                        != importType.PrimitiveTypeKind)
                    {
                        var schemaErrorMessage = Helper.IsEnumType(importParameter.TypeUsage.EdmType)
                                                     ? Strings.Mapping_FunctionImport_IncompatibleEnumParameterType(
                                                         targetParameter.Name,
                                                         cspaceTargetType.Name,
                                                         importParameter.TypeUsage.EdmType.FullName,
                                                         Helper.GetUnderlyingEdmTypeForEnumType(importParameter.TypeUsage.EdmType).Name)
                                                     : Strings.Mapping_FunctionImport_IncompatibleParameterType(
                                                         targetParameter.Name,
                                                         cspaceTargetType.Name,
                                                         importType.Name);

                        AddToSchemaErrorWithMessage(
                            schemaErrorMessage,
                            MappingErrorCode.MappingFunctionImportIncompatibleParameterType,
                            m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                    }
                }
            }

            foreach (var importParameter in functionImport.Parameters)
            {
                // find corresponding target parameter
                FunctionParameter targetParameter;
                if (!targetFunction.Parameters.TryGetValue(importParameter.Name, false, out targetParameter))
                {
                    AddToSchemaErrorWithMessage(
                        Strings.Mapping_FunctionImport_ImportParameterHasNoCorrespondingTargetParameter(importParameter.Name),
                        MappingErrorCode.MappingFunctionImportImportParameterHasNoCorrespondingTargetParameter,
                        m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                }
            }
        }

        private List<FunctionImportStructuralTypeMapping> GetFunctionImportMappingResultMapping(
            XPathNavigator nav,
            IXmlLineInfo functionImportMappingLineInfo,
            EdmFunction functionImport,
            int resultSetIndex)
        {
            var typeMappings = new List<FunctionImportStructuralTypeMapping>();

            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    var entitySet = functionImport.EntitySets.Count > resultSetIndex
                                        ? functionImport.EntitySets[resultSetIndex]
                                        : null;

                    if (nav.LocalName
                        == MslConstructs.EntityTypeMappingElement)
                    {
                        EntityType resultEntityType;
                        if (MetadataHelper.TryGetFunctionImportReturnType(functionImport, resultSetIndex, out resultEntityType))
                        {
                            // Cannot specify an entity type mapping for a function import that does not return members of an entity set.
                            if (entitySet == null)
                            {
                                AddToSchemaErrors(
                                    Strings.Mapping_FunctionImport_EntityTypeMappingForFunctionNotReturningEntitySet(
                                        MslConstructs.EntityTypeMappingElement, functionImport.Identity),
                                    MappingErrorCode.MappingFunctionImportEntityTypeMappingForFunctionNotReturningEntitySet,
                                    m_sourceLocation, functionImportMappingLineInfo, m_parsingErrors);
                            }

                            FunctionImportEntityTypeMapping typeMapping;
                            if (TryLoadFunctionImportEntityTypeMapping(
                                nav.Clone(),
                                resultEntityType,
                                (EntityType e) => Strings.Mapping_FunctionImport_InvalidContentEntityTypeForEntitySet(
                                    e.FullName,
                                    resultEntityType.FullName,
                                    entitySet.Name,
                                    functionImport.Identity),
                                out typeMapping))
                            {
                                typeMappings.Add(typeMapping);
                            }
                        }
                        else
                        {
                            AddToSchemaErrors(
                                Strings.Mapping_FunctionImport_ResultMapping_InvalidCTypeETExpected(functionImport.Identity),
                                MappingErrorCode.MappingFunctionImportUnexpectedEntityTypeMapping,
                                m_sourceLocation, functionImportMappingLineInfo, m_parsingErrors);
                        }
                    }
                    else if (nav.LocalName
                             == MslConstructs.ComplexTypeMappingElement)
                    {
                        ComplexType resultComplexType;
                        if (MetadataHelper.TryGetFunctionImportReturnType(functionImport, resultSetIndex, out resultComplexType))
                        {
                            Debug.Assert(entitySet == null, "entitySet == null for complex type mapping in function imports.");

                            FunctionImportComplexTypeMapping typeMapping;
                            if (TryLoadFunctionImportComplexTypeMapping(nav.Clone(), resultComplexType, functionImport, out typeMapping))
                            {
                                typeMappings.Add(typeMapping);
                            }
                        }
                        else
                        {
                            AddToSchemaErrors(
                                Strings.Mapping_FunctionImport_ResultMapping_InvalidCTypeCTExpected(functionImport.Identity),
                                MappingErrorCode.MappingFunctionImportUnexpectedComplexTypeMapping,
                                m_sourceLocation, functionImportMappingLineInfo, m_parsingErrors);
                        }
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            return typeMappings;
        }

        private bool TryLoadFunctionImportComplexTypeMapping(
            XPathNavigator nav,
            ComplexType resultComplexType,
            EdmFunction functionImport,
            out FunctionImportComplexTypeMapping typeMapping)
        {
            typeMapping = null;
            var lineInfo = new LineInfo(nav);

            ComplexType complexType;
            if (!TryParseComplexTypeAttribute(nav, resultComplexType, functionImport, out complexType))
            {
                return false;
            }

            var columnRenameMappings = new Collection<FunctionImportReturnTypePropertyMapping>();

            if (!LoadFunctionImportStructuralType(
                nav.Clone(), new List<StructuralType>
                                 {
                                     complexType
                                 }, columnRenameMappings, null))
            {
                return false;
            }

            typeMapping = new FunctionImportComplexTypeMapping(complexType, columnRenameMappings, lineInfo);
            return true;
        }

        private bool TryParseComplexTypeAttribute(
            XPathNavigator nav, ComplexType resultComplexType, EdmFunction functionImport, out ComplexType complexType)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;
            var complexTypeName = GetAttributeValue(nav.Clone(), MslConstructs.ComplexTypeMappingTypeNameAttribute);
            complexTypeName = GetAliasResolvedValue(complexTypeName);

            if (!EdmItemCollection.TryGetItem(complexTypeName, out complexType))
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Complex_Type, complexTypeName,
                    MappingErrorCode.InvalidComplexType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }

            if (!Helper.IsAssignableFrom(resultComplexType, complexType))
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_FunctionImport_ResultMapping_MappedTypeDoesNotMatchReturnType(
                        functionImport.Identity, complexType.FullName),
                    MappingErrorCode.InvalidComplexType, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                return false;
            }

            return true;
        }

        private bool TryLoadFunctionImportEntityTypeMapping(
            XPathNavigator nav,
            EntityType resultEntityType,
            Func<EntityType, string> registerEntityTypeMismatchError,
            out FunctionImportEntityTypeMapping typeMapping)
        {
            typeMapping = null;
            var lineInfo = new LineInfo(nav);

            // Process entity type.
            GetAttributeValue(nav.Clone(), MslConstructs.EntitySetMappingTypeNameAttribute);
            Set<EntityType> isOfTypeEntityTypes;
            Set<EntityType> entityTypes;
            {
                // Verify the entity type is appropriate to the function import's result entity type.
                if (
                    !TryParseEntityTypeAttribute(
                        nav.Clone(), resultEntityType, registerEntityTypeMismatchError, out isOfTypeEntityTypes, out entityTypes))
                {
                    return false;
                }
            }

            var currentTypesInHierarchy = isOfTypeEntityTypes.Concat(entityTypes).Distinct().OfType<StructuralType>();
            var columnRenameMappings = new Collection<FunctionImportReturnTypePropertyMapping>();

            // Process all conditions and column renames.
            var conditions = new List<FunctionImportEntityTypeMappingCondition>();

            if (!LoadFunctionImportStructuralType(nav.Clone(), currentTypesInHierarchy, columnRenameMappings, conditions))
            {
                return false;
            }

            typeMapping = new FunctionImportEntityTypeMapping(isOfTypeEntityTypes, entityTypes, conditions, columnRenameMappings, lineInfo);
            return true;
        }

        private bool LoadFunctionImportStructuralType(
            XPathNavigator nav,
            IEnumerable<StructuralType> currentTypes,
            Collection<FunctionImportReturnTypePropertyMapping> columnRenameMappings,
            List<FunctionImportEntityTypeMappingCondition> conditions)
        {
            DebugCheck.NotNull(columnRenameMappings);
            DebugCheck.NotNull(nav);
            DebugCheck.NotNull(currentTypes);

            var lineInfo = (IXmlLineInfo)(nav.Clone());

            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    if (nav.LocalName
                        == MslConstructs.ScalarPropertyElement)
                    {
                        LoadFunctionImportStructuralTypeMappingScalarProperty(nav, columnRenameMappings, currentTypes);
                    }
                    if (nav.LocalName
                        == MslConstructs.ConditionElement)
                    {
                        LoadFunctionImportEntityTypeMappingCondition(nav, conditions);
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            var errorFound = false;
            if (null != conditions)
            {
                // make sure a single condition is specified per column
                var columnsWithConditions = new HashSet<string>();
                foreach (var condition in conditions)
                {
                    if (!columnsWithConditions.Add(condition.ColumnName))
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_InvalidContent_Duplicate_Condition_Member(condition.ColumnName),
                            MappingErrorCode.ConditionError,
                            m_sourceLocation, lineInfo, m_parsingErrors);
                        errorFound = true;
                    }
                }
            }
            return !errorFound;
        }

        private void LoadFunctionImportStructuralTypeMappingScalarProperty(
            XPathNavigator nav,
            Collection<FunctionImportReturnTypePropertyMapping> columnRenameMappings,
            IEnumerable<StructuralType> currentTypes)
        {
            var lineInfo = new LineInfo(nav);
            var memberName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ScalarPropertyNameAttribute);
            var columnName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ScalarPropertyColumnNameAttribute);

            // Negative case: the property name is invalid
            if (!currentTypes.All(t => t.Members.Contains(memberName)))
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_InvalidContent_Cdm_Member(memberName),
                    MappingErrorCode.InvalidEdmMember,
                    m_sourceLocation, lineInfo, m_parsingErrors);
            }

            if (columnRenameMappings.Any(m => m.CMember == memberName))
            {
                // Negative case: duplicate member name mapping in one type rename mapping
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_InvalidContent_Duplicate_Cdm_Member(memberName),
                    MappingErrorCode.DuplicateMemberMapping,
                    m_sourceLocation, lineInfo, m_parsingErrors);
            }
            else
            {
                columnRenameMappings.Add(new FunctionImportReturnTypeScalarPropertyMapping(memberName, columnName, lineInfo));
            }
        }

        private void LoadFunctionImportEntityTypeMappingCondition(
            XPathNavigator nav, List<FunctionImportEntityTypeMappingCondition> conditions)
        {
            var lineInfo = new LineInfo(nav);

            var columnName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ConditionColumnNameAttribute);
            var value = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ConditionValueAttribute);
            var isNull = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ConditionIsNullAttribute);

            //Either Value or NotNull need to be specifid on the condition mapping but not both
            if ((isNull != null)
                && (value != null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Both_Values,
                    MappingErrorCode.ConditionError, m_sourceLocation, lineInfo, m_parsingErrors);
            }
            else if ((isNull == null)
                     && (value == null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Either_Values,
                    MappingErrorCode.ConditionError, m_sourceLocation, lineInfo, m_parsingErrors);
            }
            else
            {
                if (isNull != null)
                {
                    var isNullValue = Convert.ToBoolean(isNull, CultureInfo.InvariantCulture);
                    conditions.Add(new FunctionImportEntityTypeMappingConditionIsNull(columnName, isNullValue, lineInfo));
                }
                else
                {
                    var columnValue = nav.Clone();
                    columnValue.MoveToAttribute(MslConstructs.ConditionValueAttribute, string.Empty);
                    conditions.Add(new FunctionImportEntityTypeMappingConditionValue(columnName, columnValue, lineInfo));
                }
            }
        }

        // <summary>
        // The method loads the child nodes for the AssociationType Mapping node
        // into the internal datastructures.
        // </summary>
        private void LoadAssociationTypeMapping(
            XPathNavigator nav, AssociationSetMapping associationSetMapping, string associationTypeName, string tableName,
            EntityContainer storageEntityContainerType)
        {
            var navLineInfo = (IXmlLineInfo)nav;

            //Get the association type for association type name specified in MSL
            //If no AssociationType with the given name exists, add a schema error and return
            AssociationType associationType;
            EdmItemCollection.TryGetItem(associationTypeName, out associationType);
            if (associationType == null)
            {
                //There is no point in continuing loading if the AssociationType is null
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Association_Type, associationTypeName,
                    MappingErrorCode.InvalidAssociationType, m_sourceLocation, navLineInfo, m_parsingErrors);
                return;
            }
            //Verify that AssociationType specified should be the declared type of
            //AssociationSet or a derived Type of it.
            //Future Enhancement : Change the code to use EdmEquals
            if ((!(associationSetMapping.Set.ElementType.Equals(associationType))))
            {
                AddToSchemaErrorWithMessage(
                    Strings.Mapping_Invalid_Association_Type_For_Association_Set(
                        associationTypeName,
                        associationSetMapping.Set.ElementType.FullName, associationSetMapping.Set.Name),
                    MappingErrorCode.DuplicateTypeMapping, m_sourceLocation, navLineInfo, m_parsingErrors);
                return;
            }

            //Create an AssociationTypeMapping to hold the information for AssociationType mapping.
            var associationTypeMapping = new AssociationTypeMapping(associationType, associationSetMapping);
            associationSetMapping.AssociationTypeMapping = associationTypeMapping;
            //If the table name was not specified on the AssociationSetMapping element 
            //Then there should have been a query view. Otherwise throw.
            if (String.IsNullOrEmpty(tableName)
                && (associationSetMapping.QueryView == null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_Table_Expected, MappingErrorCode.InvalidTable,
                    m_sourceLocation, navLineInfo, m_parsingErrors);
            }
            else
            {
                var fragment = LoadAssociationMappingFragment(
                    nav.Clone(), associationSetMapping, associationTypeMapping, tableName, storageEntityContainerType);
                if (fragment != null)
                {
                    //Fragment can be null because of validation errors
                    associationTypeMapping.MappingFragment = fragment;
                }
            }
        }

        // <summary>
        // Loads function mappings for the entity type.
        // </summary>
        private void LoadAssociationTypeModificationFunctionMapping(
            XPathNavigator nav,
            AssociationSetMapping associationSetMapping)
        {
            // create function loader
            var functionLoader = new ModificationFunctionMappingLoader(this, associationSetMapping.Set);

            // Load all function definitions (for insert, delete and update)
            ModificationFunctionMapping deleteFunctionMapping = null;
            ModificationFunctionMapping insertFunctionMapping = null;
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    switch (nav.LocalName)
                    {
                        case MslConstructs.DeleteFunctionElement:
                            deleteFunctionMapping = functionLoader.LoadAssociationSetModificationFunctionMapping(
                                nav.Clone(), associationSetMapping.Set, false);
                            break;
                        case MslConstructs.InsertFunctionElement:
                            insertFunctionMapping = functionLoader.LoadAssociationSetModificationFunctionMapping(
                                nav.Clone(), associationSetMapping.Set, true);
                            break;
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            // register function mapping information
            associationSetMapping.ModificationFunctionMapping = new AssociationSetModificationFunctionMapping(
                (AssociationSet)associationSetMapping.Set, deleteFunctionMapping, insertFunctionMapping);
        }

        // <summary>
        // The method loads the child nodes for the TableMappingFragment under the EntityType node
        // into the internal datastructures.
        // </summary>
        private MappingFragment LoadMappingFragment(
            XPathNavigator nav,
            EntityTypeMapping typeMapping,
            string tableName,
            EntityContainer storageEntityContainerType,
            bool distinctFlag)
        {
            var navLineInfo = (IXmlLineInfo)nav;

            //First make sure that there was no QueryView specified for this Set
            if (typeMapping.SetMapping.QueryView != null)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_QueryView_PropertyMaps, typeMapping.SetMapping.Set.Name,
                    MappingErrorCode.PropertyMapsWithQueryView, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }

            //Get the table type that represents this table
            EntitySet tableMember;
            storageEntityContainerType.TryGetEntitySetByName(tableName, false /*ignoreCase*/, out tableMember);
            if (tableMember == null)
            {
                //There is no point in continuing loading if the Table on S side can not be found
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Table, tableName,
                    MappingErrorCode.InvalidTable, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }
            var tableType = tableMember.ElementType;
            //Create a table mapping fragment to hold the mapping information for a TableMappingFragment node
            var fragment = new MappingFragment(tableMember, typeMapping, distinctFlag);
            //Set the Start Line Information on Fragment
            fragment.StartLineNumber = navLineInfo.LineNumber;
            fragment.StartLinePosition = navLineInfo.LinePosition;

            //Go through the property mappings for this TableMappingFragment and load them in memory.
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    //need to get the type that this member exists in
                    EdmType containerType = null;
                    var propertyName = GetAttributeValue(nav.Clone(), MslConstructs.ComplexPropertyNameAttribute);
                    //PropertyName could be null for Condition Maps
                    if (propertyName != null)
                    {
                        containerType = typeMapping.GetContainerType(propertyName);
                    }
                    switch (nav.LocalName)
                    {
                        case MslConstructs.ScalarPropertyElement:
                            var scalarMap = LoadScalarPropertyMapping(nav.Clone(), containerType, tableType.Properties);
                            if (scalarMap != null)
                            {
                                //scalarMap can be null in invalid cases
                                fragment.AddPropertyMapping(scalarMap);
                            }
                            break;
                        case MslConstructs.ComplexPropertyElement:
                            var complexMap =
                                LoadComplexPropertyMapping(nav.Clone(), containerType, tableType.Properties);
                            //Complex Map can be null in case of invalid MSL files.
                            if (complexMap != null)
                            {
                                fragment.AddPropertyMapping(complexMap);
                            }
                            break;
                        case MslConstructs.ConditionElement:
                            var conditionMap =
                                LoadConditionPropertyMapping(nav.Clone(), containerType, tableType.Properties);
                            //conditionMap can be null in cases of invalid Map
                            if (conditionMap != null)
                            {
                                fragment.AddConditionProperty(
                                    conditionMap, duplicateMemberConditionError: (member) =>
                                                                                     {
                                                                                         AddToSchemaErrorsWithMemberInfo(
                                                                                             Strings.
                                                                                                 Mapping_InvalidContent_Duplicate_Condition_Member,
                                                                                             member.Name,
                                                                                             MappingErrorCode.ConditionError,
                                                                                             m_sourceLocation, navLineInfo, m_parsingErrors);
                                                                                     });
                            }
                            break;
                        default:
                            AddToSchemaErrors(
                                Strings.Mapping_InvalidContent_General,
                                MappingErrorCode.InvalidContent, m_sourceLocation, navLineInfo, m_parsingErrors);
                            break;
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }

            nav.MoveToChild(XPathNodeType.Element);
            return fragment;
        }

        // <summary>
        // The method loads the child nodes for the TableMappingFragment under the AssociationType node
        // into the internal datastructures.
        // </summary>
        private MappingFragment LoadAssociationMappingFragment(
            XPathNavigator nav, AssociationSetMapping setMapping, AssociationTypeMapping typeMapping, string tableName,
            EntityContainer storageEntityContainerType)
        {
            var navLineInfo = (IXmlLineInfo)nav;
            MappingFragment fragment = null;
            EntityType tableType = null;

            //If there is a query view, Dont create a mapping fragment since there should n't be one
            if (setMapping.QueryView == null)
            {
                //Get the table type that represents this table
                EntitySet tableMember;
                storageEntityContainerType.TryGetEntitySetByName(tableName, false /*ignoreCase*/, out tableMember);
                if (tableMember == null)
                {
                    //There is no point in continuing loading if the Table is null
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_Table, tableName,
                        MappingErrorCode.InvalidTable, m_sourceLocation, navLineInfo, m_parsingErrors);
                    return null;
                }
                tableType = tableMember.ElementType;
                //Create a Mapping fragment and load all the End node under it
                fragment = new MappingFragment(tableMember, typeMapping, false /*No distinct flag*/);
                //Set the Start Line Information on Fragment, For AssociationSet there are 
                //no fragments, so the start Line Info is same as that of Set
                fragment.StartLineNumber = setMapping.StartLineNumber;
                fragment.StartLinePosition = setMapping.StartLinePosition;
            }

            do
            {
                //need to get the type that this member exists in
                switch (nav.LocalName)
                {
                    case MslConstructs.EndPropertyMappingElement:
                        //Make sure that there was no QueryView specified for this Set
                        if (setMapping.QueryView != null)
                        {
                            AddToSchemaErrorsWithMemberInfo(
                                Strings.Mapping_QueryView_PropertyMaps, setMapping.Set.Name,
                                MappingErrorCode.PropertyMapsWithQueryView, m_sourceLocation, navLineInfo, m_parsingErrors);
                            return null;
                        }
                        var endName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.EndPropertyMappingNameAttribute);
                        EdmMember endMember = null;
                        typeMapping.AssociationType.Members.TryGetValue(endName, false, out endMember);
                        var end = endMember as AssociationEndMember;
                        if (end == null)
                        {
                            //Don't try to load the end property map if the end property itself is null
                            AddToSchemaErrorsWithMemberInfo(
                                Strings.Mapping_InvalidContent_End, endName,
                                MappingErrorCode.InvalidEdmMember, m_sourceLocation, navLineInfo, m_parsingErrors);
                            continue;
                        }
                        fragment.AddPropertyMapping((LoadEndPropertyMapping(nav.Clone(), end, tableType)));
                        break;
                    case MslConstructs.ConditionElement:
                        //Make sure that there was no QueryView specified for this Set
                        if (setMapping.QueryView != null)
                        {
                            AddToSchemaErrorsWithMemberInfo(
                                Strings.Mapping_QueryView_PropertyMaps, setMapping.Set.Name,
                                MappingErrorCode.PropertyMapsWithQueryView, m_sourceLocation, navLineInfo, m_parsingErrors);
                            return null;
                        }
                        //Need to add validation for conditions in Association mapping fragment.
                        var conditionMap = LoadConditionPropertyMapping(nav.Clone(), null /*containerType*/, tableType.Properties);
                        //conditionMap can be null in cases of invalid Map
                        if (conditionMap != null)
                        {
                            fragment.AddConditionProperty(
                                conditionMap, duplicateMemberConditionError: (member) =>
                                                                                 {
                                                                                     AddToSchemaErrorsWithMemberInfo(
                                                                                         Strings.
                                                                                             Mapping_InvalidContent_Duplicate_Condition_Member,
                                                                                         member.Name,
                                                                                         MappingErrorCode.ConditionError,
                                                                                         m_sourceLocation, navLineInfo, m_parsingErrors);
                                                                                 });
                        }
                        break;
                    case MslConstructs.ModificationFunctionMappingElement:
                        setMapping.HasModificationFunctionMapping = true;
                        LoadAssociationTypeModificationFunctionMapping(nav.Clone(), setMapping);
                        break;
                    default:
                        AddToSchemaErrors(
                            Strings.Mapping_InvalidContent_General,
                            MappingErrorCode.InvalidContent, m_sourceLocation, navLineInfo, m_parsingErrors);
                        break;
                }
            }
            while (nav.MoveToNext(XPathNodeType.Element));

            return fragment;
        }

        // <summary>
        // The method loads the ScalarProperty mapping
        // into the internal datastructures.
        // </summary>
        private ScalarPropertyMapping LoadScalarPropertyMapping(
            XPathNavigator nav, EdmType containerType, ReadOnlyMetadataCollection<EdmProperty> tableProperties)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            //Get the property name from MSL.
            var propertyName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ScalarPropertyNameAttribute);
            EdmProperty member = null;
            if (!String.IsNullOrEmpty(propertyName))
            {
                //If the container type is a collection type, there wouldn't be a member to represent this scalar property
                if (containerType == null
                    || !(Helper.IsCollectionType(containerType)))
                {
                    //If container type is null that means we have not found the member in any of the IsOfTypes.
                    if (containerType != null)
                    {
                        if (Helper.IsRefType(containerType))
                        {
                            var refType = (RefType)containerType;
                            ((EntityType)refType.ElementType).Properties.TryGetValue(propertyName, false /*ignoreCase*/, out member);
                        }
                        else
                        {
                            EdmMember tempMember;
                            (containerType as StructuralType).Members.TryGetValue(propertyName, false, out tempMember);
                            member = tempMember as EdmProperty;
                        }
                    }
                    if (member == null)
                    {
                        AddToSchemaErrorsWithMemberInfo(
                            Strings.Mapping_InvalidContent_Cdm_Member, propertyName,
                            MappingErrorCode.InvalidEdmMember, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
                    }
                }
            }
            //Get the property from Storeside
            var columnName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ScalarPropertyColumnNameAttribute);
            Debug.Assert(columnName != null, "XSD validation should have caught this");
            EdmProperty columnMember;
            tableProperties.TryGetValue(columnName, false, out columnMember);
            if (columnMember == null)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_Column, columnName,
                    MappingErrorCode.InvalidStorageMember, m_sourceLocation, xmlLineInfoNav, m_parsingErrors);
            }
            //Don't create scalar property map if the property or column metadata is null
            if ((member == null)
                || (columnMember == null))
            {
                return null;
            }

            if (!Helper.IsScalarType(member.TypeUsage.EdmType))
            {
                var error = new EdmSchemaError(
                    Strings.Mapping_Invalid_CSide_ScalarProperty(
                        member.Name),
                    (int)MappingErrorCode.InvalidTypeInScalarProperty,
                    EdmSchemaErrorSeverity.Error,
                    m_sourceLocation,
                    xmlLineInfoNav.LineNumber,
                    xmlLineInfoNav.LinePosition);
                m_parsingErrors.Add(error);
                return null;
            }

            ValidateAndUpdateScalarMemberMapping(member, columnMember, xmlLineInfoNav);
            var scalarPropertyMapping = new ScalarPropertyMapping(member, columnMember);
            return scalarPropertyMapping;
        }

        // <summary>
        // The method loads the ComplexProperty mapping into the internal datastructures.
        // </summary>
        private ComplexPropertyMapping LoadComplexPropertyMapping(
            XPathNavigator nav, EdmType containerType, ReadOnlyMetadataCollection<EdmProperty> tableProperties)
        {
            var navLineInfo = (IXmlLineInfo)nav;

            var collectionType = containerType as CollectionType;
            //Get the property name from MSL
            var propertyName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ComplexPropertyNameAttribute);
            //Get the member metadata from the contianer type passed in.
            //But if the continer type is collection type, there would n't be any member to represent the member.
            EdmProperty member = null;
            EdmType memberType = null;
            //If member specified the type name, it takes precedence
            var memberTypeName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ComplexTypeMappingTypeNameAttribute);
            var containerStructuralType = containerType as StructuralType;

            if (String.IsNullOrEmpty(memberTypeName))
            {
                if (collectionType == null)
                {
                    if (containerStructuralType != null)
                    {
                        EdmMember tempMember;
                        containerStructuralType.Members.TryGetValue(propertyName, false /*ignoreCase*/, out tempMember);
                        member = tempMember as EdmProperty;
                        if (member == null)
                        {
                            AddToSchemaErrorsWithMemberInfo(
                                Strings.Mapping_InvalidContent_Cdm_Member, propertyName,
                                MappingErrorCode.InvalidEdmMember, m_sourceLocation, navLineInfo, m_parsingErrors);
                        }
                        memberType = member.TypeUsage.EdmType;
                    }
                    else
                    {
                        AddToSchemaErrorsWithMemberInfo(
                            Strings.Mapping_InvalidContent_Cdm_Member, propertyName,
                            MappingErrorCode.InvalidEdmMember, m_sourceLocation, navLineInfo, m_parsingErrors);
                    }
                }
                else
                {
                    memberType = collectionType.TypeUsage.EdmType;
                }
            }
            else
            {
                //If container type is null that means we have not found the member in any of the IsOfTypes.
                if (containerType != null)
                {
                    EdmMember tempMember;
                    containerStructuralType.Members.TryGetValue(propertyName, false /*ignoreCase*/, out tempMember);
                    member = tempMember as EdmProperty;
                }
                if (member == null)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_Cdm_Member, propertyName,
                        MappingErrorCode.InvalidEdmMember, m_sourceLocation, navLineInfo, m_parsingErrors);
                }
                EdmItemCollection.TryGetItem(memberTypeName, out memberType);
                memberType = memberType as ComplexType;
                // If member type is null, that means the type wasn't found in the workspace
                if (memberType == null)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_InvalidContent_Complex_Type, memberTypeName,
                        MappingErrorCode.InvalidComplexType, m_sourceLocation, navLineInfo, m_parsingErrors);
                }
            }

            var complexPropertyMapping = new ComplexPropertyMapping(member);

            var cloneNav = nav.Clone();
            var hasComplexTypeMappingElements = false;
            if (cloneNav.MoveToChild(XPathNodeType.Element))
            {
                if (cloneNav.LocalName
                    == MslConstructs.ComplexTypeMappingElement)
                {
                    hasComplexTypeMappingElements = true;
                }
            }

            //There is no point in continuing if the complex member or complex member type is null
            if ((member == null)
                || (memberType == null))
            {
                return null;
            }

            if (hasComplexTypeMappingElements)
            {
                nav.MoveToChild(XPathNodeType.Element);
                do
                {
                    complexPropertyMapping.AddTypeMapping(LoadComplexTypeMapping(nav.Clone(), null, tableProperties));
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }
            else
            {
                complexPropertyMapping.AddTypeMapping(LoadComplexTypeMapping(nav.Clone(), memberType, tableProperties));
            }
            return complexPropertyMapping;
        }

        private ComplexTypeMapping LoadComplexTypeMapping(
            XPathNavigator nav, EdmType type, ReadOnlyMetadataCollection<EdmProperty> tableType)
        {
            //Get the IsPartial attribute from MSL
            var isPartial = false;
            var partialAttribute = GetAttributeValue(nav.Clone(), MslConstructs.ComplexPropertyIsPartialAttribute);
            if (!String.IsNullOrEmpty(partialAttribute))
            {
                //XSD validation should have guarenteed that the attribute value can only be true or false
                Debug.Assert(partialAttribute == "true" || partialAttribute == "false");
                isPartial = Convert.ToBoolean(partialAttribute, CultureInfo.InvariantCulture);
            }
            //Create an ComplexTypeMapping to hold the information for Type mapping.
            var typeMapping = new ComplexTypeMapping(isPartial);
            if (type != null)
            {
                typeMapping.AddType(type as ComplexType);
            }
            else
            {
                Debug.Assert(nav.LocalName == MslConstructs.ComplexTypeMappingElement);
                var typeName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ComplexTypeMappingTypeNameAttribute);
                var index = typeName.IndexOf(MslConstructs.TypeNameSperator);
                string currentTypeName = null;
                do
                {
                    if (index != -1)
                    {
                        currentTypeName = typeName.Substring(0, index);
                        typeName = typeName.Substring(index + 1, (typeName.Length - (index + 1)));
                    }
                    else
                    {
                        currentTypeName = typeName;
                        typeName = string.Empty;
                    }

                    var isTypeOfIndex = currentTypeName.IndexOf(MslConstructs.IsTypeOf, StringComparison.Ordinal);
                    if (isTypeOfIndex == 0)
                    {
                        currentTypeName = currentTypeName.Substring(
                            MslConstructs.IsTypeOf.Length, (currentTypeName.Length - (MslConstructs.IsTypeOf.Length + 1)));
                        currentTypeName = GetAliasResolvedValue(currentTypeName);
                    }
                    else
                    {
                        currentTypeName = GetAliasResolvedValue(currentTypeName);
                    }
                    ComplexType complexType;
                    EdmItemCollection.TryGetItem(currentTypeName, out complexType);
                    if (complexType == null)
                    {
                        AddToSchemaErrorsWithMemberInfo(
                            Strings.Mapping_InvalidContent_Complex_Type, currentTypeName,
                            MappingErrorCode.InvalidComplexType, m_sourceLocation, (IXmlLineInfo)nav, m_parsingErrors);
                        index = typeName.IndexOf(MslConstructs.TypeNameSperator);
                        continue;
                    }
                    if (isTypeOfIndex == 0)
                    {
                        typeMapping.AddIsOfType(complexType);
                    }
                    else
                    {
                        typeMapping.AddType(complexType);
                    }
                    index = typeName.IndexOf(MslConstructs.TypeNameSperator);
                }
                while (typeName.Length != 0);
            }

            //Now load the children of ComplexTypeMapping
            if (nav.MoveToChild(XPathNodeType.Element))
            {
                do
                {
                    EdmType containerType =
                        typeMapping.GetOwnerType(GetAttributeValue(nav.Clone(), MslConstructs.ComplexPropertyNameAttribute));
                    switch (nav.LocalName)
                    {
                        case MslConstructs.ScalarPropertyElement:
                            var scalarMap =
                                LoadScalarPropertyMapping(nav.Clone(), containerType, tableType);
                            //ScalarMap can be null in case of invalid MSL files
                            if (scalarMap != null)
                            {
                                typeMapping.AddPropertyMapping(scalarMap);
                            }
                            break;
                        case MslConstructs.ComplexPropertyElement:
                            var complexMap =
                                LoadComplexPropertyMapping(nav.Clone(), containerType, tableType);
                            //complexMap can be null in case of invalid maps
                            if (complexMap != null)
                            {
                                typeMapping.AddPropertyMapping(complexMap);
                            }
                            break;
                        case MslConstructs.ConditionElement:
                            var conditionMap =
                                LoadConditionPropertyMapping(nav.Clone(), containerType, tableType);
                            if (conditionMap != null)
                            {
                                typeMapping.AddConditionProperty(
                                    conditionMap, duplicateMemberConditionError: (member) =>
                                                                                     {
                                                                                         AddToSchemaErrorsWithMemberInfo(
                                                                                             Strings.
                                                                                                 Mapping_InvalidContent_Duplicate_Condition_Member,
                                                                                             member.Name,
                                                                                             MappingErrorCode.ConditionError,
                                                                                             m_sourceLocation, (IXmlLineInfo)nav,
                                                                                             m_parsingErrors);
                                                                                     });
                            }
                            break;
                        default:
                            throw Error.NotSupported();
                    }
                }
                while (nav.MoveToNext(XPathNodeType.Element));
            }
            return typeMapping;
        }

        // <summary>
        // The method loads the EndProperty mapping
        // into the internal datastructures.
        // </summary>
        private EndPropertyMapping LoadEndPropertyMapping(XPathNavigator nav, AssociationEndMember end, EntityType tableType)
        {
            //FutureEnhancement : Change End Property Mapping to not derive from
            //                    PropertyMapping
            var endMapping =
                new EndPropertyMapping()
                    {
                        AssociationEnd = end
                    };

            nav.MoveToChild(XPathNodeType.Element);
            do
            {
                switch (nav.LocalName)
                {
                    case MslConstructs.ScalarPropertyElement:
                        var endRef = end.TypeUsage.EdmType as RefType;
                        Debug.Assert(endRef != null);
                        var containerType = endRef.ElementType;
                        var scalarMap = LoadScalarPropertyMapping(nav.Clone(), containerType, tableType.Properties);
                        //Scalar Property Mapping can be null
                        //in case of invalid MSL files.
                        if (scalarMap != null)
                        {
                            //Make sure that the properties mapped as part of EndProperty maps are the key properties.
                            //If any other property is mapped, we should raise an error.
                            if (!containerType.KeyMembers.Contains(scalarMap.Property))
                            {
                                var navLineInfo = (IXmlLineInfo)nav;
                                AddToSchemaErrorsWithMemberInfo(
                                    Strings.Mapping_InvalidContent_EndProperty, scalarMap.Property.Name,
                                    MappingErrorCode.InvalidEdmMember, m_sourceLocation, navLineInfo, m_parsingErrors);
                                return null;
                            }
                            endMapping.AddPropertyMapping(scalarMap);
                        }
                        break;
                    default:
                        Debug.Fail("XSD validation should have ensured that End EdmProperty Maps only have Schalar properties");
                        break;
                }
            }
            while (nav.MoveToNext(XPathNodeType.Element));
            return endMapping;
        }

        // <summary>
        // The method loads the ConditionProperty mapping
        // into the internal datastructures.
        // </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private ConditionPropertyMapping LoadConditionPropertyMapping(
            XPathNavigator nav, EdmType containerType, ReadOnlyMetadataCollection<EdmProperty> tableProperties)
        {
            //Get the CDM side property name.
            var propertyName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ConditionNameAttribute);
            //Get the Store side property name from Storeside
            var columnName = GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ConditionColumnNameAttribute);

            var navLineInfo = (IXmlLineInfo)nav;

            //Either the property name or column name can be specified but both can not be.
            if ((propertyName != null)
                && (columnName != null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Both_Members,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }
            if ((propertyName == null)
                && (columnName == null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Either_Members,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }

            EdmProperty member = null;
            //Get the CDM EdmMember reprsented by the name specified.
            if (propertyName != null)
            {
                EdmMember tempMember;
                //If container type is null that means we have not found the member in any of the IsOfTypes.
                if (containerType != null)
                {
                    ((StructuralType)containerType).Members.TryGetValue(propertyName, false /*ignoreCase*/, out tempMember);
                    member = tempMember as EdmProperty;
                }
            }

            //Get the column EdmMember represented by the column name specified
            EdmProperty columnMember = null;
            if (columnName != null)
            {
                tableProperties.TryGetValue(columnName, false, out columnMember);
            }

            //Get the member for which the condition is being specified
            var conditionMember = (columnMember != null) ? columnMember : member;
            if (conditionMember == null)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_ConditionMapping_InvalidMember, ((columnName != null) ? columnName : propertyName),
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }

            bool? isNullValue = null;
            object value = null;
            //Get the attribute value for IsNull attribute
            var isNullAttribute = GetAttributeValue(nav.Clone(), MslConstructs.ConditionIsNullAttribute);

            //Get strongly Typed value if the condition was specified for a specific condition
            var edmType = conditionMember.TypeUsage.EdmType;
            if (Helper.IsPrimitiveType(edmType))
            {
                //Decide if the member is of a type that we would allow a condition on.
                //First convert the type to C space, if this is a condition in s space( before checking this).
                TypeUsage cspaceTypeUsage;
                if (conditionMember.DeclaringType.DataSpace
                    == DataSpace.SSpace)
                {
                    cspaceTypeUsage = StoreItemCollection.ProviderManifest.GetEdmType(conditionMember.TypeUsage);
                    if (cspaceTypeUsage == null)
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_ProviderReturnsNullType(conditionMember.Name),
                            MappingErrorCode.MappingStoreProviderReturnsNullEdmType,
                            m_sourceLocation, navLineInfo, m_parsingErrors);
                        return null;
                    }
                }
                else
                {
                    cspaceTypeUsage = conditionMember.TypeUsage;
                }
                var memberType = ((PrimitiveType)cspaceTypeUsage.EdmType);
                var clrMemberType = memberType.ClrEquivalentType;
                var primitiveTypeKind = memberType.PrimitiveTypeKind;
                //Only a subset of primitive types can be used in Conditions that are specified over values.
                //IsNull conditions can be specified on any primitive types
                if ((isNullAttribute == null)
                    && !IsTypeSupportedForCondition(primitiveTypeKind))
                {
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_InvalidContent_ConditionMapping_InvalidPrimitiveTypeKind,
                        conditionMember.Name, edmType.FullName, MappingErrorCode.ConditionError,
                        m_sourceLocation, navLineInfo, m_parsingErrors);
                    return null;
                }
                Debug.Assert(clrMemberType != null, "Scalar Types should have associated clr type");
                //If the value is not compatible with the type, just add an error and return
                if (
                    !TryGetTypedAttributeValue(
                        nav.Clone(), MslConstructs.ConditionValueAttribute, clrMemberType, m_sourceLocation, m_parsingErrors,
                        out value))
                {
                    return null;
                }
            }
            else if (Helper.IsEnumType(edmType))
            {
                // Enumeration type - get the actual value
                value = GetEnumAttributeValue(
                    nav.Clone(), MslConstructs.ConditionValueAttribute, (EnumType)edmType, m_sourceLocation, m_parsingErrors);
            }
            else
            {
                // Since NullableComplexTypes are not being supported,
                // we don't allow conditions on complex types
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_NonScalar,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }
            //Either Value or NotNull need to be specifid on the condition mapping but not both
            if ((isNullAttribute != null)
                && (value != null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Both_Values,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }
            if ((isNullAttribute == null)
                && (value == null))
            {
                AddToSchemaErrors(
                    Strings.Mapping_InvalidContent_ConditionMapping_Either_Values,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }

            if (isNullAttribute != null)
            {
                //XSD validation should have guarenteed that the attribute value can only be true or false
                Debug.Assert(isNullAttribute == "true" || isNullAttribute == "false");
                isNullValue = Convert.ToBoolean(isNullAttribute, CultureInfo.InvariantCulture);
            }

            if (columnMember != null
                && (columnMember.IsStoreGeneratedComputed || columnMember.IsStoreGeneratedIdentity))
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_InvalidContent_ConditionMapping_Computed, columnMember.Name,
                    MappingErrorCode.ConditionError, m_sourceLocation, navLineInfo, m_parsingErrors);
                return null;
            }

            return
                value != null
                    ? (ConditionPropertyMapping)new ValueConditionMapping(conditionMember, value)
                    : new IsNullConditionMapping(conditionMember, isNullValue.Value);
        }

        internal static bool IsTypeSupportedForCondition(PrimitiveTypeKind primitiveTypeKind)
        {
            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Boolean:
                case PrimitiveTypeKind.Byte:
                case PrimitiveTypeKind.Int16:
                case PrimitiveTypeKind.Int32:
                case PrimitiveTypeKind.Int64:
                case PrimitiveTypeKind.String:
                case PrimitiveTypeKind.SByte:
                    return true;
                case PrimitiveTypeKind.Binary:
                case PrimitiveTypeKind.DateTime:
                case PrimitiveTypeKind.Time:
                case PrimitiveTypeKind.DateTimeOffset:
                case PrimitiveTypeKind.Double:
                case PrimitiveTypeKind.Guid:
                case PrimitiveTypeKind.Single:
                case PrimitiveTypeKind.Decimal:
                    return false;
                default:
                    Debug.Fail("New primitive type kind added?");
                    return false;
            }
        }

        private static XmlSchemaSet GetOrCreateSchemaSet()
        {
            if (s_mappingXmlSchema == null)
            {
                //Get the xsd stream for CS MSL Xsd.
                var set = new XmlSchemaSet();
                AddResourceXsdToSchemaSet(set, MslConstructs.ResourceXsdNameV1);
                AddResourceXsdToSchemaSet(set, MslConstructs.ResourceXsdNameV2);
                AddResourceXsdToSchemaSet(set, MslConstructs.ResourceXsdNameV3);
                Interlocked.CompareExchange(ref s_mappingXmlSchema, set, null);
            }

            return s_mappingXmlSchema;
        }

        private static void AddResourceXsdToSchemaSet(XmlSchemaSet set, string resourceName)
        {
            using (var xsdReader = DbProviderServices.GetXmlResource(resourceName))
            {
                var xmlSchema = XmlSchema.Read(xsdReader, null);
                set.Add(xmlSchema);
            }
        }

        // <summary>
        // Throws a new MappingException giving out the line number and
        // File Name where the error in Mapping specification is present.
        // </summary>
        // <param name="parsingErrors"> Error Collection where the parsing errors are collected </param>
        internal static void AddToSchemaErrors(
            string message, MappingErrorCode errorCode, string location, IXmlLineInfo lineInfo, IList<EdmSchemaError> parsingErrors)
        {
            var error = new EdmSchemaError(
                message, (int)errorCode, EdmSchemaErrorSeverity.Error, location, lineInfo.LineNumber, lineInfo.LinePosition);
            parsingErrors.Add(error);
        }

        internal static EdmSchemaError AddToSchemaErrorsWithMemberInfo(
            Func<object, string> messageFormat, string errorMember, MappingErrorCode errorCode, string location,
            IXmlLineInfo lineInfo, IList<EdmSchemaError> parsingErrors)
        {
            var error = new EdmSchemaError(
                messageFormat(errorMember), (int)errorCode, EdmSchemaErrorSeverity.Error, location, lineInfo.LineNumber,
                lineInfo.LinePosition);
            parsingErrors.Add(error);
            return error;
        }

        internal static void AddToSchemaErrorWithMemberAndStructure(
            Func<object, object, string> messageFormat, string errorMember,
            string errorStructure, MappingErrorCode errorCode, string location, IXmlLineInfo lineInfo,
            IList<EdmSchemaError> parsingErrors)
        {
            var error = new EdmSchemaError(
                messageFormat(errorMember, errorStructure)
                , (int)errorCode, EdmSchemaErrorSeverity.Error, location, lineInfo.LineNumber, lineInfo.LinePosition);
            parsingErrors.Add(error);
        }

        private static void AddToSchemaErrorWithMessage(
            string errorMessage, MappingErrorCode errorCode, string location, IXmlLineInfo lineInfo,
            IList<EdmSchemaError> parsingErrors)
        {
            var error = new EdmSchemaError(
                errorMessage, (int)errorCode, EdmSchemaErrorSeverity.Error, location, lineInfo.LineNumber, lineInfo.LinePosition);
            parsingErrors.Add(error);
        }

        // <summary>
        // Resolve the attribute value based on the aliases provided as part of MSL file.
        // </summary>
        private string GetAliasResolvedAttributeValue(XPathNavigator nav, string attributeName)
        {
            return GetAliasResolvedValue(GetAttributeValue(nav, attributeName));
        }

        private static bool GetBoolAttributeValue(XPathNavigator nav, string attributeName, bool defaultValue)
        {
            var boolValue = defaultValue;
            var boolObj = Helper.GetTypedAttributeValue(nav, attributeName, typeof(bool));

            if (boolObj != null)
            {
                boolValue = (bool)boolObj;
            }
            return boolValue;
        }

        // <summary>
        // The method simply calls the helper method on Helper class with the
        // namespaceURI that is default for CSMapping.
        // </summary>
        private static string GetAttributeValue(XPathNavigator nav, string attributeName)
        {
            return Helper.GetAttributeValue(nav, attributeName);
        }

        // <summary>
        // The method simply calls the helper method on Helper class with the
        // namespaceURI that is default for CSMapping.
        // </summary>
        // <param name="parsingErrors"> Error Collection where the parsing errors are collected </param>
        private static bool TryGetTypedAttributeValue(
            XPathNavigator nav, string attributeName, Type clrType, string sourceLocation, IList<EdmSchemaError> parsingErrors,
            out object value)
        {
            value = null;
            try
            {
                value = Helper.GetTypedAttributeValue(nav, attributeName, clrType);
            }
            catch (FormatException)
            {
                AddToSchemaErrors(
                    Strings.Mapping_ConditionValueTypeMismatch,
                    MappingErrorCode.ConditionError, sourceLocation, (IXmlLineInfo)nav, parsingErrors);
                return false;
            }
            return true;
        }

        // <summary>
        // Returns the enum EdmMember corresponding to attribute name in enumType.
        // </summary>
        // <param name="parsingErrors"> Error Collection where the parsing errors are collected </param>
        private static EnumMember GetEnumAttributeValue(
            XPathNavigator nav, string attributeName, EnumType enumType, string sourceLocation, IList<EdmSchemaError> parsingErrors)
        {
            var xmlLineInfoNav = (IXmlLineInfo)nav;

            var value = GetAttributeValue(nav, attributeName);
            if (String.IsNullOrEmpty(value))
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_Enum_EmptyValue, enumType.FullName,
                    MappingErrorCode.InvalidEnumValue, sourceLocation, xmlLineInfoNav, parsingErrors);
            }

            EnumMember result;
            var found = enumType.Members.TryGetValue(value, false, out result);
            if (!found)
            {
                AddToSchemaErrorsWithMemberInfo(
                    Strings.Mapping_Enum_InvalidValue, value,
                    MappingErrorCode.InvalidEnumValue, sourceLocation, xmlLineInfoNav, parsingErrors);
            }
            return result;
        }

        // <summary>
        // Resolve the string value based on the aliases provided as part of MSL file.
        // </summary>
        private string GetAliasResolvedValue(string aliasedString)
        {
            if ((aliasedString == null)
                || (aliasedString.Length == 0))
            {
                return aliasedString;
            }
            //For now all attributes have no namespace
            var aliasIndex = aliasedString.LastIndexOf('.');
            //If no '.' in the string, than obviously the string is not aliased
            if (aliasIndex == -1)
            {
                return aliasedString;
            }
            var aliasKey = aliasedString.Substring(0, aliasIndex);
            string aliasValue;
            m_alias.TryGetValue(aliasKey, out aliasValue);
            if (aliasValue != null)
            {
                aliasedString = aliasValue + aliasedString.Substring(aliasIndex);
            }
            return aliasedString;
        }

        // <summary>
        // Creates Xml Reader with settings required for
        // XSD validation.
        // </summary>
        private XmlReader GetSchemaValidatingReader(XmlReader innerReader)
        {
            //Create the reader setting that will be used while
            //loading the MSL.
            var readerSettings = GetXmlReaderSettings();
            var reader = XmlReader.Create(innerReader, readerSettings);

            return reader;
        }

        private XmlReaderSettings GetXmlReaderSettings()
        {
            var readerSettings = Schema.CreateEdmStandardXmlReaderSettings();

            readerSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            readerSettings.ValidationEventHandler += XsdValidationCallBack;
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.Schemas = GetOrCreateSchemaSet();
            return readerSettings;
        }

        // <summary>
        // The method is called by the XSD validation event handler when
        // ever there are warnings or errors.
        // We ignore the warnings but the errors will result in exception.
        // </summary>
        private void XsdValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity
                != XmlSeverityType.Warning)
            {
                string sourceLocation = null;
                if (!string.IsNullOrEmpty(args.Exception.SourceUri))
                {
                    sourceLocation = Helper.GetFileNameFromUri(new Uri(args.Exception.SourceUri));
                }
                var severity = EdmSchemaErrorSeverity.Error;
                if (args.Severity
                    == XmlSeverityType.Warning)
                {
                    severity = EdmSchemaErrorSeverity.Warning;
                }
                var error = new EdmSchemaError(
                    Strings.Mapping_InvalidMappingSchema_validation(args.Exception.Message)
                    , (int)MappingErrorCode.XmlSchemaValidationError, severity, sourceLocation, args.Exception.LineNumber,
                    args.Exception.LinePosition);
                m_parsingErrors.Add(error);
            }
        }

        // <summary>
        // Validate the scalar property mapping - makes sure that the cspace type is promotable to the store side and updates
        // the store type usage
        // </summary>
        private void ValidateAndUpdateScalarMemberMapping(EdmProperty member, EdmProperty columnMember, IXmlLineInfo lineInfo)
        {
            Debug.Assert(
                Helper.IsScalarType(member.TypeUsage.EdmType),
                "c-space member type must be of primitive or enumeration type");
            Debug.Assert(Helper.IsPrimitiveType(columnMember.TypeUsage.EdmType), "s-space column type must be primitive");

            KeyValuePair<TypeUsage, TypeUsage> memberMappingInfo;
            if (!m_scalarMemberMappings.TryGetValue(member, out memberMappingInfo))
            {
                var errorCount = m_parsingErrors.Count;

                // Validates that the CSpace member type is promotable to the SSpace member types and returns a typeUsage which contains
                // the store equivalent type for the CSpace member type.
                // For e.g. If a CSpace member of type Edm.Int32 maps to SqlServer.Int64, the return type usage will contain SqlServer.int
                //          which is store equivalent type for Edm.Int32
                var storeEquivalentTypeUsage = Helper.ValidateAndConvertTypeUsage(
                    member,
                    columnMember);

                // If the cspace type is not compatible with the store type, add a schema error and return
                if (storeEquivalentTypeUsage == null)
                {
                    if (errorCount == m_parsingErrors.Count)
                    {
                        var error = new EdmSchemaError(
                            GetInvalidMemberMappingErrorMessage(member, columnMember),
                            (int)MappingErrorCode.IncompatibleMemberMapping, EdmSchemaErrorSeverity.Error,
                            m_sourceLocation, lineInfo.LineNumber,
                            lineInfo.LinePosition);
                        m_parsingErrors.Add(error);
                    }
                }
                else
                {
                    m_scalarMemberMappings.Add(
                        member, new KeyValuePair<TypeUsage, TypeUsage>(storeEquivalentTypeUsage, columnMember.TypeUsage));
                }
            }
            else
            {
                // Get the store member type to which the cspace member was mapped to previously
                var storeMappedTypeUsage = memberMappingInfo.Value;
                var modelColumnMember = columnMember.TypeUsage.ModelTypeUsage;
                if (!ReferenceEquals(columnMember.TypeUsage.EdmType, storeMappedTypeUsage.EdmType))
                {
                    var error = new EdmSchemaError(
                        Strings.Mapping_StoreTypeMismatch_ScalarPropertyMapping(
                            member.Name,
                            storeMappedTypeUsage.EdmType.Name),
                        (int)MappingErrorCode.CSpaceMemberMappedToMultipleSSpaceMemberWithDifferentTypes,
                        EdmSchemaErrorSeverity.Error,
                        m_sourceLocation,
                        lineInfo.LineNumber,
                        lineInfo.LinePosition);
                    m_parsingErrors.Add(error);
                }
                // Check if the cspace facets are promotable to the new store type facets
                else if (!TypeSemantics.IsSubTypeOf(ResolveTypeUsageForEnums(member.TypeUsage), modelColumnMember))
                {
                    var error = new EdmSchemaError(
                        GetInvalidMemberMappingErrorMessage(member, columnMember),
                        (int)MappingErrorCode.IncompatibleMemberMapping, EdmSchemaErrorSeverity.Error,
                        m_sourceLocation, lineInfo.LineNumber,
                        lineInfo.LinePosition);
                    m_parsingErrors.Add(error);
                }
            }
        }

        internal static string GetInvalidMemberMappingErrorMessage(EdmMember cSpaceMember, EdmMember sSpaceMember)
        {
            return Strings.Mapping_Invalid_Member_Mapping(
                cSpaceMember.TypeUsage.EdmType + GetFacetsForDisplay(cSpaceMember.TypeUsage),
                cSpaceMember.Name,
                cSpaceMember.DeclaringType.FullName,
                sSpaceMember.TypeUsage.EdmType + GetFacetsForDisplay(sSpaceMember.TypeUsage),
                sSpaceMember.Name,
                sSpaceMember.DeclaringType.FullName);
        }

        private static string GetFacetsForDisplay(TypeUsage typeUsage)
        {
            DebugCheck.NotNull(typeUsage);

            var facets = typeUsage.Facets;
            if (facets == null
                || facets.Count == 0)
            {
                return string.Empty;
            }

            var numFacets = facets.Count;

            var facetDisplay = new StringBuilder("[");

            for (var i = 0; i < numFacets - 1; ++i)
            {
                facetDisplay.AppendFormat("{0}={1},", facets[i].Name, facets[i].Value ?? string.Empty);
            }

            facetDisplay.AppendFormat("{0}={1}]", facets[numFacets - 1].Name, facets[numFacets - 1].Value ?? string.Empty);

            return facetDisplay.ToString();
        }

        // <summary>
        // Encapsulates state and functionality for loading a modification function mapping.
        // </summary>
        private class ModificationFunctionMappingLoader
        {
            // Storage mapping loader
            private readonly MappingItemLoader m_parentLoader;

            // Mapped function
            private EdmFunction m_function;

            // Entity set mapped by this function (may be null)
            private readonly EntitySet m_entitySet;

            // Association set mapped by this function (may be null)
            private readonly AssociationSet m_associationSet;

            // Model entity container (used to resolve set names)
            private readonly EntityContainer m_modelContainer;

            // Item collection (used to resolve function and type names)
            private readonly EdmItemCollection m_edmItemCollection;

            // Item collection (used to resolve function and type names)
            private readonly StoreItemCollection m_storeItemCollection;

            // Indicates whether the function can be bound to "current"
            // versions of properties (i.e., inserts and updates)
            private bool m_allowCurrentVersion;

            // Indicates whether the function can be bound to "original"
            // versions of properties (i.e., deletes and updates)
            private bool m_allowOriginalVersion;

            // Tracks which function parameters have been seen so far.
            private readonly Set<FunctionParameter> m_seenParameters;

            // Tracks members navigated to arrive at the current element
            private readonly Stack<EdmMember> m_members;

            // When set, indicates we are interpreting a navigation property on the given set.
            private AssociationSet m_associationSetNavigation;

            // Initialize loader
            internal ModificationFunctionMappingLoader(
                MappingItemLoader parentLoader,
                EntitySetBase extent)
            {
                DebugCheck.NotNull(parentLoader);
                DebugCheck.NotNull(extent);

                m_parentLoader = parentLoader;
                // initialize member fields
                m_modelContainer = extent.EntityContainer;
                m_edmItemCollection = parentLoader.EdmItemCollection;
                m_storeItemCollection = parentLoader.StoreItemCollection;
                m_entitySet = extent as EntitySet;
                if (null == m_entitySet)
                {
                    // do a cast here since the extent must either be an entity set
                    // or an association set
                    m_associationSet = (AssociationSet)extent;
                }
                m_seenParameters = new Set<FunctionParameter>();
                m_members = new Stack<EdmMember>();
            }

            internal ModificationFunctionMapping LoadEntityTypeModificationFunctionMapping(
                XPathNavigator nav, EntitySetBase entitySet, bool allowCurrentVersion, bool allowOriginalVersion, EntityType entityType)
            {
                FunctionParameter rowsAffectedParameter;
                m_function = LoadAndValidateFunctionMetadata(nav.Clone(), out rowsAffectedParameter);
                if (m_function == null)
                {
                    return null;
                }
                m_allowCurrentVersion = allowCurrentVersion;
                m_allowOriginalVersion = allowOriginalVersion;

                // Load all parameter bindings and result bindings
                var parameters = LoadParameterBindings(nav.Clone(), entityType);
                var resultBindings = LoadResultBindings(nav.Clone(), entityType);

                var functionMapping = new ModificationFunctionMapping(
                    entitySet, entityType, m_function, parameters, rowsAffectedParameter, resultBindings);

                return functionMapping;
            }

            // Loads a function mapping for an association set
            internal ModificationFunctionMapping LoadAssociationSetModificationFunctionMapping(
                XPathNavigator nav, EntitySetBase entitySet, bool isInsert)
            {
                FunctionParameter rowsAffectedParameter;
                m_function = LoadAndValidateFunctionMetadata(nav.Clone(), out rowsAffectedParameter);
                if (m_function == null)
                {
                    return null;
                }
                if (isInsert)
                {
                    m_allowCurrentVersion = true;
                    m_allowOriginalVersion = false;
                }
                else
                {
                    m_allowCurrentVersion = false;
                    m_allowOriginalVersion = true;
                }

                // Load all parameter bindings
                var parameters = LoadParameterBindings(nav.Clone(), m_associationSet.ElementType);

                var mapping = new ModificationFunctionMapping(
                    entitySet, entitySet.ElementType, m_function, parameters, rowsAffectedParameter, null);
                return mapping;
            }

            // Loads all result bindings.
            private IEnumerable<ModificationFunctionResultBinding> LoadResultBindings(XPathNavigator nav, EntityType entityType)
            {
                var resultBindings = new List<ModificationFunctionResultBinding>();
                var xmlLineInfoNav = (IXmlLineInfo)nav;

                // walk through all children, filtering on result bindings
                if (nav.MoveToChild(XPathNodeType.Element))
                {
                    do
                    {
                        if (nav.LocalName
                            == MslConstructs.ResultBindingElement)
                        {
                            // retrieve attributes
                            var propertyName = m_parentLoader.GetAliasResolvedAttributeValue(
                                nav.Clone(),
                                MslConstructs.ResultBindingPropertyNameAttribute);
                            var columnName = m_parentLoader.GetAliasResolvedAttributeValue(
                                nav.Clone(),
                                MslConstructs.ScalarPropertyColumnNameAttribute);

                            // resolve metadata
                            EdmProperty property = null;
                            if (null == propertyName
                                ||
                                !entityType.Properties.TryGetValue(propertyName, false, out property))
                            {
                                // add a schema error and return if the property does not exist
                                AddToSchemaErrorWithMemberAndStructure(
                                    Strings.Mapping_ModificationFunction_PropertyNotFound,
                                    propertyName, entityType.Name,
                                    MappingErrorCode.InvalidEdmMember, m_parentLoader.m_sourceLocation,
                                    xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                                return new List<ModificationFunctionResultBinding>();
                            }

                            // construct element binding (no type checking is required at mapping load time)
                            var resultBinding = new ModificationFunctionResultBinding(columnName, property);
                            resultBindings.Add(resultBinding);
                        }
                    }
                    while (nav.MoveToNext(XPathNodeType.Element));
                }

                // check for duplicate mappings of single properties
                var propertyToColumnNamesMap = new KeyToListMap<EdmProperty, string>(EqualityComparer<EdmProperty>.Default);
                foreach (var resultBinding in resultBindings)
                {
                    propertyToColumnNamesMap.Add(resultBinding.Property, resultBinding.ColumnName);
                }
                foreach (var property in propertyToColumnNamesMap.Keys)
                {
                    var columnNames = propertyToColumnNamesMap.ListForKey(property);
                    if (1 < columnNames.Count)
                    {
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_ModificationFunction_AmbiguousResultBinding,
                            property.Name, StringUtil.ToCommaSeparatedString(columnNames),
                            MappingErrorCode.AmbiguousResultBindingInModificationFunctionMapping,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav,
                            m_parentLoader.m_parsingErrors);
                        return new List<ModificationFunctionResultBinding>();
                    }
                }

                return resultBindings;
            }

            // Loads parameter bindings from the given node, validating bindings:
            // - All parameters are covered
            // - Referenced names exist in type
            // - Parameter and scalar type are compatible
            // - Legal versions are given
            private IEnumerable<ModificationFunctionParameterBinding> LoadParameterBindings(XPathNavigator nav, StructuralType type)
            {
                // recursively retrieve bindings (current member path is empty)
                // immediately construct a list of bindings to force execution of the LoadParameterBindings
                // yield method
                var parameterBindings = new List<ModificationFunctionParameterBinding>(
                    LoadParameterBindings(nav.Clone(), type, restrictToKeyMembers: false));

                // check that all parameters have been mapped
                var unmappedParameters = new Set<FunctionParameter>(m_function.Parameters);
                unmappedParameters.Subtract(m_seenParameters);
                if (0 != unmappedParameters.Count)
                {
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_ModificationFunction_MissingParameter,
                        m_function.FullName, StringUtil.ToCommaSeparatedString(unmappedParameters),
                        MappingErrorCode.InvalidParameterInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, (IXmlLineInfo)nav,
                        m_parentLoader.m_parsingErrors);
                    return new List<ModificationFunctionParameterBinding>();
                }

                return parameterBindings;
            }

            private IEnumerable<ModificationFunctionParameterBinding> LoadParameterBindings(
                XPathNavigator nav, StructuralType type,
                bool restrictToKeyMembers)
            {
                // walk through all child bindings
                if (nav.MoveToChild(XPathNodeType.Element))
                {
                    do
                    {
                        switch (nav.LocalName)
                        {
                            case MslConstructs.ScalarPropertyElement:
                                {
                                    var binding = LoadScalarPropertyParameterBinding(
                                        nav.Clone(), type, restrictToKeyMembers);
                                    if (binding != null)
                                    {
                                        yield return binding;
                                    }
                                    else
                                    {
                                        yield break;
                                    }
                                }
                                break;
                            case MslConstructs.ComplexPropertyElement:
                                {
                                    ComplexType complexType;
                                    var property = LoadComplexTypeProperty(
                                        nav.Clone(), type, out complexType);
                                    if (property != null)
                                    {
                                        // recursively retrieve mappings
                                        m_members.Push(property);
                                        foreach (var binding in
                                            LoadParameterBindings(nav.Clone(), complexType, restrictToKeyMembers))
                                        {
                                            yield return binding;
                                        }
                                        m_members.Pop();
                                    }
                                }
                                break;
                            case MslConstructs.AssociationEndElement:
                                {
                                    var toEnd = LoadAssociationEnd(nav.Clone());
                                    if (toEnd != null)
                                    {
                                        // translate the bindings for the association end
                                        m_members.Push(toEnd.CorrespondingAssociationEndMember);
                                        m_associationSetNavigation = toEnd.ParentAssociationSet;
                                        foreach (var binding in
                                            LoadParameterBindings(nav.Clone(), toEnd.EntitySet.ElementType, true /* restrictToKeyMembers */)
                                            )
                                        {
                                            yield return binding;
                                        }
                                        m_associationSetNavigation = null;
                                        m_members.Pop();
                                    }
                                }
                                break;
                            case MslConstructs.EndPropertyMappingElement:
                                {
                                    var end = LoadEndProperty(nav.Clone());
                                    if (end != null)
                                    {
                                        // translate the bindings for the end property
                                        m_members.Push(end.CorrespondingAssociationEndMember);
                                        foreach (var binding in
                                            LoadParameterBindings(nav.Clone(), end.EntitySet.ElementType, true /* restrictToKeyMembers */))
                                        {
                                            yield return binding;
                                        }
                                        m_members.Pop();
                                    }
                                }
                                break;
                        }
                    }
                    while (nav.MoveToNext(XPathNodeType.Element));
                }
            }

            private AssociationSetEnd LoadAssociationEnd(XPathNavigator nav)
            {
                var xmlLineInfoNav = (IXmlLineInfo)nav;

                // retrieve element attributes
                var associationSetName = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.AssociationSetAttribute);
                var fromRole = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.FromAttribute);
                var toRole = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.ToAttribute);

                // retrieve metadata
                RelationshipSet relationshipSet = null;
                AssociationSet associationSet;

                // validate the association set exists
                if (null == associationSetName
                    ||
                    !m_modelContainer.TryGetRelationshipSetByName(associationSetName, false, out relationshipSet)
                    ||
                    BuiltInTypeKind.AssociationSet != relationshipSet.BuiltInTypeKind)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetDoesNotExist,
                        associationSetName, MappingErrorCode.InvalidAssociationSet,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav,
                        m_parentLoader.m_parsingErrors);
                    return null;
                }
                associationSet = (AssociationSet)relationshipSet;

                // validate the from end exists
                AssociationSetEnd fromEnd = null;
                if (null == fromRole
                    ||
                    !associationSet.AssociationSetEnds.TryGetValue(fromRole, false, out fromEnd))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetRoleDoesNotExist,
                        fromRole, MappingErrorCode.InvalidAssociationSetRoleInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                // validate the to end exists
                AssociationSetEnd toEnd = null;
                if (null == toRole
                    ||
                    !associationSet.AssociationSetEnds.TryGetValue(toRole, false, out toEnd))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetRoleDoesNotExist,
                        toRole, MappingErrorCode.InvalidAssociationSetRoleInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                // validate ends reference the current entity set
                if (!fromEnd.EntitySet.Equals(m_entitySet))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetFromRoleIsNotEntitySet,
                        fromRole, MappingErrorCode.InvalidAssociationSetRoleInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                // validate cardinality of to end (can be at most one)
                if (toEnd.CorrespondingAssociationEndMember.RelationshipMultiplicity != RelationshipMultiplicity.One
                    &&
                    toEnd.CorrespondingAssociationEndMember.RelationshipMultiplicity != RelationshipMultiplicity.ZeroOrOne)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetCardinality,
                        toRole, MappingErrorCode.InvalidAssociationSetCardinalityInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                // if this is a FK, raise an error or a warning if the mapping would have been allowed in V1
                // (all dependent properties are part of the primary key)
                if (associationSet.ElementType.IsForeignKey)
                {
                    var constraint = associationSet.ElementType.ReferentialConstraints.Single();
                    var error = AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationEndMappingForeignKeyAssociation,
                        toRole, MappingErrorCode.InvalidModificationFunctionMappingAssociationEndForeignKey,
                        m_parentLoader.m_sourceLocation,
                        xmlLineInfoNav, m_parentLoader.m_parsingErrors);

                    if (fromEnd.CorrespondingAssociationEndMember == constraint.ToRole
                        &&
                        constraint.ToProperties.All(p => m_entitySet.ElementType.KeyMembers.Contains(p)))
                    {
                        // Just a warning...
                        error.Severity = EdmSchemaErrorSeverity.Warning;
                    }
                    else
                    {
                        return null;
                    }
                }
                return toEnd;
            }

            private AssociationSetEnd LoadEndProperty(XPathNavigator nav)
            {
                // retrieve element attributes
                var role = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.EndPropertyMappingNameAttribute);

                // validate the role exists
                AssociationSetEnd end = null;
                if (null == role
                    ||
                    !m_associationSet.AssociationSetEnds.TryGetValue(role, false, out end))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AssociationSetRoleDoesNotExist,
                        role, MappingErrorCode.InvalidAssociationSetRoleInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, (IXmlLineInfo)nav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                return end;
            }

            private EdmMember LoadComplexTypeProperty(XPathNavigator nav, StructuralType type, out ComplexType complexType)
            {
                var xmlLineInfoNav = (IXmlLineInfo)nav;

                // retrieve element attributes
                var propertyName = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.ComplexPropertyNameAttribute);
                var typeName = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.ComplexTypeMappingTypeNameAttribute);

                // retrieve metadata
                EdmMember property = null;
                if (null == propertyName
                    ||
                    !type.Members.TryGetValue(propertyName, false, out property))
                {
                    // raise exception if the property does not exist
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_ModificationFunction_PropertyNotFound,
                        propertyName, type.Name, MappingErrorCode.InvalidEdmMember,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    complexType = null;
                    return null;
                }
                complexType = null;
                if (null == typeName
                    ||
                    !m_edmItemCollection.TryGetItem(typeName, out complexType))
                {
                    // raise exception if the type does not exist
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_ComplexTypeNotFound,
                        typeName, MappingErrorCode.InvalidComplexType,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav
                        , m_parentLoader.m_parsingErrors);
                    return null;
                }
                if (!property.TypeUsage.EdmType.Equals(complexType)
                    &&
                    !Helper.IsSubtypeOf(property.TypeUsage.EdmType, complexType))
                {
                    // raise exception if the complex type is incorrect
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_ModificationFunction_WrongComplexType,
                        typeName, property.Name, MappingErrorCode.InvalidComplexType,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav
                        , m_parentLoader.m_parsingErrors);
                    return null;
                }
                return property;
            }

            [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
            private ModificationFunctionParameterBinding LoadScalarPropertyParameterBinding(
                XPathNavigator nav, StructuralType type, bool restrictToKeyMembers)
            {
                var xmlLineInfoNav = (IXmlLineInfo)nav;

                // get attribute values
                var parameterName = m_parentLoader.GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ParameterNameAttribute);
                var propertyName = m_parentLoader.GetAliasResolvedAttributeValue(
                    nav.Clone(), MslConstructs.ScalarPropertyNameAttribute);
                var version = m_parentLoader.GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.ParameterVersionAttribute);

                // determine version
                var isCurrent = false;
                if (null == version)
                {
                    // use default
                    if (!m_allowOriginalVersion)
                    {
                        isCurrent = true;
                    }
                    else if (!m_allowCurrentVersion)
                    {
                        isCurrent = false;
                    }
                    else
                    {
                        // add a schema error and return as there is no default
                        AddToSchemaErrors(
                            Strings.Mapping_ModificationFunction_MissingVersion,
                            MappingErrorCode.MissingVersionInModificationFunctionMapping, m_parentLoader.m_sourceLocation,
                            xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                }
                else
                {
                    // check the value given by the user
                    isCurrent = version == MslConstructs.ParameterVersionAttributeCurrentValue;
                }
                if (isCurrent && !m_allowCurrentVersion)
                {
                    //Add a schema error and return  since the 'current' property version is not available
                    AddToSchemaErrors(
                        Strings.Mapping_ModificationFunction_VersionMustBeOriginal,
                        MappingErrorCode.InvalidVersionInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav
                        , m_parentLoader.m_parsingErrors);
                    return null;
                }
                if (!isCurrent
                    && !m_allowOriginalVersion)
                {
                    // Add a schema error and return  since the 'original' property version is not available
                    AddToSchemaErrors(
                        Strings.Mapping_ModificationFunction_VersionMustBeCurrent,
                        MappingErrorCode.InvalidVersionInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav
                        , m_parentLoader.m_parsingErrors);
                    return null;
                }

                // retrieve metadata
                FunctionParameter parameter = null;
                if (null == parameterName
                    ||
                    !m_function.Parameters.TryGetValue(parameterName, false, out parameter))
                {
                    //Add a schema error and return  if the parameter does not exist
                    AddToSchemaErrorWithMemberAndStructure(
                        Strings.Mapping_ModificationFunction_ParameterNotFound,
                        parameterName, m_function.Name,
                        MappingErrorCode.InvalidParameterInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav
                        , m_parentLoader.m_parsingErrors);
                    return null;
                }
                EdmMember property = null;
                if (restrictToKeyMembers)
                {
                    if (null == propertyName
                        ||
                        !((EntityType)type).KeyMembers.TryGetValue(propertyName, false, out property))
                    {
                        // raise exception if the property does not exist
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_ModificationFunction_PropertyNotKey,
                            propertyName, type.Name,
                            MappingErrorCode.InvalidEdmMember,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                }
                else
                {
                    if (null == propertyName
                        ||
                        !type.Members.TryGetValue(propertyName, false, out property))
                    {
                        // raise exception if the property does not exist
                        AddToSchemaErrorWithMemberAndStructure(
                            Strings.Mapping_ModificationFunction_PropertyNotFound,
                            propertyName, type.Name,
                            MappingErrorCode.InvalidEdmMember,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                }

                // check that the parameter hasn't already been seen
                if (m_seenParameters.Contains(parameter))
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_ParameterBoundTwice,
                        parameterName, MappingErrorCode.ParameterBoundTwiceInModificationFunctionMapping,
                        m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                var errorCount = m_parentLoader.m_parsingErrors.Count;

                var mappedStoreType = Helper.ValidateAndConvertTypeUsage(
                    property.TypeUsage,
                    parameter.TypeUsage);

                // validate type compatibility
                if (mappedStoreType == null
                    && errorCount == m_parentLoader.m_parsingErrors.Count)
                {
                    AddToSchemaErrorWithMessage(
                        Strings.Mapping_ModificationFunction_PropertyParameterTypeMismatch(
                            property.TypeUsage.EdmType,
                            property.Name,
                            property.DeclaringType.FullName,
                            parameter.TypeUsage.EdmType,
                            parameter.Name,
                            m_function.FullName),
                        MappingErrorCode.InvalidModificationFunctionMappingPropertyParameterTypeMismatch,
                        m_parentLoader.m_sourceLocation,
                        xmlLineInfoNav,
                        m_parentLoader.m_parsingErrors);
                }

                // create the binding object
                m_members.Push(property);

                // if the member path includes a FK relationship, remap to the corresponding FK property
                IEnumerable<EdmMember> members = m_members;
                var associationSetNavigation = m_associationSetNavigation;
                if (m_members.Last().BuiltInTypeKind
                    == BuiltInTypeKind.AssociationEndMember)
                {
                    var targetEnd = (AssociationEndMember)m_members.Last();
                    var associationType = (AssociationType)targetEnd.DeclaringType;
                    if (associationType.IsForeignKey)
                    {
                        var constraint = associationType.ReferentialConstraints.Single();
                        if (constraint.FromRole == targetEnd)
                        {
                            var ordinal = constraint.FromProperties.IndexOf((EdmProperty)m_members.First());

                            // rebind to the foreign key (no longer an association set navigation)
                            members = new EdmMember[] { constraint.ToProperties[ordinal], };
                            associationSetNavigation = null;
                        }
                    }
                }
                var binding = new ModificationFunctionParameterBinding(
                    parameter, new ModificationFunctionMemberPath(
                        members, associationSetNavigation), isCurrent);
                m_members.Pop();

                // remember that we've seen a binding for this parameter
                m_seenParameters.Add(parameter);

                return binding;
            }

            // <summary>
            // Loads function metadata and ensures the function is supportable for function mapping.
            // </summary>
            private EdmFunction LoadAndValidateFunctionMetadata(XPathNavigator nav, out FunctionParameter rowsAffectedParameter)
            {
                var xmlLineInfoNav = (IXmlLineInfo)nav;

                // Different operations may be mapped to the same function (e.g. both INSERT and UPDATE are handled by a single
                // UPSERT function). Between loading functions, we can clear the set of seen parameters, because we may see them
                // again and don't want to claim there's a collision in such cases.
                m_seenParameters.Clear();

                // retrieve function attributes from the current element
                var functionName = m_parentLoader.GetAliasResolvedAttributeValue(nav.Clone(), MslConstructs.FunctionNameAttribute);
                rowsAffectedParameter = null;

                // find function metadata
                var functionOverloads =
                    m_storeItemCollection.GetFunctions(functionName);

                if (functionOverloads.Count == 0)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_UnknownFunction, functionName,
                        MappingErrorCode.InvalidModificationFunctionMappingUnknownFunction, m_parentLoader.m_sourceLocation,
                        xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                if (1 < functionOverloads.Count)
                {
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_AmbiguousFunction, functionName,
                        MappingErrorCode.InvalidModificationFunctionMappingAmbiguousFunction, m_parentLoader.m_sourceLocation,
                        xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                var function = functionOverloads[0];

                // check function is legal for function mapping
                if (MetadataHelper.IsComposable(function))
                {
                    // only non-composable functions are permitted
                    AddToSchemaErrorsWithMemberInfo(
                        Strings.Mapping_ModificationFunction_NotValidFunction, functionName,
                        MappingErrorCode.InvalidModificationFunctionMappingNotValidFunction, m_parentLoader.m_sourceLocation,
                        xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                    return null;
                }

                // check for parameter
                var rowsAffectedParameterName = GetAttributeValue(nav, MslConstructs.RowsAffectedParameterAttribute);
                if (!string.IsNullOrEmpty(rowsAffectedParameterName))
                {
                    // check that the parameter exists
                    if (!function.Parameters.TryGetValue(rowsAffectedParameterName, false, out rowsAffectedParameter))
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_FunctionImport_RowsAffectedParameterDoesNotExist(
                                rowsAffectedParameterName, function.FullName),
                            MappingErrorCode.MappingFunctionImportRowsAffectedParameterDoesNotExist,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                    // check that the parameter is an out parameter
                    if (ParameterMode.Out != rowsAffectedParameter.Mode
                        && ParameterMode.InOut != rowsAffectedParameter.Mode)
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_FunctionImport_RowsAffectedParameterHasWrongMode(
                                rowsAffectedParameterName, rowsAffectedParameter.Mode, ParameterMode.Out, ParameterMode.InOut),
                            MappingErrorCode.MappingFunctionImportRowsAffectedParameterHasWrongMode,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                    // check that the parameter type is an integer type
                    var rowsAffectedParameterType = (PrimitiveType)rowsAffectedParameter.TypeUsage.EdmType;

                    if (!TypeSemantics.IsIntegerNumericType(rowsAffectedParameter.TypeUsage))
                    {
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_FunctionImport_RowsAffectedParameterHasWrongType(
                                rowsAffectedParameterName, rowsAffectedParameterType.PrimitiveTypeKind),
                            MappingErrorCode.MappingFunctionImportRowsAffectedParameterHasWrongType,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                    m_seenParameters.Add(rowsAffectedParameter);
                }

                // check that all parameters are allowed
                foreach (var parameter in function.Parameters)
                {
                    if (ParameterMode.In != parameter.Mode
                        && rowsAffectedParameterName != parameter.Name)
                    {
                        // rows affected is 'out' not 'in'
                        AddToSchemaErrorWithMessage(
                            Strings.Mapping_ModificationFunction_NotValidFunctionParameter(
                                functionName,
                                parameter.Name, MslConstructs.RowsAffectedParameterAttribute),
                            MappingErrorCode.InvalidModificationFunctionMappingNotValidFunctionParameter,
                            m_parentLoader.m_sourceLocation, xmlLineInfoNav, m_parentLoader.m_parsingErrors);
                        return null;
                    }
                }

                return function;
            }
        }

        // <summary>
        // Checks whether the <paramref name="typeUsage" /> represents a type usage for an enumeration type and if
        // this is the case creates a new type usage built using the underlying type of the enumeration type.
        // </summary>
        // <param name="typeUsage"> TypeUsage to resolve. </param>
        // <returns>
        // If <paramref name="typeUsage" /> represents a TypeUsage for enumeration type the method returns a new TypeUsage instance created using the underlying type of the enumeration type. Otherwise the method returns
        // <paramref
        //     name="typeUsage" />
        // .
        // </returns>
        internal static TypeUsage ResolveTypeUsageForEnums(TypeUsage typeUsage)
        {
            DebugCheck.NotNull(typeUsage);

            return Helper.IsEnumType(typeUsage.EdmType)
                       ? TypeUsage.Create(Helper.GetUnderlyingEdmTypeForEnumType(typeUsage.EdmType), typeUsage.Facets)
                       : typeUsage;
        }
    }
}
