// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    // <summary>
    // Defines and injects behavior into proxy class Type definitions
    // to allow navigation properties to lazily load their references or collection elements.
    // </summary>
    internal sealed class LazyLoadBehavior
    {
        // <summary>
        // Return an expression tree that represents the actions required to load the related end
        // associated with the intercepted proxy member.
        // </summary>
        // <param name="member"> EdmMember that specifies the member to be intercepted. </param>
        // <param name="getEntityWrapperDelegate"> The Func that retrieves the wrapper from a proxy </param>
        // <returns> Expression tree that encapsulates lazy loading behavior for the supplied member, or null if the expression tree could not be constructed. </returns>
        internal static Func<TProxy, TItem, bool> GetInterceptorDelegate<TProxy, TItem>(
            EdmMember member, Func<object, object> getEntityWrapperDelegate)
            where TProxy : class
            where TItem : class
        {
            Func<TProxy, TItem, bool> interceptorDelegate = (proxy, item) => true;

            Debug.Assert(member.BuiltInTypeKind == BuiltInTypeKind.NavigationProperty, "member should represent a navigation property");
            if (member.BuiltInTypeKind
                == BuiltInTypeKind.NavigationProperty)
            {
                var navProperty = (NavigationProperty)member;
                var multiplicity = navProperty.ToEndMember.RelationshipMultiplicity;

                // Given the proxy and item parameters, construct one of the following expressions:
                //
                // For collections:
                //  LazyLoadBehavior.LoadCollection(collection, "relationshipName", "targetRoleName", proxy._entityWrapperField)
                //
                // For entity references:
                //  LazyLoadBehavior.LoadReference(item, "relationshipName", "targetRoleName", proxy._entityWrapperField)
                //
                // Both of these expressions return an object of the same type as the first parameter to LoadXYZ method.
                // In many cases, this will be the first parameter.

                if (multiplicity == RelationshipMultiplicity.Many)
                {
                    interceptorDelegate = (proxy, item) => LoadProperty(
                        item,
                        navProperty.RelationshipType.Identity,
                        navProperty.ToEndMember.Identity,
                        false,
                        getEntityWrapperDelegate(proxy));
                }
                else
                {
                    interceptorDelegate = (proxy, item) => LoadProperty(
                        item,
                        navProperty.RelationshipType.Identity,
                        navProperty.ToEndMember.Identity,
                        true,
                        getEntityWrapperDelegate(proxy));
                }
            }

            return interceptorDelegate;
        }

        // <summary>
        // Determine if the specified member is compatible with lazy loading.
        // </summary>
        // <param name="ospaceEntityType"> OSpace EntityType representing a type that may be proxied. </param>
        // <param name="member">
        // Member of the <paramref name="ospaceEntityType" /> to be examined.
        // </param>
        // <returns> True if the member is compatible with lazy loading; otherwise false. </returns>
        // <remarks>
        // To be compatible with lazy loading,
        // a member must meet the criteria for being able to be proxied (defined elsewhere),
        // and must be a navigation property.
        // In addition, for relationships with a multiplicity of Many,
        // the property type must be an implementation of ICollection&lt;T&gt;.
        // </remarks>
        internal static bool IsLazyLoadCandidate(EntityType ospaceEntityType, EdmMember member)
        {
            Debug.Assert(ospaceEntityType.DataSpace == DataSpace.OSpace, "ospaceEntityType.DataSpace must be OSpace");

            var isCandidate = false;

            if (member.BuiltInTypeKind
                == BuiltInTypeKind.NavigationProperty)
            {
                var navProperty = (NavigationProperty)member;
                var multiplicity = navProperty.ToEndMember.RelationshipMultiplicity;

                var propertyInfo = ospaceEntityType.ClrType.GetTopProperty(member.Name);
                Debug.Assert(propertyInfo != null, "Should have found lazy loading property");
                var propertyValueType = propertyInfo.PropertyType;

                if (multiplicity == RelationshipMultiplicity.Many)
                {
                    isCandidate = propertyValueType.TryGetElementType(typeof(ICollection<>)) != null;
                }
                else if (multiplicity == RelationshipMultiplicity.One
                         || multiplicity == RelationshipMultiplicity.ZeroOrOne)
                {
                    // This is an EntityReference property.
                    isCandidate = true;
                }
            }

            return isCandidate;
        }

        // <summary>
        // Method called by proxy interceptor delegate to provide lazy loading behavior for navigation properties.
        // </summary>
        // <typeparam name="TItem"> property type </typeparam>
        // <param name="propertyValue"> The property value whose associated relationship is to be loaded. </param>
        // <param name="relationshipName"> String name of the relationship. </param>
        // <param name="targetRoleName">
        // String name of the related end to be loaded for the relationship specified by
        // <paramref
        //     name="relationshipName" />
        // .
        // </param>
        // <param name="wrapperObject"> Entity wrapper object used to retrieve RelationshipManager for the proxied entity. </param>
        // <returns> True if the value instance was mutated and can be returned False if the class should refetch the value because the instance has changed </returns>
        private static bool LoadProperty<TItem>(
            TItem propertyValue, string relationshipName, string targetRoleName, bool mustBeNull, object wrapperObject) where TItem : class
        {
            // Only attempt to load collection if:
            //
            // 1. Collection is non-null.
            // 2. ObjectContext.ContextOptions.LazyLoadingEnabled is true
            // 3. A non-null RelationshipManager can be retrieved (this is asserted).
            // 4. The EntityCollection is not already loaded.

            var wrapper = (IEntityWrapper)wrapperObject; // We want an exception if the cast fails.

            if (wrapper != null
                && wrapper.Context != null)
            {
                var relationshipManager = wrapper.RelationshipManager;
                Debug.Assert(relationshipManager != null, "relationshipManager should be non-null");
                if (relationshipManager != null
                    && (!mustBeNull || propertyValue == null))
                {
                    var relatedEnd = relationshipManager.GetRelatedEndInternal(relationshipName, targetRoleName);
                    relatedEnd.DeferredLoad();
                }
            }

            return propertyValue != null;
        }
    }
}
