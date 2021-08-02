// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.Update.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;

    // <summary>
    // Encapsulates information about ends of an association set needed to correctly
    // interpret updates.
    // </summary>
    internal sealed class AssociationSetMetadata
    {
        // <summary>
        // Gets association ends that must be modified if the association
        // is changed (e.g. the mapping of the association is conditioned
        // on some property of the end)
        // </summary>
        internal readonly Set<AssociationEndMember> RequiredEnds;

        // <summary>
        // Gets association ends that may be implicitly modified as a result
        // of changes to the association (e.g. collocated entity with server
        // generated value)
        // </summary>
        internal readonly Set<AssociationEndMember> OptionalEnds;

        // <summary>
        // Gets association ends whose values may influence the association
        // (e.g. where there is a ReferentialIntegrity or "foreign key" constraint)
        // </summary>
        internal readonly Set<AssociationEndMember> IncludedValueEnds;

        // <summary>
        // true iff. there are interesting ends for this association set.
        // </summary>
        internal bool HasEnds
        {
            get { return 0 < RequiredEnds.Count || 0 < OptionalEnds.Count || 0 < IncludedValueEnds.Count; }
        }

        // <summary>
        // Initialize Metadata for an AssociationSet
        // </summary>
        internal AssociationSetMetadata(Set<EntitySet> affectedTables, AssociationSet associationSet, MetadataWorkspace workspace)
        {
            // If there is only 1 table, there can be no ambiguity about the "destination" of a relationship, so such
            // sets are not typically required.
            var isRequired = 1 < affectedTables.Count;

            // determine the ends of the relationship
            var ends = associationSet.AssociationSetEnds;

            // find collocated entities
            foreach (var table in affectedTables)
            {
                // Find extents influencing the table
                var influencingExtents = MetadataHelper.GetInfluencingEntitySetsForTable(table, workspace);

                foreach (var influencingExtent in influencingExtents)
                {
                    foreach (var end in ends)
                    {
                        // If the extent is an end of the relationship and we haven't already added it to the
                        // required set...
                        if (end.EntitySet.EdmEquals(influencingExtent))
                        {
                            if (isRequired)
                            {
                                AddEnd(ref RequiredEnds, end.CorrespondingAssociationEndMember);
                            }
                            else if (null == RequiredEnds
                                     || !RequiredEnds.Contains(end.CorrespondingAssociationEndMember))
                            {
                                AddEnd(ref OptionalEnds, end.CorrespondingAssociationEndMember);
                            }
                        }
                    }
                }
            }

            // fix Required and Optional sets
            FixSet(ref RequiredEnds);
            FixSet(ref OptionalEnds);

            // for associations with referential constraints, the principal end is always interesting
            // since its key values may take precedence over the key values of the dependent end
            foreach (var constraint in associationSet.ElementType.ReferentialConstraints)
            {
                // FromRole is the principal end in the referential constraint
                var principalEnd = (AssociationEndMember)constraint.FromRole;

                if (!RequiredEnds.Contains(principalEnd)
                    &&
                    !OptionalEnds.Contains(principalEnd))
                {
                    AddEnd(ref IncludedValueEnds, principalEnd);
                }
            }

            FixSet(ref IncludedValueEnds);
        }

        // <summary>
        // Initialize given required ends.
        // </summary>
        internal AssociationSetMetadata(IEnumerable<AssociationEndMember> requiredEnds)
        {
            if (requiredEnds.Any())
            {
                RequiredEnds = new Set<AssociationEndMember>(requiredEnds);
            }
            FixSet(ref RequiredEnds);
            FixSet(ref OptionalEnds);
            FixSet(ref IncludedValueEnds);
        }

        private static void AddEnd(ref Set<AssociationEndMember> set, AssociationEndMember element)
        {
            if (null == set)
            {
                set = new Set<AssociationEndMember>();
            }
            set.Add(element);
        }

        private static void FixSet(ref Set<AssociationEndMember> set)
        {
            if (null == set)
            {
                set = Set<AssociationEndMember>.Empty;
            }
            else
            {
                set.MakeReadOnly();
            }
        }
    }
}
