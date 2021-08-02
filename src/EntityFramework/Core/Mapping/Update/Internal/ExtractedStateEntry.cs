// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.Update.Internal
{
    using System.Data.Entity.Utilities;
    using System.Diagnostics;

    // <summary>
    // Represents the data contained in a StateEntry using internal data structures
    // of the UpdatePipeline.
    // </summary>
    internal struct ExtractedStateEntry
    {
        internal readonly EntityState State;
        internal readonly PropagatorResult Original;
        internal readonly PropagatorResult Current;
        internal readonly IEntityStateEntry Source;

        internal ExtractedStateEntry(EntityState state, PropagatorResult original, PropagatorResult current, IEntityStateEntry source)
        {
            State = state;
            Original = original;
            Current = current;
            Source = source;
        }

        internal ExtractedStateEntry(UpdateTranslator translator, IEntityStateEntry stateEntry)
        {
            DebugCheck.NotNull(translator);
            DebugCheck.NotNull(stateEntry);

            State = stateEntry.State;
            Source = stateEntry;

            switch (stateEntry.State)
            {
                case EntityState.Deleted:
                    Original = translator.RecordConverter.ConvertOriginalValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.AllModified);
                    Current = null;
                    break;
                case EntityState.Unchanged:
                    Original = translator.RecordConverter.ConvertOriginalValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.NoneModified);
                    Current = translator.RecordConverter.ConvertCurrentValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.NoneModified);
                    break;
                case EntityState.Modified:
                    Original = translator.RecordConverter.ConvertOriginalValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.SomeModified);
                    Current = translator.RecordConverter.ConvertCurrentValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.SomeModified);
                    break;
                case EntityState.Added:
                    Original = null;
                    Current = translator.RecordConverter.ConvertCurrentValuesToPropagatorResult(
                        stateEntry, ModifiedPropertiesBehavior.AllModified);
                    break;
                default:
                    Debug.Assert(false, "Unexpected IEntityStateEntry.State for entity " + stateEntry.State);
                    Original = null;
                    Current = null;
                    break;
            }
        }
    }
}
