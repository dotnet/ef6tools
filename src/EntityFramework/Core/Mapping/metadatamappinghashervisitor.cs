// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    internal class MetadataMappingHasherVisitor : BaseMetadataMappingVisitor
    {
        private CompressingHashBuilder m_hashSourceBuilder;
        private Dictionary<Object, int> m_itemsAlreadySeen = new Dictionary<Object, int>();
        private int m_instanceNumber;
        private EdmItemCollection m_EdmItemCollection;
        private double m_MappingVersion;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private MetadataMappingHasherVisitor(double mappingVersion, bool sortSequence)
            : base(sortSequence)
        {
            m_MappingVersion = mappingVersion;
            m_hashSourceBuilder = new CompressingHashBuilder(MetadataHelper.CreateMetadataHashAlgorithm(m_MappingVersion));
        }

        protected override void Visit(EntityContainerMapping entityContainerMapping)
        {
            DebugCheck.NotNull(entityContainerMapping);

            // at the entry point of visitor, we setup the versions
            Debug.Assert(
                m_MappingVersion == entityContainerMapping.StorageMappingItemCollection.MappingVersion,
                "the original version and the mapping collection version are not the same");
            m_MappingVersion = entityContainerMapping.StorageMappingItemCollection.MappingVersion;

            m_EdmItemCollection = entityContainerMapping.StorageMappingItemCollection.EdmItemCollection;

            int index;
            if (!AddObjectToSeenListAndHashBuilder(entityContainerMapping, out index))
            {
                // if this has been add to the seen list, then just 
                return;
            }
            if (m_itemsAlreadySeen.Count > 1)
            {
                // this means user try another visit over SECM, this is allowed but all the previous visit all lost due to clean
                // user can visit different SECM objects by using the same visitor to load the SECM object
                Clean();
                Visit(entityContainerMapping);
                return;
            }

            AddObjectStartDumpToHashBuilder(entityContainerMapping, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(entityContainerMapping.Identity);

            AddV2ObjectContentToHashBuilder(entityContainerMapping.GenerateUpdateViews, m_MappingVersion);

            base.Visit(entityContainerMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EntityContainer entityContainer)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(entityContainer, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(entityContainer, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(entityContainer.Identity);
            // Name is covered by Identity

            base.Visit(entityContainer);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EntitySetBaseMapping setMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(setMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(setMapping, index);

            #region Inner data visit

            base.Visit(setMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(TypeMapping typeMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(typeMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(typeMapping, index);

            #region Inner data visit

            base.Visit(typeMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(MappingFragment mappingFragment)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(mappingFragment, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(mappingFragment, index);

            #region Inner data visit

            AddV2ObjectContentToHashBuilder(mappingFragment.IsSQueryDistinct, m_MappingVersion);

            base.Visit(mappingFragment);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(PropertyMapping propertyMapping)
        {
            base.Visit(propertyMapping);
        }

        protected override void Visit(ComplexPropertyMapping complexPropertyMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(complexPropertyMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(complexPropertyMapping, index);

            #region Inner data visit

            base.Visit(complexPropertyMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(ComplexTypeMapping complexTypeMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(complexTypeMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(complexTypeMapping, index);

            #region Inner data visit

            base.Visit(complexTypeMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(ConditionPropertyMapping conditionPropertyMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(conditionPropertyMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(conditionPropertyMapping, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(conditionPropertyMapping.IsNull);
            AddObjectContentToHashBuilder(conditionPropertyMapping.Value);

            base.Visit(conditionPropertyMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(ScalarPropertyMapping scalarPropertyMapping)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(scalarPropertyMapping, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(scalarPropertyMapping, index);

            #region Inner data visit

            base.Visit(scalarPropertyMapping);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EntitySetBase entitySetBase)
        {
            base.Visit(entitySetBase);
        }

        protected override void Visit(EntitySet entitySet)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(entitySet, out index))
            {
                return;
            }

            #region Inner data visit

            AddObjectStartDumpToHashBuilder(entitySet, index);
            AddObjectContentToHashBuilder(entitySet.Name);
            AddObjectContentToHashBuilder(entitySet.Schema);
            AddObjectContentToHashBuilder(entitySet.Table);

            base.Visit(entitySet);

            var sequence = MetadataHelper.GetTypeAndSubtypesOf(entitySet.ElementType, m_EdmItemCollection, false)
                              .Where(type => type != entitySet.ElementType);
            foreach (var entityType in GetSequence(sequence, it => it.Identity))
            {
                Visit(entityType);
            }

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(AssociationSet associationSet)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(associationSet, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(associationSet, index);

            #region Inner data visit

            // Name is coverd by Identity
            AddObjectContentToHashBuilder(associationSet.Identity);
            AddObjectContentToHashBuilder(associationSet.Schema);
            AddObjectContentToHashBuilder(associationSet.Table);

            base.Visit(associationSet);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EntityType entityType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(entityType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(entityType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(entityType.Abstract);
            AddObjectContentToHashBuilder(entityType.Identity);
            // FullName, Namespace and Name are all covered by Identity

            base.Visit(entityType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(AssociationSetEnd associationSetEnd)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(associationSetEnd, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(associationSetEnd, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(associationSetEnd.Identity);
            // Name is covered by Identity

            base.Visit(associationSetEnd);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(AssociationType associationType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(associationType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(associationType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(associationType.Abstract);
            AddObjectContentToHashBuilder(associationType.Identity);
            // FullName, Namespace, and Name are all covered by Identity

            base.Visit(associationType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EdmProperty edmProperty)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(edmProperty, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(edmProperty, index);

            #region Inner data visit

            // since the delaring type is fixed and referenced to the upper type, 
            // there is no need to hash this
            //this.AddObjectContentToHashBuilder(edmProperty.DeclaringType);
            AddObjectContentToHashBuilder(edmProperty.DefaultValue);
            AddObjectContentToHashBuilder(edmProperty.Identity);
            // Name is covered by Identity
            AddObjectContentToHashBuilder(edmProperty.IsStoreGeneratedComputed);
            AddObjectContentToHashBuilder(edmProperty.IsStoreGeneratedIdentity);
            AddObjectContentToHashBuilder(edmProperty.Nullable);

            base.Visit(edmProperty);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(NavigationProperty navigationProperty)
        {
            // navigation properties are not considered in view generation
            return;
        }

        protected override void Visit(EdmMember edmMember)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(edmMember, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(edmMember, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(edmMember.Identity);
            // Name is covered by Identity
            AddObjectContentToHashBuilder(edmMember.IsStoreGeneratedComputed);
            AddObjectContentToHashBuilder(edmMember.IsStoreGeneratedIdentity);

            base.Visit(edmMember);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(AssociationEndMember associationEndMember)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(associationEndMember, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(associationEndMember, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(associationEndMember.DeleteBehavior);
            AddObjectContentToHashBuilder(associationEndMember.Identity);
            // Name is covered by Identity
            AddObjectContentToHashBuilder(associationEndMember.IsStoreGeneratedComputed);
            AddObjectContentToHashBuilder(associationEndMember.IsStoreGeneratedIdentity);
            AddObjectContentToHashBuilder(associationEndMember.RelationshipMultiplicity);

            base.Visit(associationEndMember);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(ReferentialConstraint referentialConstraint)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(referentialConstraint, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(referentialConstraint, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(referentialConstraint.Identity);

            base.Visit(referentialConstraint);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(RelationshipEndMember relationshipEndMember)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(relationshipEndMember, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(relationshipEndMember, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(relationshipEndMember.DeleteBehavior);
            AddObjectContentToHashBuilder(relationshipEndMember.Identity);
            // Name is covered by Identity
            AddObjectContentToHashBuilder(relationshipEndMember.IsStoreGeneratedComputed);
            AddObjectContentToHashBuilder(relationshipEndMember.IsStoreGeneratedIdentity);
            AddObjectContentToHashBuilder(relationshipEndMember.RelationshipMultiplicity);

            base.Visit(relationshipEndMember);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(TypeUsage typeUsage)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(typeUsage, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(typeUsage, index);

            #region Inner data visit

            //No need to add identity of TypeUsage to the hash since it would take into account
            //facets that viewgen would not care and we visit the important facets anyway.

            base.Visit(typeUsage);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(RelationshipType relationshipType)
        {
            base.Visit(relationshipType);
        }

        protected override void Visit(EdmType edmType)
        {
            base.Visit(edmType);
        }

        protected override void Visit(EnumType enumType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(enumType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(enumType, index);

            AddObjectContentToHashBuilder(enumType.Identity);
            Visit(enumType.UnderlyingType);

            base.Visit(enumType);

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EnumMember enumMember)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(enumMember, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(enumMember, index);

            AddObjectContentToHashBuilder(enumMember.Name);
            AddObjectContentToHashBuilder(enumMember.Value);

            base.Visit(enumMember);

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(CollectionType collectionType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(collectionType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(collectionType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(collectionType.Identity);
            // Identity contains Name, NamespaceName and FullName

            base.Visit(collectionType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(RefType refType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(refType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(refType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(refType.Identity);
            // Identity contains Name, NamespaceName and FullName

            base.Visit(refType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EntityTypeBase entityTypeBase)
        {
            base.Visit(entityTypeBase);
        }

        protected override void Visit(Facet facet)
        {
            int index;
            if (facet.Name
                != DbProviderManifest.NullableFacetName)
            {
                // skip all the non interesting facets
                return;
            }

            if (!AddObjectToSeenListAndHashBuilder(facet, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(facet, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(facet.Identity);
            // Identity already contains Name
            AddObjectContentToHashBuilder(facet.Value);

            base.Visit(facet);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(EdmFunction edmFunction)
        {
            // View Generation doesn't deal with functions
            // so just return;
        }

        protected override void Visit(ComplexType complexType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(complexType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(complexType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(complexType.Abstract);
            AddObjectContentToHashBuilder(complexType.Identity);
            // Identity covers, FullName, Name, and NamespaceName

            base.Visit(complexType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(PrimitiveType primitiveType)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(primitiveType, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(primitiveType, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(primitiveType.Name);
            AddObjectContentToHashBuilder(primitiveType.NamespaceName);

            base.Visit(primitiveType);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(FunctionParameter functionParameter)
        {
            int index;
            if (!AddObjectToSeenListAndHashBuilder(functionParameter, out index))
            {
                return;
            }

            AddObjectStartDumpToHashBuilder(functionParameter, index);

            #region Inner data visit

            AddObjectContentToHashBuilder(functionParameter.Identity);
            // Identity already has Name
            AddObjectContentToHashBuilder(functionParameter.Mode);

            base.Visit(functionParameter);

            #endregion

            AddObjectEndDumpToHashBuilder();
        }

        protected override void Visit(DbProviderManifest providerManifest)
        {
            // the provider manifest will be checked by all the other types lining up.
            // no need to store more info.
        }

        internal string HashValue
        {
            get { return m_hashSourceBuilder.ComputeHash(); }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void Clean()
        {
            m_hashSourceBuilder = new CompressingHashBuilder(MetadataHelper.CreateMetadataHashAlgorithm(m_MappingVersion));
            m_instanceNumber = 0;
            m_itemsAlreadySeen = new Dictionary<object, int>();
        }

        // <summary>
        // if already seen, then out the object instance index, return false;
        // if haven't seen, then add it to the m_itemAlreadySeen, out the current index, return true
        // </summary>
        private bool TryAddSeenItem(Object o, out int indexSeen)
        {
            if (!m_itemsAlreadySeen.TryGetValue(o, out indexSeen))
            {
                m_itemsAlreadySeen.Add(o, m_instanceNumber);

                indexSeen = m_instanceNumber;
                m_instanceNumber++;

                return true;
            }
            return false;
        }

        // <summary>
        // if the object has seen, then add the seen object style to the hash source, return false;
        // if not, then add it to the seen list, and append the object start dump to the hash source, return true
        // </summary>
        private bool AddObjectToSeenListAndHashBuilder(object o, out int instanceIndex)
        {
            if (o == null)
            {
                instanceIndex = -1;
                return false;
            }
            if (!TryAddSeenItem(o, out instanceIndex))
            {
                AddObjectStartDumpToHashBuilder(o, instanceIndex);
                AddSeenObjectToHashBuilder(instanceIndex);
                AddObjectEndDumpToHashBuilder();
                return false;
            }
            return true;
        }

        private void AddSeenObjectToHashBuilder(int instanceIndex)
        {
            Debug.Assert(instanceIndex >= 0, "referencing index should not be less than 0");
            m_hashSourceBuilder.AppendLine("Instance Reference: " + instanceIndex);
        }

        private void AddObjectStartDumpToHashBuilder(object o, int objectIndex)
        {
            m_hashSourceBuilder.AppendObjectStartDump(o, objectIndex);
        }

        private void AddObjectEndDumpToHashBuilder()
        {
            m_hashSourceBuilder.AppendObjectEndDump();
        }

        private void AddObjectContentToHashBuilder(object content)
        {
            if (content != null)
            {
                var formatContent = content as IFormattable;
                if (formatContent != null)
                {
                    // if the content is formattable, the following code made it culture invariant,
                    // for instance, the int, "30,000" can be formatted to "30-000" if the user 
                    // has a different language and region setting
                    m_hashSourceBuilder.AppendLine(formatContent.ToString(null, CultureInfo.InvariantCulture));
                }
                else
                {
                    m_hashSourceBuilder.AppendLine(content.ToString());
                }
            }
            else
            {
                m_hashSourceBuilder.AppendLine("NULL");
            }
        }

        // <summary>
        // Add V2 schema properties and attributes to the hash builder
        // </summary>
        private void AddV2ObjectContentToHashBuilder(object content, double version)
        {
            // if the version number is greater than or equal to V2, then we add the value
            if (version >= XmlConstants.EdmVersionForV2)
            {
                AddObjectContentToHashBuilder(content);
            }
        }

        internal static string GetMappingClosureHash(double mappingVersion, EntityContainerMapping entityContainerMapping, bool sortSequence = true)
        {
            DebugCheck.NotNull(entityContainerMapping);

            var visitor = new MetadataMappingHasherVisitor(mappingVersion, sortSequence);
            visitor.Visit(entityContainerMapping);
            return visitor.HashValue;
        }
    }
}
