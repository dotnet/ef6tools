﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    using System.Collections.ObjectModel;
    using System.Data.Common;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;

    internal class NextResultGenerator
    {
        private readonly EntityCommand _entityCommand;
        private readonly ReadOnlyCollection<EntitySet> _entitySets;
        private readonly ObjectContext _context;
        private readonly EdmType[] _edmTypes;
        private readonly int _resultSetIndex;
        private readonly bool _streaming;
        private readonly MergeOption _mergeOption;

        internal NextResultGenerator(
            ObjectContext context, EntityCommand entityCommand, EdmType[] edmTypes, ReadOnlyCollection<EntitySet> entitySets,
            MergeOption mergeOption, bool streaming, int resultSetIndex)
        {
            _context = context;
            _entityCommand = entityCommand;
            _entitySets = entitySets;
            _edmTypes = edmTypes;
            _resultSetIndex = resultSetIndex;
            _streaming = streaming;
            _mergeOption = mergeOption;
        }

        internal ObjectResult<TElement> GetNextResult<TElement>(DbDataReader storeReader)
        {
            var isNextResult = false;
            try
            {
                isNextResult = storeReader.NextResult();
            }
            catch (Exception e)
            {
                if (e.IsCatchableExceptionType())
                {
                    throw new EntityCommandExecutionException(Strings.EntityClient_StoreReaderFailed, e);
                }
                throw;
            }

            if (isNextResult)
            {
                var edmType = _edmTypes[_resultSetIndex];
                MetadataHelper.CheckFunctionImportReturnType<TElement>(edmType, _context.MetadataWorkspace);
                return _context.MaterializedDataRecord<TElement>(
                    _entityCommand, storeReader, _resultSetIndex, _entitySets, _edmTypes, null, _mergeOption, _streaming);
            }
            else
            {
                return null;
            }
        }
    }
}
