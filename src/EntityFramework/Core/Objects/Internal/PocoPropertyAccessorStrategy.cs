// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;

    // <summary>
    // Implementation of the property accessor strategy that gets and sets values on POCO entities.  That is,
    // entities that do not implement IEntityWithRelationships.
    // </summary>
    internal sealed class PocoPropertyAccessorStrategy : IPropertyAccessorStrategy
    {
        internal static readonly MethodInfo AddToCollectionGeneric 
            = typeof(PocoPropertyAccessorStrategy).GetOnlyDeclaredMethod("AddToCollection");

        internal static readonly MethodInfo RemoveFromCollectionGeneric 
            = typeof(PocoPropertyAccessorStrategy).GetOnlyDeclaredMethod("RemoveFromCollection");

        private readonly object _entity;

        // <summary>
        // Constructs a strategy object to work with the given entity.
        // </summary>
        // <param name="entity"> The entity to use </param>
        public PocoPropertyAccessorStrategy(object entity)
        {
            _entity = entity;
        }

        #region Navigation Property Accessors

        #region GetNavigationPropertyValue

        // See IPropertyAccessorStrategy
        public object GetNavigationPropertyValue(RelatedEnd relatedEnd)
        {
            object navPropValue = null;
            if (relatedEnd != null)
            {
                if (relatedEnd.TargetAccessor.ValueGetter == null)
                {
                    var type = GetDeclaringType(relatedEnd);
                    var propertyInfo = type.GetTopProperty(relatedEnd.TargetAccessor.PropertyName);
                    if (propertyInfo == null)
                    {
                        throw new EntityException(
                            Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(relatedEnd.TargetAccessor.PropertyName, type.FullName));
                    }
                    var factory = new EntityProxyFactory();
                    relatedEnd.TargetAccessor.ValueGetter = factory.CreateBaseGetter(propertyInfo.DeclaringType, propertyInfo);
                }
                var loadingState = relatedEnd.DisableLazyLoading();
                try
                {
                    navPropValue = relatedEnd.TargetAccessor.ValueGetter(_entity);
                }
                catch (Exception ex)
                {
                    throw new EntityException(
                        Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(
                            relatedEnd.TargetAccessor.PropertyName, _entity.GetType().FullName), ex);
                }
                finally
                {
                    relatedEnd.ResetLazyLoading(loadingState);
                }
            }
            return navPropValue;
        }

        #endregion

        #region SetNavigationPropertyValue

        // See IPropertyAccessorStrategy
        public void SetNavigationPropertyValue(RelatedEnd relatedEnd, object value)
        {
            if (relatedEnd != null)
            {
                if (relatedEnd.TargetAccessor.ValueSetter == null)
                {
                    var type = GetDeclaringType(relatedEnd);
                    var propertyInfo = type.GetTopProperty(relatedEnd.TargetAccessor.PropertyName);
                    if (propertyInfo == null)
                    {
                        throw new EntityException(
                            Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(relatedEnd.TargetAccessor.PropertyName, type.FullName));
                    }
                    var factory = new EntityProxyFactory();
                    relatedEnd.TargetAccessor.ValueSetter = factory.CreateBaseSetter(propertyInfo.DeclaringType, propertyInfo);
                }
                try
                {
                    relatedEnd.TargetAccessor.ValueSetter(_entity, value);
                }
                catch (Exception ex)
                {
                    throw new EntityException(
                        Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(
                            relatedEnd.TargetAccessor.PropertyName, _entity.GetType().FullName), ex);
                }
            }
        }

        private static Type GetDeclaringType(RelatedEnd relatedEnd)
        {
            if (relatedEnd.NavigationProperty != null)
            {
                var declaringEntityType = (EntityType)relatedEnd.NavigationProperty.DeclaringType;
                var mapping = Util.GetObjectMapping(declaringEntityType, relatedEnd.WrappedOwner.Context.MetadataWorkspace);
                return mapping.ClrType.ClrType;
            }
            else
            {
                return relatedEnd.WrappedOwner.IdentityType;
            }
        }

        private static Type GetNavigationPropertyType(Type entityType, string propertyName)
        {
            Type navPropType;
            var property = entityType.GetTopProperty(propertyName);
            if (property != null)
            {
                navPropType = property.PropertyType;
            }
            else
            {
                var field = entityType.GetField(propertyName);
                if (field != null)
                {
                    navPropType = field.FieldType;
                }
                else
                {
                    throw new EntityException(Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(propertyName, entityType.FullName));
                }
            }
            return navPropType;
        }

        #endregion

        #endregion

        #region Collection Navigation Property Accessors

        #region CollectionAdd

        // See IPropertyAccessorStrategy
        public void CollectionAdd(RelatedEnd relatedEnd, object value)
        {
            var entity = _entity;
            try
            {
                var collection = GetNavigationPropertyValue(relatedEnd);
                if (collection == null)
                {
                    collection = CollectionCreate(relatedEnd);
                    SetNavigationPropertyValue(relatedEnd, collection);
                }
                Debug.Assert(collection != null, "Collection is null");

                // do not call Add if the collection is a RelatedEnd instance
                if (ReferenceEquals(collection, relatedEnd))
                {
                    return;
                }

                if (relatedEnd.TargetAccessor.CollectionAdd == null)
                {
                    relatedEnd.TargetAccessor.CollectionAdd = CreateCollectionAddFunction(
                        GetDeclaringType(relatedEnd), relatedEnd.TargetAccessor.PropertyName);
                }

                relatedEnd.TargetAccessor.CollectionAdd(collection, value);
            }
            catch (Exception ex)
            {
                throw new EntityException(
                    Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(
                        relatedEnd.TargetAccessor.PropertyName, entity.GetType().FullName),
                    ex);
            }
        }

        // Helper method to create delegate with property setter
        private static Action<object, object> CreateCollectionAddFunction(Type type, string propertyName)
        {
            var navPropType = GetNavigationPropertyType(type, propertyName);
            var elementType = EntityUtil.GetCollectionElementType(navPropType);

            var addToCollection = AddToCollectionGeneric.MakeGenericMethod(elementType);
            return (Action<object, object>)addToCollection.Invoke(null, null);
        }

        private static Action<object, object> AddToCollection<T>()
        {
            return (collectionArg, item) =>
                {
                    var collection = (ICollection<T>)collectionArg;
                    var array = collection as Array;
                    if (array != null
                        && array.IsFixedSize)
                    {
                        throw new InvalidOperationException(Strings.RelatedEnd_CannotAddToFixedSizeArray(array.GetType()));
                    }
                    collection.Add((T)item);
                };
        }

        #endregion

        #region CollectionRemove

        // See IPropertyAccessorStrategy
        public bool CollectionRemove(RelatedEnd relatedEnd, object value)
        {
            var entity = _entity;
            try
            {
                var collection = GetNavigationPropertyValue(relatedEnd);
                if (collection != null)
                {
                    // do not call Add if the collection is a RelatedEnd instance
                    if (ReferenceEquals(collection, relatedEnd))
                    {
                        return true;
                    }

                    if (relatedEnd.TargetAccessor.CollectionRemove == null)
                    {
                        relatedEnd.TargetAccessor.CollectionRemove = CreateCollectionRemoveFunction(
                            GetDeclaringType(relatedEnd), relatedEnd.TargetAccessor.PropertyName);
                    }

                    return relatedEnd.TargetAccessor.CollectionRemove(collection, value);
                }
            }
            catch (Exception ex)
            {
                throw new EntityException(
                    Strings.PocoEntityWrapper_UnableToSetFieldOrProperty(relatedEnd.TargetAccessor.PropertyName, entity.GetType().FullName),
                    ex);
            }
            return false;
        }

        // Helper method to create delegate with property setter
        private static Func<object, object, bool> CreateCollectionRemoveFunction(Type type, string propertyName)
        {
            var navPropType = GetNavigationPropertyType(type, propertyName);
            var elementType = EntityUtil.GetCollectionElementType(navPropType);

            var removeFromCollection = RemoveFromCollectionGeneric.MakeGenericMethod(elementType);
            return (Func<object, object, bool>)removeFromCollection.Invoke(null, null);
        }

        private static Func<object, object, bool> RemoveFromCollection<T>()
        {
            return (collectionArg, item) =>
                {
                    var collection = (ICollection<T>)collectionArg;
                    var array = collection as Array;
                    if (array != null
                        && array.IsFixedSize)
                    {
                        throw new InvalidOperationException(Strings.RelatedEnd_CannotRemoveFromFixedSizeArray(array.GetType()));
                    }
                    return collection.Remove((T)item);
                };
        }

        #endregion

        #region CollectionCreate

        // See IPropertyAccessorStrategy
        public object CollectionCreate(RelatedEnd relatedEnd)
        {
            if (_entity is IEntityWithRelationships)
            {
                return relatedEnd;
            }
            else
            {
                if (relatedEnd.TargetAccessor.CollectionCreate == null)
                {
                    var entityType = GetDeclaringType(relatedEnd);
                    var propName = relatedEnd.TargetAccessor.PropertyName;
                    var navPropType = GetNavigationPropertyType(entityType, propName);
                    relatedEnd.TargetAccessor.CollectionCreate = CreateCollectionCreateDelegate(navPropType, propName);
                }
                return relatedEnd.TargetAccessor.CollectionCreate();
            }
        }

        // <summary>
        // We only get here if a navigation property getter returns null.  In this case, we try to set the
        // navigation property to some collection that will work.
        // </summary>
        private static Func<object> CreateCollectionCreateDelegate(Type navigationPropertyType, string propName)
        {
            var typeToInstantiate = EntityUtil.DetermineCollectionType(navigationPropertyType);

            if (typeToInstantiate == null)
            {
                throw new EntityException(
                    Strings.PocoEntityWrapper_UnableToMaterializeArbitaryNavPropType(propName, navigationPropertyType));
            }

            return Expression.Lambda<Func<object>>(
                DelegateFactory.GetNewExpressionForCollectionType(typeToInstantiate)).Compile();
        }

        #endregion

        #endregion
    }
}
