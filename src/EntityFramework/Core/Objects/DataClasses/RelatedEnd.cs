// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.DataClasses
{
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.Internal;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for EntityCollection and EntityReference
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [DataContract]
    [Serializable]
    public abstract class RelatedEnd : IRelatedEnd
    {
        //-----------------
        // Internal Constructors
        //-----------------

        // <summary>
        // The default constructor is required for some serialization scenarios with EntityReference.
        // </summary>
        internal RelatedEnd()
        {
            _wrappedOwner = NullEntityWrapper.NullWrapper;
        }

        internal RelatedEnd(IEntityWrapper wrappedOwner, RelationshipNavigation navigation, IRelationshipFixer relationshipFixer)
        {
            DebugCheck.NotNull(wrappedOwner);
            DebugCheck.NotNull(wrappedOwner.Entity);
            DebugCheck.NotNull(navigation);
            DebugCheck.NotNull(relationshipFixer);

            InitializeRelatedEnd(wrappedOwner, navigation, relationshipFixer);
        }

        // ------
        // Fields
        // ------
        private const string _entityKeyParamName = "EntityKeyValue";

        // The following fields are serialized.  Adding or removing a serialized field is considered
        // a breaking change.  This includes changing the field type or field name of existing
        // serialized fields. If you need to make this kind of change, it may be possible, but it
        // will require some custom serialization/deserialization code.
        // These fields should not be changed once they have been initialized with non-null values, but they can't be read-only because there
        // are serialization scenarios where they have to be set after construction

        // <summary>
        // Note that this field should no longer be used directly.  Instead, use the _wrappedOwner
        // field.  This field is retained only for compatibility with the serialization format introduced in v1.
        // </summary>
        [Obsolete]
        private IEntityWithRelationships _owner;

        private RelationshipNavigation _navigation;
        private IRelationshipFixer _relationshipFixer;

        internal bool _isLoaded;

        // The fields in this group are set only when attached to a context, so we don't need to serialize.
        [NonSerialized]
        private RelationshipSet _relationshipSet;

        [NonSerialized]
        private ObjectContext _context;

        [NonSerialized]
        private bool _usingNoTracking;

        [NonSerialized]
        private RelationshipType _relationMetadata;

        [NonSerialized]
        private RelationshipEndMember _fromEndMember; //owner end property

        [NonSerialized]
        private RelationshipEndMember _toEndMember;

        [NonSerialized]
        private string _sourceQuery;

        [NonSerialized]
        private IEnumerable<EdmMember> _sourceQueryParamProperties; // indicates which properties populate query parameters

        [NonSerialized]
        internal bool _suppressEvents;

        [NonSerialized]
        internal CollectionChangeEventHandler _onAssociationChanged;

        [NonSerialized]
        private IEntityWrapper _wrappedOwner;

        [NonSerialized]
        private EntityWrapperFactory _entityWrapperFactory;

        // ------
        // Events
        // ------

        /// <summary>Occurs when a change is made to a related end.</summary>
        public event CollectionChangeEventHandler AssociationChanged
        {
            add
            {
                CheckOwnerNull();
                _onAssociationChanged += value;
            }
            remove
            {
                CheckOwnerNull();
                _onAssociationChanged -= value;
            }
        }

        // <summary>
        // internal event to notify change in collection
        // </summary>
        internal virtual event CollectionChangeEventHandler AssociationChangedForObjectView
        {
            // we fire this event only from EntityCollection, definitely not from EntityReference
            add { Debug.Assert(false, "should never happen"); }
            remove { Debug.Assert(false, "should never happen"); }
        }

        // ----------
        // Properties
        // ----------

        internal bool IsForeignKey
        {
            get
            {
                Debug.Assert(ObjectContext != null, "the IsForeignKey property shouldn't be used in detached scenarios");
                Debug.Assert(_relationMetadata != null, "this._relationMetadata == null");

                return ((AssociationType)_relationMetadata).IsForeignKey;
            }
        }

        // <summary>
        // This class describes a relationship navigation from the
        // navigation property on one entity to another entity.
        // RelationshipNavigation uniquely identify a relationship type.
        // The RelationshipNavigation class is internal only, so this property is also internal.
        // See RelationshipName, SourceRoleName, and TargetRoleName for the public exposure
        // of the information contained in this RelationshipNavigation.
        // </summary>
        internal RelationshipNavigation RelationshipNavigation
        {
            get { return _navigation; }
        }

        /// <summary>Gets the name of the relationship in which this related end participates.</summary>
        /// <returns>
        /// The name of the relationship in which this <see cref="T:System.Data.Entity.Core.Objects.DataClasses.RelatedEnd" /> participates. The relationship name is not namespace qualified.
        /// </returns>
        [SoapIgnore]
        [XmlIgnore]
        public string RelationshipName
        {
            get
            {
                CheckOwnerNull();
                return _navigation.RelationshipName;
            }
        }

        /// <summary>Gets the role name at the source end of the relationship.</summary>
        /// <returns>
        /// A <see cref="T:System.String" /> that is the role name.
        /// </returns>
        [SoapIgnore]
        [XmlIgnore]
        public virtual string SourceRoleName
        {
            get
            {
                CheckOwnerNull();
                return _navigation.From;
            }
        }

        /// <summary>Gets the role name at the target end of the relationship.</summary>
        /// <returns>
        /// A <see cref="T:System.String" /> that is the role name.
        /// </returns>
        [SoapIgnore]
        [XmlIgnore]
        public virtual string TargetRoleName
        {
            get
            {
                CheckOwnerNull();
                return _navigation.To;
            }
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IEnumerable" /> that represents the objects that belong to the related end.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerable" /> that represents the objects that belong to the related end.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IEnumerable IRelatedEnd.CreateSourceQuery()
        {
            CheckOwnerNull();
            return CreateSourceQueryInternal();
        }

        internal virtual IEntityWrapper WrappedOwner
        {
            get { return _wrappedOwner; }
        }

        internal virtual ObjectContext ObjectContext
        {
            get { return _context; }
        }

        internal virtual EntityWrapperFactory EntityWrapperFactory
        {
            get
            {
                if (_entityWrapperFactory == null)
                {
                    _entityWrapperFactory = new EntityWrapperFactory();
                }
                return _entityWrapperFactory;
            }
        }

        /// <summary>Gets a reference to the metadata for the related end.</summary>
        /// <returns>
        /// A <see cref="T:System.Data.Entity.Core.Metadata.Edm.RelationshipSet" /> object that contains metadata for the end of a relationship.
        /// </returns>
        [SoapIgnore]
        [XmlIgnore]
        public virtual RelationshipSet RelationshipSet
        {
            get
            {
                CheckOwnerNull();
                return _relationshipSet;
            }
        }

        internal virtual RelationshipType RelationMetadata
        {
            get { return _relationMetadata; }
        }

        internal virtual RelationshipEndMember ToEndMember
        {
            get { return _toEndMember; }
        }

        internal bool UsingNoTracking
        {
            get { return _usingNoTracking; }
        }

        internal MergeOption DefaultMergeOption
        {
            get { return UsingNoTracking ? MergeOption.NoTracking : MergeOption.AppendOnly; }
        }

        internal virtual RelationshipEndMember FromEndMember
        {
            get { return _fromEndMember; }
        }

        /// <inheritdoc />
        [SoapIgnore]
        [XmlIgnore]
        public bool IsLoaded
        {
            get
            {
                CheckOwnerNull();
                return _isLoaded;
            }
            set
            {
                CheckOwnerNull();

                _isLoaded = value;
            }
        }

        // <summary>
        // This is the query which represents the source of the
        // related end.  It is constructed on demand using the
        // _connection and _cache fields and a query string based on
        // the type of related end and the metadata passed into its
        // constructor indicating the particular EDM construct the
        // related end models. This method is called by both subclasses of this type
        // and those subclasses pass in their generic type parameter in order
        // to produce an ObjectQuery of the right type. This allows this common
        // functionality to be implemented here in the base class while still
        // allowing the base class to be non-generic.
        // </summary>
        // <param name="mergeOption"> MergeOption to use when creating the query </param>
        // <param name="hasResults"> Indicates whether the query can produce results. For instance, a lookup with null key values cannot produce results. </param>
        // <returns> The query loading related entities. </returns>
        internal ObjectQuery<TEntity> CreateSourceQuery<TEntity>(MergeOption mergeOption, out bool hasResults)
        {
            // must have a context
            if (_context == null)
            {
                hasResults = false;
                return null;
            }

            var stateEntry = _context.ObjectStateManager.FindEntityEntry(_wrappedOwner.Entity);
            EntityState entityState;
            if (stateEntry == null)
            {
                if (UsingNoTracking)
                {
                    entityState = EntityState.Detached;
                }
                else
                {
                    throw Error.Collections_InvalidEntityStateSource();
                }
            }
            else
            {
                Debug.Assert(stateEntry != null, "Entity should exist in the current context");
                entityState = stateEntry.State;
            }

            //Throw if entity is in added state, unless this is the dependent end of an FK relationship
            if (entityState == EntityState.Added
                &&
                (!IsForeignKey ||
                 !IsDependentEndOfReferentialConstraint(checkIdentifying: false)))
            {
                throw Error.Collections_InvalidEntityStateSource();
            }

            Debug.Assert(
                !(entityState != EntityState.Detached && UsingNoTracking),
                "Entity with NoTracking option cannot exist in the ObjectStateManager");

            // the CreateSourceQuery method can only return non-NULL when we're
            // either detached & mergeOption is NoTracking or
            // Added/Modified/Unchanged/Deleted and mergeOption is NOT NoTracking
            // (if entity is attached to the context, mergeOption should never be NoTracking)
            // If the entity state is added, at this point it is an FK dependent end
            if (!((entityState == EntityState.Detached && UsingNoTracking) ||
                  entityState == EntityState.Modified ||
                  entityState == EntityState.Unchanged ||
                  entityState == EntityState.Deleted ||
                  entityState == EntityState.Added))
            {
                hasResults = false;
                return null;
            }

            if (null == _sourceQuery)
            {
                _sourceQuery = GenerateQueryText();
            }

            var query = new ObjectQuery<TEntity>(_sourceQuery, _context, mergeOption);

            hasResults = AddQueryParameters(query);

            // It should not be possible to add or remove parameters from the new query, since the query text
            // is fixed. Adding or removing parameters will likely make the query fail to execute.
            query.Parameters.SetReadOnly(true);

            // Return the new ObjectQuery. Note that this is intentionally a tear-off so that any changes made
            // to its Parameters collection (or the ObjectParameters themselves) have no effect on anyone else
            // that may retrieve this query - each access will always return a new ObjectQuery instance.
            return query;
        }

        private string GenerateQueryText()
        {
            Debug.Assert(_relationshipSet != null, "If we are attached to a context, we should have a relationship set.");
            Debug.Assert(_relationshipSet.BuiltInTypeKind == BuiltInTypeKind.AssociationSet, "Non-AssociationSet Relationship Set?");

            var key = _wrappedOwner.EntityKey;
            if (key == null)
            {
                throw Error.EntityKey_UnexpectedNull();
            }

            var associationMetadata = (AssociationType)_relationMetadata;

            var targetEntitySet = ((AssociationSet)_relationshipSet).AssociationSetEnds[_toEndMember.Name].EntitySet;

            var targetEntityType = MetadataHelper.GetEntityTypeForEnd((AssociationEndMember)_toEndMember);
            var ofTypeRequired = false;
            if (!targetEntitySet.ElementType.EdmEquals(targetEntityType)
                &&
                !TypeSemantics.IsSubTypeOf(targetEntitySet.ElementType, targetEntityType))
            {
                // If the type contained in the target entity set is not equal to
                // or a subtype of the referenced type, then an OfType must be
                // applied to the target entityset to yield only those elements that
                // are of the referenced type or a subtype of the referenced type.
                ofTypeRequired = true;

                // The type name used in the OfType clause must be the name of the
                // corresponding O-Space Entity type, since the source query will be
                // parsed using the CLR perspective (by ObjectQuery).
                var targetOSpaceTypeUsage = ObjectContext.MetadataWorkspace.GetOSpaceTypeUsage(TypeUsage.Create(targetEntityType));
                targetEntityType = (EntityType)targetOSpaceTypeUsage.EdmType;
            }

            StringBuilder sourceBuilder;
            if (associationMetadata.IsForeignKey)
            {
                var fkConstraint = associationMetadata.ReferentialConstraints[0];
                var principalProps = fkConstraint.FromProperties;
                var dependentProps = fkConstraint.ToProperties;
                Debug.Assert(principalProps.Count == dependentProps.Count, "Mismatched foreign key properties?");

                if (fkConstraint.ToRole.EdmEquals(_toEndMember))
                {
                    // This related end goes from 'principal' to 'dependent', and has the key of the principal.
                    // In this case it is sufficient to filter the target (dependent) set where the foreign key
                    // properties have the same values as the corresponding entity key properties from the principal.
                    //
                    // SELECT VALUE D 
                    //   FROM OfType(##DependentEntityset, ##DependentEntityType) 
                    // AS D
                    // WHERE 
                    //   D.DependentProperty1 = @PrincipalProperty1 [AND
                    //   ...
                    //   D.DependentPropertyN = @PrincipalPropertyN]
                    //
                    // Note that the OfType operator can be omitted if the element type of ##DependentEntitySet
                    // is equal to the Entity type produced by the target end of the relationship.
                    sourceBuilder = new StringBuilder("SELECT VALUE D FROM ");
                    AppendEntitySet(sourceBuilder, targetEntitySet, targetEntityType, ofTypeRequired);
                    sourceBuilder.Append(" AS D WHERE ");

                    // For each principal key property there is a corresponding query parameter that supplies the value
                    // from this owner's entity key, so KeyParam1 corresponds to the first key member, etc.
                    // We remember the order of the corresponding principal key values in the _sourceQueryParamProperties
                    // field.
                    var keyParamNameGen = new AliasGenerator(_entityKeyParamName); // Aliases are cached in AliasGenerator
                    _sourceQueryParamProperties = principalProps;

                    for (var idx = 0; idx < dependentProps.Count; idx++)
                    {
                        if (idx > 0)
                        {
                            sourceBuilder.Append(" AND ");
                        }

                        sourceBuilder.Append("D.[");
                        sourceBuilder.Append(dependentProps[idx].Name);
                        sourceBuilder.Append("] = @");
                        sourceBuilder.Append(keyParamNameGen.Next());
                    }
                }
                else
                {
                    // This related end goes from 'dependent' to 'principal', and has the key of the dependent
                    // In this case it is necessary to filter the target (principal) entity set on the foreign 
                    // key relationship properties to retrieve the corresponding principal entity.
                    //
                    // SELECT VALUE P FROM
                    //   OfType(##PrincipalEntityset, ##PrincipalEntityType) AS P
                    // WHERE 
                    //   P.PrincipalProperty1 = @DependentProperty1 AND ...
                    //
                    Debug.Assert(
                        fkConstraint.FromRole.EdmEquals(_toEndMember),
                        "Source query for foreign key association related end is not based on principal or dependent?");

                    sourceBuilder = new StringBuilder("SELECT VALUE P FROM ");
                    AppendEntitySet(sourceBuilder, targetEntitySet, targetEntityType, ofTypeRequired);
                    sourceBuilder.Append(" AS P WHERE ");

                    var keyParamNameGen = new AliasGenerator(_entityKeyParamName); // Aliases are cached in AliasGenerator
                    _sourceQueryParamProperties = dependentProps;
                    for (var idx = 0; idx < principalProps.Count; idx++)
                    {
                        if (idx > 0)
                        {
                            sourceBuilder.Append(" AND ");
                        }
                        sourceBuilder.Append("P.[");
                        sourceBuilder.Append(principalProps[idx].Name);
                        sourceBuilder.Append("] = @");
                        sourceBuilder.Append(keyParamNameGen.Next());
                    }
                    return sourceBuilder.ToString();
                }
            }
            else
            {
                // Translate to:
                // SELECT VALUE [TargetEntity]
                //  FROM 
                //      (SELECT VALUE x FROM ##RelationshipSet AS x
                //       WHERE Key(x.[##SourceRoleName]) = ROW(@key1 AS key1[..., @keyN AS keyN])
                //       ) AS [AssociationEntry]
                //  INNER JOIN 
                //       OfType(##TargetEntityset, ##TargetRole.EntityType) AS [TargetEntity] 
                //  ON
                //       Key([AssociationEntry].##TargetRoleName) = Key(Ref([TargetEntity]))
                //
                // Note that the OfType operator can be omitted if the element type of ##TargetEntitySet
                // is equal to the Entity type produced by the target end of the relationship.

                sourceBuilder = new StringBuilder("SELECT VALUE [TargetEntity] FROM (SELECT VALUE x FROM ");
                sourceBuilder.Append("[");
                sourceBuilder.Append(_relationshipSet.EntityContainer.Name);
                sourceBuilder.Append("].[");
                sourceBuilder.Append(_relationshipSet.Name);
                sourceBuilder.Append("] AS x WHERE Key(x.[");
                sourceBuilder.Append(_fromEndMember.Name);
                sourceBuilder.Append("]) = ");

                AppendKeyParameterRow(sourceBuilder, key.GetEntitySet(ObjectContext.MetadataWorkspace).ElementType.KeyMembers);

                sourceBuilder.Append(") AS [AssociationEntry] INNER JOIN ");

                AppendEntitySet(sourceBuilder, targetEntitySet, targetEntityType, ofTypeRequired);

                sourceBuilder.Append(" AS [TargetEntity] ON Key([AssociationEntry].[");
                sourceBuilder.Append(_toEndMember.Name);
                sourceBuilder.Append("]) = Key(Ref([TargetEntity]))");
            }

            return sourceBuilder.ToString();
        }

        private bool AddQueryParameters<TEntity>(ObjectQuery<TEntity> query)
        {
            var key = _wrappedOwner.EntityKey;
            if (key == null)
            {
                throw Error.EntityKey_UnexpectedNull();
            }

            var hasResults = true;

            // Add a parameter for each entity key value found on the key.
            var paramNameGen = new AliasGenerator(_entityKeyParamName); // Aliases are cached in AliasGenerator
            var parameterMembers = _sourceQueryParamProperties
                                   ?? key.GetEntitySet(ObjectContext.MetadataWorkspace).ElementType.KeyMembers;

            foreach (var parameterMember in parameterMembers)
            {
                // Create a new ObjectParameter with the next parameter name and the next entity value.
                // When _sourceQueryParamProperties are defined, it means we are handling a foreign key association. For an FK association,
                // the current entity values are considered truth. Otherwise, we use EntityKey values for backwards
                // compatibility with independent association behaviors in .NET 3.5.
                object value;
                if (null == _sourceQueryParamProperties)
                {
                    // retrieve the value from the entity key (independent association lookup)
                    value = _wrappedOwner.EntityKey.EntityKeyValues.Single(ekv => ekv.Key == parameterMember.Name).Value;
                }
                else
                {
                    // retrieve the value from the entity itself (FK lookup)
                    if (CachedForeignKeyIsConceptualNull())
                    {
                        value = null;
                    }
                    else
                    {
                        value = GetCurrentValueFromEntity(parameterMember);
                    }
                }
                ObjectParameter queryParam;
                if (null == value)
                {
                    var parameterEdmType = parameterMember.TypeUsage.EdmType;
                    Debug.Assert(Helper.IsScalarType(parameterEdmType), "Only primitive or enum type expected for parameters");

                    var parameterClrType = Helper.IsPrimitiveType(parameterEdmType)
                                               ? ((PrimitiveType)parameterEdmType).ClrEquivalentType
                                               : (ObjectContext.MetadataWorkspace.GetObjectSpaceType((EnumType)parameterEdmType)).ClrType;

                    queryParam = new ObjectParameter(paramNameGen.Next(), parameterClrType);
                    // If any lookup value is null, the query cannot match any rows.
                    hasResults = false;
                }
                else
                {
                    queryParam = new ObjectParameter(paramNameGen.Next(), value);
                }

                // Map the type of the key member to C-Space and explicitly specify this mapped type
                // as the effective type of the new ObjectParameter - this is required so that the
                // type of the key value parameter matches the declared type of the key member when
                // the query text is parsed.
                queryParam.TypeUsage = Helper.GetModelTypeUsage(parameterMember);

                // Add the new parameter to the Parameters collection of the query.
                query.Parameters.Add(queryParam);
            }

            return hasResults;
        }

        private object GetCurrentValueFromEntity(EdmMember member)
        {
            // retrieve member accessor from the object context (which already keeps track of the relevant
            // metadata)
            var metaType = _context.ObjectStateManager.GetOrAddStateManagerTypeMetadata(member.DeclaringType);
            var metaMember = metaType.Member(metaType.GetOrdinalforCLayerMemberName(member.Name));
            return metaMember.GetValue(_wrappedOwner.Entity);
        }

        private static void AppendKeyParameterRow(StringBuilder sourceBuilder, IList<EdmMember> keyMembers)
        {
            sourceBuilder.Append("ROW(");
            var keyParamNameGen = new AliasGenerator(_entityKeyParamName); // Aliases are cached in AliasGenerator
            var keyMemberCount = keyMembers.Count;
            for (var idx = 0; idx < keyMemberCount; idx++)
            {
                var keyParamName = keyParamNameGen.Next();
                sourceBuilder.Append("@");
                sourceBuilder.Append(keyParamName);
                sourceBuilder.Append(" AS ");
                sourceBuilder.Append(keyParamName);

                if (idx < keyMemberCount - 1)
                {
                    sourceBuilder.Append(",");
                }
            }
            sourceBuilder.Append(")");
        }

        private static void AppendEntitySet(
            StringBuilder sourceBuilder, EntitySet targetEntitySet, EntityType targetEntityType, bool ofTypeRequired)
        {
            if (ofTypeRequired)
            {
                sourceBuilder.Append("OfType(");
            }
            sourceBuilder.Append("[");
            sourceBuilder.Append(targetEntitySet.EntityContainer.Name);
            sourceBuilder.Append("].[");
            sourceBuilder.Append(targetEntitySet.Name);
            sourceBuilder.Append("]");
            if (ofTypeRequired)
            {
                sourceBuilder.Append(", [");
                if (!string.IsNullOrEmpty(targetEntityType.NamespaceName))
                {
                    sourceBuilder.Append(targetEntityType.NamespaceName);
                    sourceBuilder.Append("].[");
                }
                sourceBuilder.Append(targetEntityType.Name);
                sourceBuilder.Append("])");
            }
        }

        // <summary>
        // Validates that a call to Load has the correct conditions
        // This helps to reduce the complexity of the Load call (SQLBU 524128)
        // </summary>
        // <returns> See RelatedEnd.CreateSourceQuery method. This is returned here so we can create it and validate the state before returning it to the caller </returns>
        internal virtual ObjectQuery<TEntity> ValidateLoad<TEntity>(MergeOption mergeOption, string relatedEndName, out bool hasResults)
        {
            var sourceQuery = CreateSourceQuery<TEntity>(mergeOption, out hasResults);
            if (null == sourceQuery)
            {
                throw Error.RelatedEnd_RelatedEndNotAttachedToContext(relatedEndName);
            }

            var entry = ObjectContext.ObjectStateManager.FindEntityEntry(_wrappedOwner.Entity);
            //Throw in case entity is in deleted state
            if (entry != null
                && entry.State == EntityState.Deleted)
            {
                throw Error.Collections_InvalidEntityStateLoad(relatedEndName);
            }

            // MergeOption for Load must be NoTracking if and only if the source entity was NoTracking. If the source entity was 
            // retrieved with any other MergeOption, the Load MergeOption can be anything but NoTracking. I.e. The entity could
            // have been loaded with OverwriteChanges and the Load option can be AppendOnly.
            if (UsingNoTracking != (mergeOption == MergeOption.NoTracking))
            {
                throw Error.RelatedEnd_MismatchedMergeOptionOnLoad(mergeOption);
            }

            if (UsingNoTracking)
            {
                if (IsLoaded)
                {
                    throw Error.RelatedEnd_LoadCalledOnAlreadyLoadedNoTrackedRelatedEnd();
                }

                if (!IsEmpty())
                {
                    throw Error.RelatedEnd_LoadCalledOnNonEmptyNoTrackedRelatedEnd();
                }
            }

            return sourceQuery;
        }

        // -------
        // Methods
        // -------

        /// <summary>
        /// Loads the related object or objects into the related end with the default merge option.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// When the source object was retrieved by using a <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" /> query 
        /// and the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" /> is not <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />
        /// or the related objects are already loaded
        /// or when the source object is not attached to the <see cref="T:System.Data.Entity.Core.Objects.ObjectContext" />
        /// or when the source object is being tracked but is in the
        /// <see cref="F:System.Data.Entity.EntityState.Added" /> or <see cref="F:System.Data.Entity.EntityState.Deleted" /> state
        /// or the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// used for <see cref="M:System.Data.Entity.Core.Objects.DataClasses.RelatedEnd.Load" />
        /// is <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />.
        /// </exception>
        public void Load()
        {
            // CheckOwnerNull is called in the impementation
            Load(DefaultMergeOption);
        }

#if !NET40

        /// <summary>
        /// Asynchronously loads the related object or objects into the related end with the default merge option.
        /// </summary>
        /// <remarks>
        /// Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        /// that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// When the source object was retrieved by using a <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" /> query 
        /// and the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" /> is not <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />
        /// or the related objects are already loaded
        /// or when the source object is not attached to the <see cref="T:System.Data.Entity.Core.Objects.ObjectContext" />
        /// or when the source object is being tracked but is in the
        /// <see cref="F:System.Data.Entity.EntityState.Added" /> or <see cref="F:System.Data.Entity.EntityState.Deleted" /> state
        /// or the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// used for <see cref="M:System.Data.Entity.Core.Objects.DataClasses.RelatedEnd.Load" />
        /// is <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />.
        /// </exception>
        public Task LoadAsync(CancellationToken cancellationToken)
        {
            return LoadAsync(DefaultMergeOption, cancellationToken);
        }

#endif

        /// <summary>
        /// Loads an object or objects from the related end with the specified merge option.
        /// </summary>
        /// <param name="mergeOption">
        /// The <see cref="T:System.Data.Entity.Core.Objects.MergeOption" /> to use when merging objects into an existing
        /// <see cref="T:System.Data.Entity.Core.Objects.DataClasses.EntityCollection`1" />.
        /// </param>
        /// <exception cref="T:System.InvalidOperationException">
        /// When the source object was retrieved by using a <see  cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />  query 
        /// and the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// is not <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />
        /// or the related objects are already loaded
        /// or when the source object is not attached to the <see cref="T:System.Data.Entity.Core.Objects.ObjectContext" />
        /// or when the source object is being tracked but is in the
        /// <see cref="F:System.Data.Entity.EntityState.Added" />  or <see cref="F:System.Data.Entity.EntityState.Deleted" /> state 
        /// or the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// used for <see cref="M:System.Data.Entity.Core.Objects.DataClasses.RelatedEnd.Load" />
        /// is <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />.
        /// </exception>
        public abstract void Load(MergeOption mergeOption);

#if !NET40

        /// <summary>
        /// Asynchronously loads an object or objects from the related end with the specified merge option.
        /// </summary>
        /// <remarks>
        /// Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        /// that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="mergeOption">
        /// The <see cref="T:System.Data.Entity.Core.Objects.MergeOption" /> to use when merging objects into an existing
        /// <see cref="T:System.Data.Entity.Core.Objects.DataClasses.EntityCollection`1" />.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// When the source object was retrieved by using a <see  cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />  query 
        /// and the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// is not <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />
        /// or the related objects are already loaded
        /// or when the source object is not attached to the <see cref="T:System.Data.Entity.Core.Objects.ObjectContext" />
        /// or when the source object is being tracked but is in the
        /// <see cref="F:System.Data.Entity.EntityState.Added" />  or <see cref="F:System.Data.Entity.EntityState.Deleted" /> state 
        /// or the <see cref="T:System.Data.Entity.Core.Objects.MergeOption" />
        /// used for <see cref="M:System.Data.Entity.Core.Objects.DataClasses.RelatedEnd.Load" />
        /// is <see cref="F:System.Data.Entity.Core.Objects.MergeOption.NoTracking" />.
        /// </exception>
        public abstract Task LoadAsync(MergeOption mergeOption, CancellationToken cancellationToken);

#endif

        internal void DeferredLoad()
        {
            if (_wrappedOwner != null
                &&
                _wrappedOwner != NullEntityWrapper.NullWrapper
                &&
                !IsLoaded
                &&
                _context != null
                &&
                _context.ContextOptions.LazyLoadingEnabled
                &&
                !_context.InMaterialization
                &&
                CanDeferredLoad)
            {
                // Ensure the parent EntityState is NoTracking, Unchanged, or Modified
                // Detached, Added, and Deleted parents cannot call Load
                Debug.Assert(_wrappedOwner != null, "Wrapper owner should never be null");
                if (UsingNoTracking ||
                    (_wrappedOwner.ObjectStateEntry != null &&
                     (_wrappedOwner.ObjectStateEntry.State == EntityState.Unchanged ||
                      _wrappedOwner.ObjectStateEntry.State == EntityState.Modified ||
                      (_wrappedOwner.ObjectStateEntry.State == EntityState.Added &&
                       IsForeignKey &&
                       IsDependentEndOfReferentialConstraint(false)))))
                {
                    // Avoid infinite recursive calls
                    _context.ContextOptions.LazyLoadingEnabled = false;
                    try
                    {
                        Load();
                    }
                    finally
                    {
                        _context.ContextOptions.LazyLoadingEnabled = true;
                    }
                }
            }
        }

        internal virtual bool CanDeferredLoad
        {
            get { return true; }
        }

        // <summary>
        // Takes a list of related entities and merges them into the current collection.
        // </summary>
        // <param name="collection"> Entities to relate to the owner of this EntityCollection </param>
        // <param name="mergeOption"> MergeOption to use when updating existing relationships </param>
        // <param name="setIsLoaded"> Indicates whether IsLoaded should be set to true after the Load is complete. Should be false in cases where we cannot guarantee that the set of entities is complete and matches the server, such as Attach. </param>
        internal virtual void Merge<TEntity>(IEnumerable<TEntity> collection, MergeOption mergeOption, bool setIsLoaded)
        {
            DebugCheck.NotNull(collection);

            var refreshedCollection = collection as List<IEntityWrapper>;
            if (refreshedCollection == null)
            {
                refreshedCollection = new List<IEntityWrapper>();
                var targetEntitySet = ((AssociationSet)RelationshipSet).AssociationSetEnds[TargetRoleName].EntitySet;
                foreach (var entity in collection)
                {
                    var wrapper = EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext);
                    // When the MergeOption is NoTraking, we need to make sure the wrapper reflects the current context and
                    // has an EntityKey
                    if (mergeOption == MergeOption.NoTracking)
                    {
                        EntityWrapperFactory.UpdateNoTrackingWrapper(wrapper, ObjectContext, targetEntitySet);
                    }
                    refreshedCollection.Add(wrapper);
                }
            }
            Merge<TEntity>(refreshedCollection, mergeOption, setIsLoaded);
        }

        // Internal version of Merge that works on wrapped entities.
        internal virtual void Merge<TEntity>(List<IEntityWrapper> collection, MergeOption mergeOption, bool setIsLoaded)
        {
            //Dev note: do not add event firing in Merge API, if it need to be added, add it to the caller
            if (WrappedOwner.EntityKey == null)
            {
                throw Error.EntityKey_UnexpectedNull();
            }

            ObjectContext.ObjectStateManager.UpdateRelationships(
                ObjectContext, mergeOption, (AssociationSet)RelationshipSet, (AssociationEndMember)FromEndMember, WrappedOwner,
                (AssociationEndMember)ToEndMember, collection, setIsLoaded);

            if (setIsLoaded)
            {
                // If the input collection contains all related entities, mark the collection as "loaded"
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Attaches an entity to the related end.  This method works in exactly the same way as Attach(object).
        /// It is maintained for backward compatibility with previous versions of IRelatedEnd.
        /// </summary>
        /// <param name="entity"> The entity to attach to the related end </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when
        /// <paramref name="entity" />
        /// is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity cannot be related via the current relationship end.</exception>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IRelatedEnd.Attach(IEntityWithRelationships entity)
        {
            Check.NotNull(entity, "entity");

            ((IRelatedEnd)this).Attach((object)entity);
        }

        /// <summary>
        /// Attaches an entity to the related end. If the related end is already filled
        /// or partially filled, this merges the existing entities with the given entity. The given
        /// entity is not assumed to be the complete set of related entities.
        /// Owner and all entities passed in must be in Unchanged or Modified state.
        /// Deleted elements are allowed only when the state manager is already tracking the relationship
        /// instance.
        /// </summary>
        /// <param name="entity"> The entity to attach to the related end </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when
        /// <paramref name="entity" />
        /// is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity cannot be related via the current relationship end.</exception>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IRelatedEnd.Attach(object entity)
        {
            Check.NotNull(entity, "entity");

            CheckOwnerNull();
            Attach(new[] { EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext) }, false);
        }

        internal void Attach(IEnumerable<IEntityWrapper> wrappedEntities, bool allowCollection)
        {
            CheckOwnerNull();
            ValidateOwnerForAttach();

            // validate children and collect them in the "refreshedCollection" for this instance
            var index = 0;
            var collection = new List<IEntityWrapper>();

            foreach (var entity in wrappedEntities)
            {
                ValidateEntityForAttach(entity, index++, allowCollection);
                collection.Add(entity);
            }

            _suppressEvents = true;
            try
            {
                // After Attach, the two entities should be related in the Unchanged state, so use OverwriteChanges
                // Since no query is done in this case, the MergeOption only controls the relationships
                Merge(collection, MergeOption.OverwriteChanges, false /*setIsLoaded*/);
                var constraint = ((AssociationType)RelationMetadata).ReferentialConstraints.FirstOrDefault();
                if (constraint != null)
                {
                    var stateManager = ObjectContext.ObjectStateManager;
                    var ownerEntry = stateManager.FindEntityEntry(_wrappedOwner.Entity);
                    Debug.Assert(ownerEntry != null, "Both entities should be attached.");
                    if (IsDependentEndOfReferentialConstraint(checkIdentifying: false))
                    {
                        Debug.Assert(collection.Count == 1, "Dependant should attach to single principal");
                        if (!VerifyRIConstraintsWithRelatedEntry(
                            constraint, ownerEntry.GetCurrentEntityValue, collection[0].ObjectStateEntry.EntityKey))
                        {
                            throw new InvalidOperationException(constraint.BuildConstraintExceptionMessage());
                        }
                    }
                    else
                    {
                        foreach (var wrappedTarget in collection)
                        {
                            var targetRelatedEnd = GetOtherEndOfRelationship(wrappedTarget);
                            if (targetRelatedEnd.IsDependentEndOfReferentialConstraint(checkIdentifying: false))
                            {
                                var targetEntry = stateManager.FindEntityEntry((targetRelatedEnd).WrappedOwner.Entity);
                                Debug.Assert(targetEntry != null, "Both entities should be attached.");
                                if (!VerifyRIConstraintsWithRelatedEntry(
                                    constraint, targetEntry.GetCurrentEntityValue, ownerEntry.EntityKey))
                                {
                                    throw new InvalidOperationException(constraint.BuildConstraintExceptionMessage());
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _suppressEvents = false;
            }
            OnAssociationChanged(CollectionChangeAction.Refresh, null);
        }

        // verifies requirements for Owner in Attach()
        internal void ValidateOwnerForAttach()
        {
            if (null == ObjectContext || UsingNoTracking)
            {
                throw Error.RelatedEnd_InvalidOwnerStateForAttach();
            }

            // find state entry
            var stateEntry = ObjectContext.ObjectStateManager.GetEntityEntry(_wrappedOwner.Entity);
            if (stateEntry.State != EntityState.Modified
                &&
                stateEntry.State != EntityState.Unchanged)
            {
                throw Error.RelatedEnd_InvalidOwnerStateForAttach();
            }
        }

        // verifies requirements for child entity passed to Attach()
        internal void ValidateEntityForAttach(IEntityWrapper wrappedEntity, int index, bool allowCollection)
        {
            if (null == wrappedEntity
                || null == wrappedEntity.Entity)
            {
                if (allowCollection)
                {
                    throw Error.RelatedEnd_InvalidNthElementNullForAttach(index);
                }
                else
                {
                    throw new ArgumentNullException("wrappedEntity");
                }
            }

            // Having this verification here results in having the same exception no matter how the further code path is changed.
            VerifyType(wrappedEntity);

            // verify the entity exists in the current context
            Debug.Assert(null != ObjectContext, "ObjectContext must not be null after call to ValidateOwnerForAttach");
            Debug.Assert(!UsingNoTracking, "We should not be here for NoTracking case.");
            var stateEntry = ObjectContext.ObjectStateManager.FindEntityEntry(wrappedEntity.Entity);
            if (null == stateEntry
                || !ReferenceEquals(stateEntry.Entity, wrappedEntity.Entity))
            {
                if (allowCollection)
                {
                    throw Error.RelatedEnd_InvalidNthElementContextForAttach(index);
                }
                else
                {
                    throw Error.RelatedEnd_InvalidEntityContextForAttach();
                }
            }
            Debug.Assert(stateEntry.State != EntityState.Detached, "State cannot be detached if the entry was retrieved from the context");

            // verify the state of the entity (may not be in added state, since 
            // we only support attaching relationships to existing entities)
            if (stateEntry.State != EntityState.Unchanged
                &&
                stateEntry.State != EntityState.Modified)
            {
                if (allowCollection)
                {
                    throw Error.RelatedEnd_InvalidNthElementStateForAttach(index);
                }
                else
                {
                    throw Error.RelatedEnd_InvalidEntityStateForAttach();
                }
            }
        }

        internal abstract IEnumerable CreateSourceQueryInternal();

        /// <summary>
        /// Adds an entity to the related end.  This method works in exactly the same way as Add(object).
        /// It is maintained for backward compatibility with previous versions of IRelatedEnd.
        /// </summary>
        /// <param name="entity"> Entity instance to add to the related end </param>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IRelatedEnd.Add(IEntityWithRelationships entity)
        {
            Check.NotNull(entity, "entity");

            ((IRelatedEnd)this).Add((object)entity);
        }

        /// <summary>
        /// Adds an entity to the related end.  If the owner is
        /// attached to a cache then the all the connected ends are
        /// added to the object cache and their corresponding relationships
        /// are also added to the ObjectStateManager. The RelatedEnd of the
        /// relationship is also fixed.
        /// </summary>
        /// <param name="entity"> Entity instance to add to the related end </param>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void IRelatedEnd.Add(object entity)
        {
            Check.NotNull(entity, "entity");

            Add(EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext));
        }

        internal void Add(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);

            if (_wrappedOwner.Entity != null)
            {
                Add(wrappedEntity, applyConstraints: true);
            }
            else
            {
                // The related end is in a disconnected state, so the related end is just a container
                // A common scenario for this is during WCF deserialization
                DisconnectedAdd(wrappedEntity);
            }
        }

        /// <summary>
        /// Removes an entity from the related end.  This method works in exactly the same way as Remove(object).
        /// It is maintained for backward compatibility with previous versions of IRelatedEnd.
        /// </summary>
        /// <param name="entity"> Entity instance to remove from the related end </param>
        /// <returns> Returns true if the entity was successfully removed, false if the entity was not part of the RelatedEnd. </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IRelatedEnd.Remove(IEntityWithRelationships entity)
        {
            Check.NotNull(entity, "entity");

            return ((IRelatedEnd)this).Remove((object)entity);
        }

        /// <summary>
        /// Removes an entity from the related end.  If owner is
        /// attached to a cache, marks relationship for deletion and if
        /// the relationship is composition also marks the entity for deletion.
        /// </summary>
        /// <param name="entity"> Entity instance to remove from the related end </param>
        /// <returns> Returns true if the entity was successfully removed, false if the entity was not part of the RelatedEnd. </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IRelatedEnd.Remove(object entity)
        {
            Check.NotNull(entity, "entity");

            DeferredLoad();
            return Remove(EntityWrapperFactory.WrapEntityUsingContext(entity, ObjectContext), false);
        }

        // Internal version that works on a wrapped entity and can be called from multiple
        // places where the public version is no longer appropriate.
        internal bool Remove(IEntityWrapper wrappedEntity, bool preserveForeignKey)
        {
            DebugCheck.NotNull(wrappedEntity);

            if (_wrappedOwner.Entity != null)
            {
                if (ContainsEntity(wrappedEntity))
                {
                    Remove(
                        wrappedEntity, /*fixup*/true, /*deleteEntity*/false, /*deleteOwner*/false, /*applyReferentialConstraints*/true,
                        preserveForeignKey);
                    return true;
                }
                // The entity is not related so return false
                return false;
            }
            else
            {
                // The related end is in a disconnected state, so the related end is just a container
                // A common scenario for this is during WCF deserialization
                return DisconnectedRemove(wrappedEntity);
            }
        }

        internal abstract void DisconnectedAdd(IEntityWrapper wrappedEntity);
        internal abstract bool DisconnectedRemove(IEntityWrapper wrappedEntity);

        internal void Add(IEntityWrapper wrappedEntity, bool applyConstraints)
        {
            // SQLBU: 508819 508813 508752
            // Detect as soon as possible if we are trying to re-add entities which are in Deleted state.
            // When one of the entity is in Deleted state, attempt would be made to re-add this entity
            // to the OSM which is not allowed.
            // NOTE: Current cleaning code (which uses cleanupOwnerEntity and cleanupPassedInEntity) 
            // works only if one of the entity is not attached to the context.
            // PERFORMANCE: following can be performed faster if ObjectStateManager provide method to
            // lookup only in dictionary with Deleted entities (because here we are interested only in Deleted entities)
            if (_context != null
                && !UsingNoTracking)
            {
                ValidateStateForAdd(_wrappedOwner);
                ValidateStateForAdd(wrappedEntity);
            }

            Add(
                wrappedEntity,
                applyConstraints: applyConstraints,
                addRelationshipAsUnchanged: false,
                relationshipAlreadyExists: false,
                allowModifyingOtherEndOfRelationship: true,
                forceForeignKeyChanges: true);
        }

        internal void CheckRelationEntitySet(EntitySet set)
        {
            DebugCheck.NotNull(set);
            Debug.Assert(
                _relationshipSet != null,
                "Should only be checking the RelationshipSet on an attached entity and it should always be non-null in that case");

            if ((((AssociationSet)_relationshipSet).AssociationSetEnds[_navigation.To] != null)
                &&
                (((AssociationSet)_relationshipSet).AssociationSetEnds[_navigation.To].EntitySet != set))
            {
                throw Error.RelatedEnd_EntitySetIsNotValidForRelationship(
                    set.EntityContainer.Name, set.Name, _navigation.To, _relationshipSet.EntityContainer.Name, _relationshipSet.Name);
            }
        }

        internal void ValidateStateForAdd(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            var entry = ObjectContext.ObjectStateManager.FindEntityEntry(wrappedEntity.Entity);
            if (entry != null
                && entry.State == EntityState.Deleted)
            {
                throw Error.RelatedEnd_UnableToAddRelationshipWithDeletedEntity();
            }
        }

        internal void Add(
            IEntityWrapper wrappedTarget,
            bool applyConstraints,
            bool addRelationshipAsUnchanged,
            bool relationshipAlreadyExists,
            bool allowModifyingOtherEndOfRelationship,
            // needed by ChangeRelationshipState - check multiplicity constraints instead of silently updating other end of relationship
            bool forceForeignKeyChanges)
        {
            DebugCheck.NotNull(wrappedTarget);
            // Do verification
            if (!VerifyEntityForAdd(wrappedTarget, relationshipAlreadyExists))
            {
                // Allow the same item to be "added" to a collection as a no-op operation
                return;
            }

            var key = wrappedTarget.EntityKey;
            if (key != null
                && ObjectContext != null)
            {
                CheckRelationEntitySet(key.GetEntitySet(ObjectContext.MetadataWorkspace));
            }

            var targetRelatedEnd = GetOtherEndOfRelationship(wrappedTarget);

            Debug.Assert(targetRelatedEnd.WrappedOwner == wrappedTarget);

            ValidateContextsAreCompatible(targetRelatedEnd);

            targetRelatedEnd.VerifyEntityForAdd(_wrappedOwner, relationshipAlreadyExists);

            // Do the actual add

            // Perform multiplicity constraints verification for the target related end before current related end is modified.
            // The "allowModifyingOtherEndOfRelationship" is used by ObjectStateManager.ChangeRelationshipState.
            targetRelatedEnd.VerifyMultiplicityConstraintsForAdd(!allowModifyingOtherEndOfRelationship);

            // Add the target entity to the source entity's collection or reference
            if (CheckIfNavigationPropertyContainsEntity(wrappedTarget))
            {
                AddToLocalCache(wrappedTarget, applyConstraints);
            }
            else
            {
                AddToCache(wrappedTarget, applyConstraints);
            }

            // Fix up the target end of the relationship by adding the source entity to the target entity's collection or reference
            // devnote: applyConstraints should be always false to enable scenarios like this:
            //             orderLine.Order = order1;
            //             order2.OrderLines.Add(orderLine); // orderLine.Order is changed to order2
            if (targetRelatedEnd.CheckIfNavigationPropertyContainsEntity(WrappedOwner))
            {
                // Example: IPOCO order, POCO customer with a bidirectional relationship
                //  customer.Orders.Add(order);
                //  order.Customer = customer <-- the Orders collection already contains "order" on fixup and this would add a duplicate
                targetRelatedEnd.AddToLocalCache(_wrappedOwner, applyConstraints: false);
            }
            else
            {
                targetRelatedEnd.AddToCache(_wrappedOwner, applyConstraints: false);
            }
            // delay event firing for targetRelatedEnd. once we fire the event, we should be at operation completed state

            SynchronizeContexts(targetRelatedEnd, relationshipAlreadyExists, addRelationshipAsUnchanged);

            // FK: update foreign key values on the dependent end.
            if (ObjectContext != null
                && IsForeignKey
                && !ObjectContext.ObjectStateManager.TransactionManager.IsGraphUpdate)
            {
                // Note that we use "forceForeignKeyChanges" below so that the FK properties will be set as modified
                // even if they don't actually change.
                if (!UpdateDependentEndForeignKey(targetRelatedEnd, forceForeignKeyChanges))
                {
                    targetRelatedEnd.UpdateDependentEndForeignKey(this, forceForeignKeyChanges);
                }
            }

            // else neither entity is associated with a context, so there is no state manager to update
            // fire the Association changed event, first on targetRelatedEnd then on this EC
            targetRelatedEnd.OnAssociationChanged(CollectionChangeAction.Add, _wrappedOwner.Entity);
            OnAssociationChanged(CollectionChangeAction.Add, wrappedTarget.Entity);
        }

        // <summary>Adds the current RelatedEnd object to the navigation property if compatible.</summary>
        // <param name="otherRelatedEnd">The other related end.</param>
        internal virtual void AddToNavigationPropertyIfCompatible(RelatedEnd otherRelatedEnd)
        {
            // By default, always add
            AddToNavigationProperty(otherRelatedEnd.WrappedOwner);
        }

        // <summary>Specifies whether the cached foreign key is conceptual null.</summary>
        // <returns>true if the cached foreign key is conceptual null; otherwise, false.</returns>
        internal virtual bool CachedForeignKeyIsConceptualNull()
        {
            // Only relevant for EntityReference
            return false;
        }

        // <summary>Updates the dependent end foreign keys.</summary>
        // <returns>The dependent end foreign keys.</returns>
        // <param name="targetRelatedEnd">The target related end.</param>
        // <param name="forceForeignKeyChanges">true to force foreign key changes; otherwise, false.</param>
        internal virtual bool UpdateDependentEndForeignKey(RelatedEnd targetRelatedEnd, bool forceForeignKeyChanges)
        {
            Debug.Assert(!IsDependentEndOfReferentialConstraint(false), "Dependent end cannot be a collection.");

            return false;
        }

        // <summary>Verifies the detached key matches.</summary>
        // <param name="entityKey">The entity keys.</param>
        internal virtual void VerifyDetachedKeyMatches(EntityKey entityKey)
        {
            // Only relevant to entity references
        }

        private void ValidateContextsAreCompatible(RelatedEnd targetRelatedEnd)
        {
            if (ReferenceEquals(ObjectContext, targetRelatedEnd.ObjectContext)
                && ObjectContext != null)
            {
                // Both entities are associated with the same non-null context

                // Make sure that they are either both tracked or both not tracked, or both don't have contexts
                if (UsingNoTracking != targetRelatedEnd.UsingNoTracking)
                {
                    throw Error.RelatedEnd_CannotCreateRelationshipBetweenTrackedAndNoTrackedEntities(
                        UsingNoTracking ? _navigation.From : _navigation.To);
                }
            }
            else if (ObjectContext != null
                     && targetRelatedEnd.ObjectContext != null)
            {
                // Both entities have a context
                if (UsingNoTracking && targetRelatedEnd.UsingNoTracking)
                {
                    // Both entities are NoTracking, but have different contexts
                    // Attach the owner's context to the target's RelationshipManager
                    // O-C mappings are 1:1, so this operation is allowed
                    targetRelatedEnd.WrappedOwner.ResetContext(
                        ObjectContext, GetTargetEntitySetFromRelationshipSet(), MergeOption.NoTracking);
                }
                else
                {
                    // Both entities are already tracked by different non-null contexts
                    throw Error.RelatedEnd_CannotCreateRelationshipEntitiesInDifferentContexts();
                }
            }
            else if ((_context == null || UsingNoTracking)
                     && (targetRelatedEnd.ObjectContext != null && !targetRelatedEnd.UsingNoTracking))
            {
                // Only the target has a context, so validate it is in a suitable state
                targetRelatedEnd.ValidateStateForAdd(targetRelatedEnd.WrappedOwner);
            }
        }

        private void SynchronizeContexts(RelatedEnd targetRelatedEnd, bool relationshipAlreadyExists, bool addRelationshipAsUnchanged)
        {
            // Ensure that both entities end up in the same context:
            // (1) If neither entity is attached to a context, we don't need to do anything else.
            // (2) If they are both in the same one, we need to make sure neither one was created with MergeOption.NoTracking,
            //     and if not, add a relationship entry if it doesn't already exist.
            // (3) If both entities are already in different contexts, fail.            
            // (4) Otherwise, only one entity is attached, and that is the context we will use.
            //     For the entity that is not attached, attach it to that context.    

            RelatedEnd attachedRelatedEnd = null; // the end of the relationship that is already attached to a context, if there is one.
            IEntityWrapper entityToAdd = null; // the entity to be added to attachedRelatedEnd
            var wrappedTarget = targetRelatedEnd.WrappedOwner;

            if (ReferenceEquals(ObjectContext, targetRelatedEnd.ObjectContext)
                && ObjectContext != null)
            {
                // Both entities are associated with the same non-null context

                // Make sure that a relationship entry exists between these two entities. It is possible that the entities could
                // have been added to the context independently of each other, so the relationship may not exist yet.
                if (!IsForeignKey
                    && !relationshipAlreadyExists
                    && !UsingNoTracking)
                {
                    // If this Add is triggered by setting the principle end of an unchanged/modified dependent end, then the relationship should be Unchanged
                    if (!ObjectContext.ObjectStateManager.TransactionManager.IsLocalPublicAPI
                        && WrappedOwner.EntityKey != null
                        && !WrappedOwner.EntityKey.IsTemporary
                        && IsDependentEndOfReferentialConstraint(false))
                    {
                        addRelationshipAsUnchanged = true;
                    }

                    AddRelationshipToObjectStateManager(wrappedTarget, addRelationshipAsUnchanged, /*doAttach*/false);
                }

                // The condition (IsAddTracking || IsAttachTracking || IsDetectChanges) excludes the case
                // when the method is called from materialization when we don't want to verify the navigation property.
                if (wrappedTarget.RequiresRelationshipChangeTracking
                    &&
                    (ObjectContext.ObjectStateManager.TransactionManager.IsAddTracking ||
                     ObjectContext.ObjectStateManager.TransactionManager.IsAttachTracking ||
                     ObjectContext.ObjectStateManager.TransactionManager.IsDetectChanges))
                {
                    AddToNavigationProperty(wrappedTarget);
                    targetRelatedEnd.AddToNavigationProperty(_wrappedOwner);
                }
            }
            else if (ObjectContext != null
                     || targetRelatedEnd.ObjectContext != null)
            {
                // Only one entity has a context, so figure out which one it is, and determine which entity we will be adding to it
                if (ObjectContext == null)
                {
                    attachedRelatedEnd = targetRelatedEnd;
                    entityToAdd = _wrappedOwner;
                }
                else
                {
                    attachedRelatedEnd = this;
                    entityToAdd = wrappedTarget;
                }

                if (!attachedRelatedEnd.UsingNoTracking)
                {
                    var transactionManager = attachedRelatedEnd.WrappedOwner.Context.ObjectStateManager.TransactionManager;
                    transactionManager.BeginAddTracking();

                    try
                    {
                        var doCleanup = true;

                        try
                        {
                            if (transactionManager.TrackProcessedEntities)
                            {
                                // The Entity could have been already wrapped by DetectChanges
                                if (!transactionManager.WrappedEntities.ContainsKey(entityToAdd.Entity))
                                {
                                    transactionManager.WrappedEntities.Add(entityToAdd.Entity, entityToAdd);
                                }
                                transactionManager.ProcessedEntities.Add(attachedRelatedEnd.WrappedOwner);
                            }

                            attachedRelatedEnd.AddGraphToObjectStateManager(
                                entityToAdd, relationshipAlreadyExists,
                                addRelationshipAsUnchanged, doAttach: false);

                            if (entityToAdd.RequiresRelationshipChangeTracking
                                && TargetAccessor.HasProperty)
                            {
                                Debug.Assert(
                                    CheckIfNavigationPropertyContainsEntity(wrappedTarget),
                                    "owner's navigation property doesn't contain the target entity as expected");
                                targetRelatedEnd.AddToNavigationProperty(_wrappedOwner);
                            }

                            doCleanup = false;
                        }
                        finally
                        {
                            if (doCleanup)
                            {
                                Debug.Assert(entityToAdd != null, "entityToAdd should be set if attachedRelatedEnd is set");

                                attachedRelatedEnd.WrappedOwner.Context.ObjectStateManager.DegradePromotedRelationships();

                                // Remove the source entity from the target related end
                                attachedRelatedEnd.FixupOtherEndOfRelationshipForRemove(entityToAdd, /*preserveForeignKey*/ false);

                                // Remove the target entity from the source related end
                                attachedRelatedEnd.RemoveFromCache(entityToAdd, /*resetIsLoaded*/ false, /*preserveForeignKey*/ false);

                                // Remove the graph that we just tried to add to the context
                                entityToAdd.RelationshipManager.NodeVisited = true;
                                RelationshipManager.RemoveRelatedEntitiesFromObjectStateManager(entityToAdd);
                                RemoveEntityFromObjectStateManager(entityToAdd);
                            }
                        }
                    }
                    finally
                    {
                        transactionManager.EndAddTracking();
                    }
                }
            }
        }

        private void AddGraphToObjectStateManager(
            IEntityWrapper wrappedEntity, bool relationshipAlreadyExists,
            bool addRelationshipAsUnchanged, bool doAttach)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(!UsingNoTracking, "Should not be attempting to add graphs to the state manager with NoTracking related ends");

            AddEntityToObjectStateManager(wrappedEntity, doAttach);
            if (!relationshipAlreadyExists
                && ObjectContext != null
                && wrappedEntity.Context != null)
            {
                if (!IsForeignKey)
                {
                    AddRelationshipToObjectStateManager(wrappedEntity, addRelationshipAsUnchanged, doAttach);
                }

                if (wrappedEntity.RequiresRelationshipChangeTracking
                    || WrappedOwner.RequiresRelationshipChangeTracking)
                {
                    UpdateSnapshotOfRelationships(wrappedEntity);
                    if (doAttach)
                    {
                        var entry = _context.ObjectStateManager.GetEntityEntry(wrappedEntity.Entity);
                        wrappedEntity.RelationshipManager.CheckReferentialConstraintProperties(entry);
                    }
                }
            }
            WalkObjectGraphToIncludeAllRelatedEntities(wrappedEntity, addRelationshipAsUnchanged, doAttach);
        }

        private void UpdateSnapshotOfRelationships(IEntityWrapper wrappedEntity)
        {
            var otherRelatedEnd = GetOtherEndOfRelationship(wrappedEntity);
            if (!otherRelatedEnd.ContainsEntity(WrappedOwner))
            {
                // Since we now align changes, we can allow the Add to remove the old value
                // Reference/FK violations are detected elsewhere
                otherRelatedEnd.AddToLocalCache(WrappedOwner, applyConstraints: false);
            }
        }

        internal void Remove(
            IEntityWrapper wrappedEntity, bool doFixup, bool deleteEntity, bool deleteOwner, bool applyReferentialConstraints,
            bool preserveForeignKey)
        {
            if (wrappedEntity.RequiresRelationshipChangeTracking
                && // Is it POCO?
                doFixup
                // Remove() is called for both ends of relationship, once with doFixup==true, once with doFixup==false. Verify only one time.
                && TargetAccessor.HasProperty) // Is there anything to verify?
            {
                var contains = CheckIfNavigationPropertyContainsEntity(wrappedEntity);

                if (!contains)
                {
                    var relatedEnd = GetOtherEndOfRelationship(wrappedEntity);
                    relatedEnd.RemoveFromNavigationProperty(WrappedOwner);
                }
            }

            if (!ContainsEntity(wrappedEntity))
            {
                return;
            }

            // There can be a case when symmetrical Remove() shall be performed because of Referential Constraints
            // Example:
            //   Relationship Client -> Order with Referential Constraint on in.
            //   When user calls (pseudo code) Order.Remove(Client), we perform Client.Remove(Order), 
            //   because removing relationship between Client and Order should cause cascade delete on the Order side.
            if (null != _context
                && doFixup
                &&
                applyReferentialConstraints
                && IsDependentEndOfReferentialConstraint(false)) // don't check the nullability of the "from" properties
            {
                // Remove _wrappedOwner from the related end with applying Referential Constraints
                var relatedEnd = GetOtherEndOfRelationship(wrappedEntity);
                relatedEnd.Remove(_wrappedOwner, doFixup, deleteEntity, deleteOwner, applyReferentialConstraints, preserveForeignKey);

                return;
            }

            //The following call will verify that the given entity is part of the collection or ref.
            var fireEvent = RemoveFromCache(wrappedEntity, false, preserveForeignKey);

            if (!UsingNoTracking
                && ObjectContext != null
                && !IsForeignKey)
            {
                MarkRelationshipAsDeletedInObjectStateManager(wrappedEntity, _wrappedOwner, _relationshipSet, _navigation);
            }

            if (doFixup)
            {
                FixupOtherEndOfRelationshipForRemove(wrappedEntity, preserveForeignKey);

                // For the "LocalPublicAPI" just remove the entity from the related end, don't trigger cascade delete
                if (_context == null
                    || !_context.ObjectStateManager.TransactionManager.IsLocalPublicAPI)
                {
                    //The related end "entity" cannot live without this side "owner". It should be deleted. Cascade this 
                    // effect to related entities of the "related" entity
                    // We skip this delete/detach if the entity is being reparented (TransactionManager.EntityBeingReparented)
                    // or if the reference is being nulled as part of fixup in a POCO proxy while setting the FK (InFKSetter).
                    if (null != _context
                        && (deleteEntity ||
                            (deleteOwner && CheckCascadeDeleteFlag(_fromEndMember)) ||
                            (applyReferentialConstraints && IsPrincipalEndOfReferentialConstraint()))
                        &&
                        !ReferenceEquals(wrappedEntity.Entity, _context.ObjectStateManager.TransactionManager.EntityBeingReparented)
                        && !ReferenceEquals(_context.ObjectStateManager.EntityInvokingFKSetter, wrappedEntity.Entity))
                    {
                        //Once related entity is deleted, all relationships involving related entity would be updated

                        // RemoveEntityFromRelatedEnds check for graph circularities to make sure
                        // it does not get into infinite loop
                        EnsureRelationshipNavigationAccessorsInitialized();
                        RemoveEntityFromRelatedEnds(wrappedEntity, _wrappedOwner, _navigation.Reverse);
                        MarkEntityAsDeletedInObjectStateManager(wrappedEntity);
                    }
                }
            }

            if (fireEvent)
            {
                OnAssociationChanged(CollectionChangeAction.Remove, wrappedEntity.Entity);
            }
        }

        // <summary>
        // Returns true if this Related end represents the dependent of a Referential Constraint
        // </summary>
        // <param name="checkIdentifying"> If true then the method will only return true if the Referential Constraint is identifying </param>
        internal bool IsDependentEndOfReferentialConstraint(bool checkIdentifying)
        {
            if (null != _relationMetadata)
            {
                // NOTE Referential constraints collection will usually contains 0 or 1 element,
                // so performance shouldn't be an issue here
                foreach (var constraint in ((AssociationType)RelationMetadata).ReferentialConstraints)
                {
                    if (constraint.ToRole == FromEndMember)
                    {
                        if (checkIdentifying)
                        {
                            var entityType = constraint.ToRole.GetEntityType();
                            var allPropertiesAreKeyProperties = CheckIfAllPropertiesAreKeyProperties(
                                entityType.KeyMemberNames, constraint.ToProperties);

                            return allPropertiesAreKeyProperties;
                        }
                        else
                        {
                            // Example: 
                            //    Client<C_ID> --- Order<O_ID, Client_ID>
                            //    RI Constraint: Principal/From <Client.C_ID>,  Dependent/To <Order.Client_ID>
                            // When current RelatedEnd is a CollectionOrReference in Order's relationships,
                            // constarint.ToRole == this._fromEndProperty == Order
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // <summary>
        // Check if current RelatedEnd is a Principal end of some Referential Constraint and if some of the "from" properties is not-nullable
        // </summary>
        internal bool IsPrincipalEndOfReferentialConstraint()
        {
            if (null != _relationMetadata)
            {
                // NOTE Referential constraints collection will usually contains 0 or 1 element,
                // so performance shouldn't be an issue here
                foreach (var constraint in ((AssociationType)_relationMetadata).ReferentialConstraints)
                {
                    if (constraint.FromRole == _fromEndMember)
                    {
                        var entityType = constraint.ToRole.GetEntityType();
                        var allPropertiesAreKeyProperties = CheckIfAllPropertiesAreKeyProperties(
                            entityType.KeyMemberNames, constraint.ToProperties);

                        // Example: 
                        //    Client<C_ID> --- Order<O_ID, Client_ID>
                        //    RI Constraint: Principal/From <Client.C_ID>,  Dependent/To <Order.Client_ID>
                        // When current RelatedEnd is a CollectionOrReference in Client's relationships,
                        // constarint.FromRole == this._fromEndProperty == Client
                        return allPropertiesAreKeyProperties;
                    }
                }
            }
            return false;
        }

        internal static bool CheckIfAllPropertiesAreKeyProperties(
            string[] keyMemberNames, ReadOnlyMetadataCollection<EdmProperty> toProperties)
        {
            // Check if some of the "to" properties is not a key property
            foreach (var property in toProperties)
            {
                var found = false;
                foreach (var keyPropertyName in keyMemberNames)
                {
                    if (keyPropertyName == property.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }

        // Add given entity and its relationship to ObjectStateManager. Walk graph to recursively
        // add all entities in the graph.
        // If doAttach==TRUE, the entities are attached directly as Unchanged without calling AcceptChanges()
        internal void IncludeEntity(IEntityWrapper wrappedEntity, bool addRelationshipAsUnchanged, bool doAttach)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(!UsingNoTracking, "Should not be trying to include entities in the state manager for NoTracking related ends");

            //check to see if entity is already added to the cache
            //search by object reference so that we will not find any entries with the same key but a different object instance
            // NOTE: if (cacheEntry.Entity == entity) then this part of the graph is skipped
            var cacheEntry = _context.ObjectStateManager.FindEntityEntry(wrappedEntity.Entity);
            Debug.Assert(
                cacheEntry == null || cacheEntry.Entity == wrappedEntity.Entity,
                "Expected to have looked up this state entry by reference, how did we get a different entity?");

            if (null != cacheEntry
                && cacheEntry.State == EntityState.Deleted)
            {
                throw Error.RelatedEnd_UnableToAddRelationshipWithDeletedEntity();
            }

            if (wrappedEntity.RequiresRelationshipChangeTracking
                || WrappedOwner.RequiresRelationshipChangeTracking)
            {
                // Verify relationship fixup before including rest of the graph.
                var otherRelatedEnd = GetOtherEndOfRelationship(wrappedEntity);

                Debug.Assert(otherRelatedEnd.WrappedOwner == wrappedEntity);

                // Validate the type is compatible before trying to get/set properties on it.
                // The following will throw if the type is not mapped.
                ObjectContext.GetTypeUsage(otherRelatedEnd.WrappedOwner.IdentityType);

                otherRelatedEnd.AddToNavigationPropertyIfCompatible(this);
            }

            if (null == cacheEntry)
            {
                // NOTE (Attach): if (null == entity.Key) then check must be performed whether entity really
                // doesn't exist in the context (by creating fake Key and calling FindObjectStateEntry(Key) )
                // This is done in the ObjectContext::AttachSingleObject().

                AddGraphToObjectStateManager(
                    wrappedEntity, /*relationshipAlreadyExists*/ false,
                    addRelationshipAsUnchanged, doAttach);
            }
            // There is a possibility that related entity is added to cache but relationship is not added.
            // Example: Suppose A and B are related. When walking the graph it is possible that 
            // node B was visited through some relationship other than A-B. 
            else if (null == FindRelationshipEntryInObjectStateManager(wrappedEntity))
            {
                VerifyDetachedKeyMatches(wrappedEntity.EntityKey);

                if (ObjectContext != null
                    && wrappedEntity.Context != null)
                {
                    if (!IsForeignKey)
                    {
                        if (cacheEntry.State
                            == EntityState.Added)
                        {
                            // In POCO, when the graph is partially attached and user is calling Attach on the detached entity
                            // and the entity in the context is in the Added state, the relationship has to created also in Added state.
                            AddRelationshipToObjectStateManager(wrappedEntity, addRelationshipAsUnchanged, false);
                        }
                        else
                        {
                            AddRelationshipToObjectStateManager(wrappedEntity, addRelationshipAsUnchanged, doAttach);
                        }
                    }

                    if (wrappedEntity.RequiresRelationshipChangeTracking
                        || WrappedOwner.RequiresRelationshipChangeTracking)
                    {
                        UpdateSnapshotOfRelationships(wrappedEntity);
                        if (doAttach && cacheEntry.State != EntityState.Added)
                        {
                            var entry = ObjectContext.ObjectStateManager.GetEntityEntry(wrappedEntity.Entity);
                            wrappedEntity.RelationshipManager.CheckReferentialConstraintProperties(entry);
                        }
                    }
                }
            }

            // else relationship is already there, nothing more to do
        }

        internal void MarkForeignKeyPropertiesModified()
        {
            Debug.Assert(IsForeignKey, "cannot update foreign key values if the relationship is not a FK");
            var constraint = ((AssociationType)RelationMetadata).ReferentialConstraints[0];
            Debug.Assert(constraint != null, "null constraint");

            var dependentEntry = WrappedOwner.ObjectStateEntry;
            Debug.Assert(dependentEntry != null, "Expected tracked entity.");

            // No need to try to mark properties as modified for added/deleted/detached entities.
            // Even if the entity is modified, the FK props may not be modified.
            if (dependentEntry.State == EntityState.Unchanged
                || dependentEntry.State == EntityState.Modified)
            {
                foreach (var dependentProp in constraint.ToProperties)
                {
                    dependentEntry.SetModifiedProperty(dependentProp.Name);
                }
            }
        }

        internal abstract bool CheckIfNavigationPropertyContainsEntity(IEntityWrapper wrapper);

        internal abstract void VerifyNavigationPropertyForAdd(IEntityWrapper wrapper);

        internal void AddToNavigationProperty(IEntityWrapper wrapper)
        {
            Debug.Assert(RelationshipNavigation != null, "null RelationshipNavigation");

            if (TargetAccessor.HasProperty
                && !CheckIfNavigationPropertyContainsEntity(wrapper))
            {
                Debug.Assert(wrapper.Context != null, "Expected context to be available.");
                // We keep track of the nav properties we have set during Add/Attach so that they
                // can be undone during rollback.
                var tm = wrapper.Context.ObjectStateManager.TransactionManager;
                if (tm.IsAddTracking
                    || tm.IsAttachTracking)
                {
                    wrapper.Context.ObjectStateManager.TrackPromotedRelationship(this, wrapper);
                }
                AddToObjectCache(wrapper);
            }
        }

        internal void RemoveFromNavigationProperty(IEntityWrapper wrapper)
        {
            Debug.Assert(RelationshipNavigation != null, "null RelationshipNavigation");

            if (TargetAccessor.HasProperty
                && CheckIfNavigationPropertyContainsEntity(wrapper))
            {
                RemoveFromObjectCache(wrapper);
            }
        }

        // Remove given entity and its relationship from ObjectStateManager.
        // Traversegraph to recursively remove all entities in the graph.
        internal void ExcludeEntity(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(!UsingNoTracking, "Should not try to exclude entities from the state manager for NoTracking related ends.");

            if (!_context.ObjectStateManager.TransactionManager.TrackProcessedEntities
                ||
                !(_context.ObjectStateManager.TransactionManager.IsAttachTracking
                  || _context.ObjectStateManager.TransactionManager.IsAddTracking)
                ||
                _context.ObjectStateManager.TransactionManager.ProcessedEntities.Contains(wrappedEntity))
            {
                //check to see if entity is already removed from the cache
                var cacheEntry = _context.ObjectStateManager.FindEntityEntry(wrappedEntity.Entity);

                if (null != cacheEntry
                    && cacheEntry.State != EntityState.Deleted
                    && !wrappedEntity.RelationshipManager.NodeVisited)
                {
                    wrappedEntity.RelationshipManager.NodeVisited = true;

                    RelationshipManager.RemoveRelatedEntitiesFromObjectStateManager(wrappedEntity);
                    if (!IsForeignKey)
                    {
                        RemoveRelationshipFromObjectStateManager(wrappedEntity, _wrappedOwner, _relationshipSet, _navigation);
                    }
                    RemoveEntityFromObjectStateManager(wrappedEntity);
                }
                // There is a possibility that related entity is removed from cache but relationship is not removed.
                // Example: Suppose A and B are related. When walking the graph it is possible that 
                // node B was visited through some relationship other than A-B. 
                else if (!IsForeignKey
                         && null != FindRelationshipEntryInObjectStateManager(wrappedEntity))
                {
                    RemoveRelationshipFromObjectStateManager(wrappedEntity, _wrappedOwner, _relationshipSet, _navigation);
                }
            }
        }

        internal RelationshipEntry FindRelationshipEntryInObjectStateManager(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(!UsingNoTracking, "Should not look for RelationshipEntry in ObjectStateManager for NoTracking cases.");
            var entityKey = wrappedEntity.EntityKey;
            var ownerKey = _wrappedOwner.EntityKey;
            return _context.ObjectStateManager.FindRelationship(
                _relationshipSet,
                new KeyValuePair<string, EntityKey>(_navigation.From, ownerKey),
                new KeyValuePair<string, EntityKey>(_navigation.To, entityKey));
        }

        internal void Clear(IEntityWrapper wrappedEntity, RelationshipNavigation navigation, bool doCascadeDelete)
        {
            ClearCollectionOrRef(wrappedEntity, navigation, doCascadeDelete);
        }

        // Check if related entities contain proper property values 
        // (entities with temporary keys are skipped)
        internal void CheckReferentialConstraintProperties(EntityEntry ownerEntry)
        {
            foreach (var constraint in ((AssociationType)RelationMetadata).ReferentialConstraints)
            {
                if (constraint.ToRole == FromEndMember)
                {
                    if (!CheckReferentialConstraintPrincipalProperty(ownerEntry, constraint))
                    {
                        throw new InvalidOperationException(constraint.BuildConstraintExceptionMessage());
                    }
                }
                else if (constraint.FromRole == FromEndMember)
                {
                    if (!CheckReferentialConstraintDependentProperty(ownerEntry, constraint))
                    {
                        throw new InvalidOperationException(constraint.BuildConstraintExceptionMessage());
                    }
                }
            }
        }

        internal virtual bool CheckReferentialConstraintPrincipalProperty(EntityEntry ownerEntry, ReferentialConstraint constraint)
        {
            Debug.Assert(false, "Expected the principal end to be an entity reference");

            return false;
        }

        internal virtual bool CheckReferentialConstraintDependentProperty(EntityEntry ownerEntry, ReferentialConstraint constraint)
        {
            if (!IsEmpty())
            {
                foreach (var wrappedRelatedEntity in GetWrappedEntities())
                {
                    var dependent = wrappedRelatedEntity.ObjectStateEntry;
                    if (dependent != null
                        &&
                        dependent.State != EntityState.Added
                        &&
                        dependent.State != EntityState.Deleted
                        &&
                        dependent.State != EntityState.Detached)
                    {
                        if (!VerifyRIConstraintsWithRelatedEntry(
                            constraint, dependent.GetCurrentEntityValue, ownerEntry.EntityKey))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal static bool VerifyRIConstraintsWithRelatedEntry(
            ReferentialConstraint constraint, Func<string, object> getDependentPropertyValue, EntityKey principalKey)
        {
            Debug.Assert(
                constraint.FromProperties.Count == constraint.ToProperties.Count,
                "RIC: Referential constraints From/To properties list have different size");

            // NOTE order of properties in collections (From/ToProperties) is important.
            for (var i = 0; i < constraint.FromProperties.Count; ++i)
            {
                var fromPropertyName = constraint.FromProperties[i].Name;
                var toPropertyName = constraint.ToProperties[i].Name;

                var currentValue = principalKey.FindValueByName(fromPropertyName);
                var expectedValue = getDependentPropertyValue(toPropertyName);

                Debug.Assert(currentValue != null, "currentValue is part of Key on an attached entity, it must not be null");

                if (!ByValueEqualityComparer.Default.Equals(currentValue, expectedValue))
                {
                    // RI Constraint violated
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IEnumerator" /> that iterates through the collection of related objects.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> that iterates through the collection of related objects.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            //CheckOwnerNull() is called in GetInternalEnumerable()
            DeferredLoad();
            return GetInternalEnumerable().GetEnumerator();
        }

        internal void RemoveAll()
        {
            //copy into list because changing collection member is not allowed during enumeration.
            // If possible avoid copying into list.
            List<IEntityWrapper> deletedEntities = null;

            var fireEvent = false;
            try
            {
                _suppressEvents = true;
                foreach (var wrappedEntity in GetWrappedEntities())
                {
                    if (null == deletedEntities)
                    {
                        deletedEntities = new List<IEntityWrapper>();
                    }
                    deletedEntities.Add(wrappedEntity);
                }

                if (fireEvent = (null != deletedEntities) && (deletedEntities.Count > 0))
                {
                    foreach (var wrappedEntity in deletedEntities)
                    {
                        Remove(
                            wrappedEntity, /*fixup*/true, /*deleteEntity*/false, /*deleteOwner*/true, /*applyReferentialConstraints*/true,
                            /*preserveForeignKey*/false);
                    }
                }
            }
            finally
            {
                _suppressEvents = false;
            }
            if (fireEvent)
            {
                OnAssociationChanged(CollectionChangeAction.Refresh, null);
            }
        }

        internal virtual void DetachAll(EntityState ownerEntityState)
        {
            //copy into list because changing collection member is not allowed during enumeration.
            // If possible avoid copying into list.
            var deletedEntities = new List<IEntityWrapper>();

            foreach (var wrappedEntity in GetWrappedEntities())
            {
                deletedEntities.Add(wrappedEntity);
            }

            var detachRelationship =
                ownerEntityState == EntityState.Added ||
                _fromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many;

            // every-fix up will fire with Remove action
            // every forward operation (removing from this relatedEnd) will fire with Refresh
            // do not merge the loops, handle the related ends separately (when the event is being fired, 
            // we should be in good state: for every entity deleted, related event should have been fired)
            foreach (var wrappedEntity in deletedEntities)
            {
                // future enhancement: it does not make sense to return in the half way, either remove this code or
                // move it to the right place
                if (!ContainsEntity(wrappedEntity))
                {
                    return;
                }

                if (detachRelationship)
                {
                    DetachRelationshipFromObjectStateManager(wrappedEntity, _wrappedOwner, _relationshipSet, _navigation);
                }
                var relatedEnd = GetOtherEndOfRelationship(wrappedEntity);
                relatedEnd.RemoveFromCache(_wrappedOwner, resetIsLoaded: true, preserveForeignKey: false);
                relatedEnd.OnAssociationChanged(CollectionChangeAction.Remove, _wrappedOwner.Entity);
            }

            foreach (var wrappedEntity in deletedEntities)
            {
                GetOtherEndOfRelationship(wrappedEntity);
                RemoveFromCache(wrappedEntity, resetIsLoaded: false, preserveForeignKey: false);
            }
            OnAssociationChanged(CollectionChangeAction.Refresh, null);

            Debug.Assert(IsEmpty(), "Collection or reference should be empty");
        }

        #region Add

        internal void AddToCache(IEntityWrapper wrappedEntity, bool applyConstraints)
        {
            AddToLocalCache(wrappedEntity, applyConstraints);
            AddToObjectCache(wrappedEntity);
        }

        internal abstract void AddToLocalCache(IEntityWrapper wrappedEntity, bool applyConstraints);
        internal abstract void AddToObjectCache(IEntityWrapper wrappedEntity);

        #endregion

        #region Remove

        internal bool RemoveFromCache(IEntityWrapper wrappedEntity, bool resetIsLoaded, bool preserveForeignKey)
        {
            var result = RemoveFromLocalCache(wrappedEntity, resetIsLoaded, preserveForeignKey);
            RemoveFromObjectCache(wrappedEntity);
            return result;
        }

        // Remove from the RelatedEnd
        internal abstract bool RemoveFromLocalCache(IEntityWrapper wrappedEntity, bool resetIsLoaded, bool preserveForeignKey);
        // Remove from the underlying POCO navigation property
        internal abstract bool RemoveFromObjectCache(IEntityWrapper wrappedEntity);

        #endregion

        // <returns> True if the verify succeeded, False if the Add should no-op </returns>
        internal virtual bool VerifyEntityForAdd(IEntityWrapper wrappedEntity, bool relationshipAlreadyExists)
        {
            DebugCheck.NotNull(wrappedEntity);

            if (relationshipAlreadyExists
                && ContainsEntity(wrappedEntity))
            {
                return false;
            }

            VerifyType(wrappedEntity);

            return true;
        }

        internal abstract void VerifyType(IEntityWrapper wrappedEntity);
        internal abstract bool CanSetEntityType(IEntityWrapper wrappedEntity);
        internal abstract void Include(bool addRelationshipAsUnchanged, bool doAttach);
        internal abstract void Exclude();
        internal abstract void ClearCollectionOrRef(IEntityWrapper wrappedEntity, RelationshipNavigation navigation, bool doCascadeDelete);
        internal abstract bool ContainsEntity(IEntityWrapper wrappedEntity);
        internal abstract IEnumerable GetInternalEnumerable();
        internal abstract IEnumerable<IEntityWrapper> GetWrappedEntities();

        internal abstract void RetrieveReferentialConstraintProperties(
            Dictionary<string, KeyValuePair<object, IntBox>> keyValues, HashSet<object> visited);

        internal abstract bool IsEmpty();
        internal abstract void OnRelatedEndClear();
        internal abstract void ClearWrappedValues();
        internal abstract void VerifyMultiplicityConstraintsForAdd(bool applyConstraints);

        internal virtual void OnAssociationChanged(CollectionChangeAction collectionChangeAction, object entity)
        {
            Debug.Assert(!(entity is IEntityWrapper), "Object is an IEntityWrapper instance instead of the raw entity.");
            if (!_suppressEvents)
            {
                if (_onAssociationChanged != null)
                {
                    _onAssociationChanged(this, (new CollectionChangeEventArgs(collectionChangeAction, entity)));
                }
            }
        }

        internal virtual void AddEntityToObjectStateManager(IEntityWrapper wrappedEntity, bool doAttach)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(_context != null, "Can't add to state manager if _context is null");
            Debug.Assert(!UsingNoTracking, "Should not add an Entity to ObjectStateManager for NoTracking cases.");

            var es = GetTargetEntitySetFromRelationshipSet();
            if (!doAttach)
            {
                _context.AddSingleObject(es, wrappedEntity, "entity");
            }
            else
            {
                _context.AttachSingleObject(wrappedEntity, es);
            }
        }

        internal EntitySet GetTargetEntitySetFromRelationshipSet()
        {
            EntitySet entitySet = null;
            var associationSet = (AssociationSet)_relationshipSet;
            Debug.Assert(associationSet != null, "(AssociationSet) cast failed");

            var associationEndMember = (AssociationEndMember)ToEndMember;
            Debug.Assert(associationEndMember != null, "(AssociationEndMember) cast failed");

            entitySet = associationSet.AssociationSetEnds[associationEndMember.Name].EntitySet;
            Debug.Assert(entitySet != null, "cannot find entitySet");
            return entitySet;
        }

        private RelationshipEntry AddRelationshipToObjectStateManager(
            IEntityWrapper wrappedEntity, bool addRelationshipAsUnchanged, bool doAttach)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(!UsingNoTracking, "Should not add Relationship to ObjectStateManager for NoTracking cases.");
            Debug.Assert(!IsForeignKey, "for IsForeignKey relationship ObjectStateEntries don't exist");
            Debug.Assert(_context != null && wrappedEntity.Context != null, "should be called only if both entities are attached");
            Debug.Assert(_context == wrappedEntity.Context, "both entities should be attached to the same context");

            var ownerKey = _wrappedOwner.EntityKey;
            var entityKey = wrappedEntity.EntityKey;
            if ((object)ownerKey == null)
            {
                throw Error.EntityKey_UnexpectedNull();
            }
            if ((object)entityKey == null)
            {
                throw Error.EntityKey_UnexpectedNull();
            }

            return ObjectContext.ObjectStateManager.AddRelation(
                new RelationshipWrapper(
                    (AssociationSet)_relationshipSet,
                    new KeyValuePair<string, EntityKey>(_navigation.From, ownerKey),
                    new KeyValuePair<string, EntityKey>(_navigation.To, entityKey)),
                // When Add method is called through Load API the relationship cache entries
                // needs to be added to ObjectStateManager in Unchanged state rather then Added state
                (addRelationshipAsUnchanged || doAttach) ? EntityState.Unchanged : EntityState.Added);
        }

        private static void WalkObjectGraphToIncludeAllRelatedEntities(
            IEntityWrapper wrappedEntity,
            bool addRelationshipAsUnchanged, bool doAttach)
        {
            DebugCheck.NotNull(wrappedEntity);
            foreach (var relatedEnd in wrappedEntity.RelationshipManager.Relationships)
            {
                relatedEnd.Include(addRelationshipAsUnchanged, doAttach);
            }
        }

        internal static void RemoveEntityFromObjectStateManager(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            EntityEntry entry;

            if (wrappedEntity.Context != null
                && wrappedEntity.Context.ObjectStateManager.TransactionManager.IsAttachTracking
                && wrappedEntity.Context.ObjectStateManager.TransactionManager.PromotedKeyEntries.TryGetValue(wrappedEntity.Entity, out entry))
            {
                // This is executed only in the cleanup code from ObjectContext.AttachTo()
                // If the entry was promoted in AttachTo(), it has to be degraded now instead of being deleted.
                entry.DegradeEntry();
            }
            else
            {
                entry = MarkEntityAsDeletedInObjectStateManager(wrappedEntity);
                if (entry != null
                    && entry.State != EntityState.Detached)
                {
                    entry.AcceptChanges();
                }
            }
        }

        private static void RemoveRelationshipFromObjectStateManager(
            IEntityWrapper wrappedEntity, IEntityWrapper wrappedOwner, RelationshipSet relationshipSet, RelationshipNavigation navigation)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(
                relationshipSet == null || !(relationshipSet.ElementType as AssociationType).IsForeignKey,
                "for IsForeignKey relationships ObjectStateEntries don't exist");

            var deletedEntry = MarkRelationshipAsDeletedInObjectStateManager(wrappedEntity, wrappedOwner, relationshipSet, navigation);
            if (deletedEntry != null
                && deletedEntry.State != EntityState.Detached)
            {
                deletedEntry.AcceptChanges();
            }
        }

        private void FixupOtherEndOfRelationshipForRemove(IEntityWrapper wrappedEntity, bool preserveForeignKey)
        {
            DebugCheck.NotNull(wrappedEntity);
            var relatedEnd = GetOtherEndOfRelationship(wrappedEntity);
            relatedEnd.Remove(
                _wrappedOwner, /*fixup*/false, /*deleteEntity*/false, /*deleteOwner*/false, /*applyReferentialConstraints*/false,
                preserveForeignKey);
            relatedEnd.RemoveFromNavigationProperty(_wrappedOwner);
        }

        private static EntityEntry MarkEntityAsDeletedInObjectStateManager(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            EntityEntry entry = null;
            if (wrappedEntity.Context != null)
            {
                entry = wrappedEntity.Context.ObjectStateManager.FindEntityEntry(wrappedEntity.Entity);

                if (entry != null)
                {
                    entry.Delete( /*doFixup*/false);
                }
            }
            return entry;
        }

        private static RelationshipEntry MarkRelationshipAsDeletedInObjectStateManager(
            IEntityWrapper wrappedEntity, IEntityWrapper wrappedOwner, RelationshipSet relationshipSet, RelationshipNavigation navigation)
        {
            DebugCheck.NotNull(wrappedEntity);
            Debug.Assert(
                relationshipSet == null || !(relationshipSet.ElementType as AssociationType).IsForeignKey,
                "for IsForeignKey relationships ObjectStateEntries don't exist");
            RelationshipEntry entry = null;
            if (wrappedOwner.Context != null
                && wrappedEntity.Context != null
                && relationshipSet != null)
            {
                var ownerKey = wrappedOwner.EntityKey;
                var entityKey = wrappedEntity.EntityKey;

                entry = wrappedEntity.Context.ObjectStateManager.DeleteRelationship(
                    relationshipSet,
                    new KeyValuePair<string, EntityKey>(navigation.From, ownerKey),
                    new KeyValuePair<string, EntityKey>(navigation.To, entityKey));
            }
            return entry;
        }

        private static void DetachRelationshipFromObjectStateManager(
            IEntityWrapper wrappedEntity, IEntityWrapper wrappedOwner, RelationshipSet relationshipSet, RelationshipNavigation navigation)
        {
            DebugCheck.NotNull(wrappedEntity);
            if (wrappedOwner.Context != null
                && wrappedEntity.Context != null
                && relationshipSet != null)
            {
                var ownerKey = wrappedOwner.EntityKey;
                var entityKey = wrappedEntity.EntityKey;
                var entry = wrappedEntity.Context.ObjectStateManager.FindRelationship(
                    relationshipSet,
                    new KeyValuePair<string, EntityKey>(navigation.From, ownerKey),
                    new KeyValuePair<string, EntityKey>(navigation.To, entityKey));
                if (entry != null)
                {
                    entry.DetachRelationshipEntry();
                }
            }
        }

        private static void RemoveEntityFromRelatedEnds(
            IEntityWrapper wrappedEntity1, IEntityWrapper wrappedEntity2, RelationshipNavigation navigation)
        {
            DebugCheck.NotNull(wrappedEntity1);
            DebugCheck.NotNull(wrappedEntity2);
            foreach (var relatedEnd in wrappedEntity1.RelationshipManager.Relationships)
            {
                var doCascadeDelete = false;
                //check for cascade delete flag
                doCascadeDelete = CheckCascadeDeleteFlag(relatedEnd.FromEndMember) || relatedEnd.IsPrincipalEndOfReferentialConstraint();
                //Remove the owner from the related end
                relatedEnd.Clear(wrappedEntity2, navigation, doCascadeDelete);
            }
        }

        private static bool CheckCascadeDeleteFlag(RelationshipEndMember relationEndProperty)
        {
            if (null != relationEndProperty)
            {
                return (relationEndProperty.DeleteBehavior == OperationAction.Cascade);
            }
            return false;
        }

        internal void AttachContext(ObjectContext context, MergeOption mergeOption)
        {
            if (!_wrappedOwner.InitializingProxyRelatedEnds)
            {
                var ownerKey = _wrappedOwner.EntityKey;
                if ((object)ownerKey == null)
                {
                    throw Error.EntityKey_UnexpectedNull();
                }
                var entitySet = ownerKey.GetEntitySet(context.MetadataWorkspace);

                AttachContext(context, entitySet, mergeOption);
            }
        }

        // <summary>
        // Set the context and load options so that Query can be constructed on demand.
        // </summary>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal void AttachContext(ObjectContext context, EntitySet entitySet, MergeOption mergeOption)
        {
            DebugCheck.NotNull(context);
            DebugCheck.NotNull(entitySet);

            EntityUtil.CheckArgumentMergeOption(mergeOption);

            _wrappedOwner.RelationshipManager.NodeVisited = false;
            // If the context is the same as what we already have, and the mergeOption is consistent with our UsingNoTracking setting, nothing more to do
            if (_context == context
                && (_usingNoTracking == (mergeOption == MergeOption.NoTracking)))
            {
                return;
            }

            var doCleanup = true;

            try
            {
                // if the source isn't null, clear it
                _sourceQuery = null;
                _context = context;
                _entityWrapperFactory = context.EntityWrapperFactory;
                _usingNoTracking = (mergeOption == MergeOption.NoTracking);

                EdmType relationshipType;
                RelationshipSet relationshipSet;
                FindRelationshipSet(_context, entitySet, out relationshipType, out relationshipSet);

                if (relationshipSet != null)
                {
                    _relationshipSet = relationshipSet;
                    _relationMetadata = (RelationshipType)relationshipType;
                }
                else
                {
                    foreach (var set in entitySet.EntityContainer.BaseEntitySets)
                    {
                        var associationset = set as AssociationSet;
                        if (associationset != null)
                        {
                            if (associationset.ElementType == relationshipType
                                &&
                                associationset.AssociationSetEnds[_navigation.From].EntitySet != entitySet
                                &&
                                associationset.AssociationSetEnds[_navigation.From].EntitySet.ElementType == entitySet.ElementType)
                            {
                                throw Error.RelatedEnd_EntitySetIsNotValidForRelationship(
                                    entitySet.EntityContainer.Name, entitySet.Name, _navigation.From, set.EntityContainer.Name, set.Name);
                            }
                        }
                    }
                    var relationshipName = _navigation.RelationshipName;
                    Debug.Assert(!String.IsNullOrEmpty(relationshipName), "empty relationshipName");
                    throw Error.Collections_NoRelationshipSetMatched(relationshipName);
                }

                //find relation end property
                var foundFromRelationEnd = false;
                var foundToRelationEnd = false;
                foreach (var relationEnd in ((AssociationType)_relationMetadata).AssociationEndMembers)
                //Only Association relationship is supported
                {
                    if (relationEnd.Name
                        == _navigation.From)
                    {
                        Debug.Assert(!foundFromRelationEnd, "More than one related end was found with the same role name.");

                        foundFromRelationEnd = true;
                        _fromEndMember = relationEnd;
                    }
                    if (relationEnd.Name
                        == _navigation.To)
                    {
                        Debug.Assert(!foundToRelationEnd, "More than one related end was found with the same role name.");

                        foundToRelationEnd = true;
                        _toEndMember = relationEnd;
                    }
                }
                if (!(foundFromRelationEnd && foundToRelationEnd))
                {
                    throw Error.RelatedEnd_RelatedEndNotFound();
                }

                ValidateDetachedEntityKey();

                doCleanup = false;
            }
            finally
            {
                if (doCleanup)
                {
                    // Uninitialize fields, so the cleanup code (for example in RelationshipWrapper.RemoveRelatedEntitiesFromObjectStateManager) 
                    // knows that this RelatedEnd was not properly Attached.
                    DetachContext();
                }
            }
        }

        // <summary>Validated the detached entity keys associated with the related end.</summary>
        internal virtual void ValidateDetachedEntityKey()
        {
            // Only relevant for EntityReference
        }

        internal void FindRelationshipSet(
            ObjectContext context, EntitySet entitySet, out EdmType relationshipType,
            out RelationshipSet relationshipSet)
        {
            if (_navigation.AssociationType == null || _navigation.AssociationType.Index < 0)
            {
                FindRelationshipSet(context, _navigation, entitySet, out relationshipType, out relationshipSet);
                return;
            }

            var metadataOptimization = context.MetadataWorkspace.MetadataOptimization;

            var associationType = metadataOptimization.GetCSpaceAssociationType(_navigation.AssociationType);

            relationshipType = associationType;
            relationshipSet = metadataOptimization.FindCSpaceAssociationSet(associationType, _navigation.From, entitySet);
        }

        internal static void FindRelationshipSet(ObjectContext context, RelationshipNavigation navigation,
            EntitySet entitySet, out EdmType relationshipType, out RelationshipSet relationshipSet)
        {
            // find the relationship set
            DebugCheck.NotNull(context.MetadataWorkspace);

            // find the TypeMetadata for the given relationship
            relationshipType = context.MetadataWorkspace.GetItem<EdmType>(navigation.RelationshipName, DataSpace.CSpace);
            if (relationshipType == null)
            {
                var relationshipName = navigation.RelationshipName;
                Debug.Assert(!String.IsNullOrEmpty(relationshipName), "empty relationshipName");
                throw Error.Collections_NoRelationshipSetMatched(relationshipName);
            }

            // find the RelationshipSet
            foreach (var entitySetBase in entitySet.AssociationSets)
            {
                if (entitySetBase.ElementType == relationshipType)
                {
                    if (entitySetBase.AssociationSetEnds[navigation.From].EntitySet == entitySet)
                    {
                        relationshipSet = entitySetBase;
                        return;
                    }
                }
            }
            relationshipSet = null;
        }

        // <summary>
        // Clear the source and context.
        // </summary>
        internal void DetachContext()
        {
            if (_context != null
                &&
                ObjectContext.ObjectStateManager.TransactionManager.IsAttachTracking
                &&
                ObjectContext.ObjectStateManager.TransactionManager.OriginalMergeOption == MergeOption.NoTracking)
            {
                _usingNoTracking = true;
                return;
            }

            _sourceQuery = null;
            _context = null;
            _relationshipSet = null;
            _fromEndMember = null;
            _toEndMember = null;
            _relationMetadata = null;

            // Detached entity should have IsLoaded property set to false
            _isLoaded = false;
        }

        internal RelatedEnd GetOtherEndOfRelationship(IEntityWrapper wrappedEntity)
        {
            DebugCheck.NotNull(wrappedEntity);
            EnsureRelationshipNavigationAccessorsInitialized();
            return wrappedEntity.RelationshipManager.GetRelatedEnd(_navigation.Reverse, _relationshipFixer);
        }

        // We have to allow a default constructor for serialization, so we need to make sure that the only
        // thing you can do with a null owner is get/set the EntityReference.EntityKey property. All other
        // operations are invalid. This needs to be used on all public methods in this class and EntityReference
        // but not in EntityCollection because EntityCollection does not have a default constructor.
        // It is not possible to get an EntityReference with a null Owner into the RelationshipManager, and there 
        // is no way to access EntityReference without creating one using the default constructor or going through
        // the RelationshipManager, so we don't need to check this in internal or private methods.
        internal virtual void CheckOwnerNull()
        {
            if (_wrappedOwner.Entity == null)
            {
                throw Error.RelatedEnd_OwnerIsNull();
            }
        }

        // This method is intended to be used to support the public API InitializeRelatedReference, where we have to take an existing EntityReference
        // and set up the appropriate fields as shown below, instead of creating a new EntityReference and setting these fields in the constructor.   
        // This is also used by the constructor -- if we add something that needs to be set at construction time, it probably needs to be set for InitializeRelatedReference as well.
        internal void InitializeRelatedEnd(
            IEntityWrapper wrappedOwner, RelationshipNavigation navigation, IRelationshipFixer relationshipFixer)
        {
            SetWrappedOwner(wrappedOwner);
            _navigation = navigation;
            _relationshipFixer = relationshipFixer;
        }

        internal void SetWrappedOwner(IEntityWrapper wrappedOwner)
        {
            _wrappedOwner = wrappedOwner != null ? wrappedOwner : NullEntityWrapper.NullWrapper;
#pragma warning disable 612 // Disable "obsolete" warning for the _owner field. Used for backwards compatibility.
            _owner = wrappedOwner.Entity as IEntityWithRelationships;
#pragma warning restore 612
        }

        internal static bool IsValidEntityKeyType(EntityKey entityKey)
        {
            return !(entityKey.IsTemporary ||
                     ReferenceEquals(EntityKey.EntityNotValidKey, entityKey) ||
                     ReferenceEquals(EntityKey.NoEntitySetKey, entityKey));
        }

        // This method is required to maintain compatibility with the v1 binary serialization format. 
        // In particular, it recreates a entity wrapper from the serialized owner.
        // Note that this is only expected to work for non-POCO entities, since serialization of POCO
        // entities will not result in serialization of the RelationshipManager or its related objects.
        /// <summary>
        /// Used internally to deserialize entity objects along with the
        /// <see
        ///     cref="T:System.Data.Entity.Core.Objects.DataClasses.RelationshipManager" />
        /// instances.
        /// </summary>
        /// <param name="context">The serialized stream.</param>
        [OnDeserialized]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA2238:ImplementSerializationMethodsCorrectly")]
        public void OnDeserialized(StreamingContext context)
        {
#pragma warning disable 612 // Disable "obsolete" warning for the _owner field. Used for backwards compatibility.
            _wrappedOwner = EntityWrapperFactory.WrapEntityUsingContext(_owner, ObjectContext);
#pragma warning restore 612
        }

        [NonSerialized]
        private NavigationProperty navigationPropertyCache;

        internal NavigationProperty NavigationProperty
        {
            get
            {
                if (navigationPropertyCache == null
                    && _wrappedOwner.Context != null
                    && TargetAccessor.HasProperty)
                {
                    var navigationPropertyName = TargetAccessor.PropertyName;

                    var entityType = _wrappedOwner.Context.MetadataWorkspace.GetItem<EntityType>(
                        _wrappedOwner.IdentityType.FullNameWithNesting(), DataSpace.OSpace);
                    NavigationProperty member;
                    if (!entityType.NavigationProperties.TryGetValue(navigationPropertyName, false, out member))
                    {
                        throw Error.RelationshipManager_NavigationPropertyNotFound(navigationPropertyName);
                    }
                    // Avoid metadata lookups by caching the navigation property locally
                    navigationPropertyCache = member;
                }
                return navigationPropertyCache;
            }
        }

        #region POCO Navigation Property Accessors

        internal NavigationPropertyAccessor TargetAccessor
        {
            get
            {
                if (_wrappedOwner.Entity != null)
                {
                    EnsureRelationshipNavigationAccessorsInitialized();
                    return RelationshipNavigation.ToPropertyAccessor;
                }
                else
                {
                    // Disconnected RelatedEnds have no POCO navigation properties
                    return NavigationPropertyAccessor.NoNavigationProperty;
                }
            }
        }

        // If the RelationshipNavigation has not been fully initialized, it means this RelatedEnd was created without metadata
        // This can occur in serialization scenarios
        // Try to look up the metadata in all metadata repositories that are available and populate it
        // This must be called before accessing any of the Accessor properties on the RelationshipNavigation
        private void EnsureRelationshipNavigationAccessorsInitialized()
        {
            Debug.Assert(_navigation != null, "Null RelationshipNavigation");
            Debug.Assert(_wrappedOwner.Entity != null, "Must be connected to lookup metadata");
            if (!RelationshipNavigation.IsInitialized)
            {
                NavigationPropertyAccessor sourceAccessor = null;
                NavigationPropertyAccessor targetAccessor = null;

                var relationshipName = _navigation.RelationshipName;
                var sourceRoleName = _navigation.From;
                var targetRoleName = _navigation.To;

                var associationType = RelationMetadata as AssociationType
                                      ?? _wrappedOwner.RelationshipManager.GetRelationshipType(relationshipName);

                AssociationEndMember sourceEnd;
                if (associationType.AssociationEndMembers.TryGetValue(sourceRoleName, false, out sourceEnd))
                {
                    var sourceEntityType = MetadataHelper.GetEntityTypeForEnd(sourceEnd);
                    targetAccessor = MetadataHelper.GetNavigationPropertyAccessor(
                        sourceEntityType, relationshipName, sourceRoleName, targetRoleName);
                }

                AssociationEndMember targetEnd;
                if (associationType.AssociationEndMembers.TryGetValue(targetRoleName, false, out targetEnd))
                {
                    var targetEntityType = MetadataHelper.GetEntityTypeForEnd(targetEnd);
                    sourceAccessor = MetadataHelper.GetNavigationPropertyAccessor(
                        targetEntityType, relationshipName, targetRoleName, sourceRoleName);
                }

                if (sourceAccessor == null
                    || targetAccessor == null)
                {
                    throw RelationshipManager.UnableToGetMetadata(WrappedOwner, relationshipName);
                }

                RelationshipNavigation.InitializeAccessors(sourceAccessor, targetAccessor);
            }
        }

        #endregion

        internal bool DisableLazyLoading()
        {
            if (_context == null)
            {
                return false;
            }

            var loadingState = _context.ContextOptions.LazyLoadingEnabled;
            _context.ContextOptions.LazyLoadingEnabled = false;

            return loadingState;
        }

        internal void ResetLazyLoading(bool state)
        {
            if (_context != null)
            {
                _context.ContextOptions.LazyLoadingEnabled = state;
            }
        }
    }
}
