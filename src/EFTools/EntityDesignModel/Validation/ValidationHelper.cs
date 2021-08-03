// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    using System.Linq;
    using Microsoft.Data.Entity.Design.Model.Entity;

    internal static class ValidationHelper
    {
        internal static bool IsStorageModelEmpty(EFArtifact artifact)
        {
            var result = false;

            var storageModel = artifact.StorageModel();
            if (storageModel != null)
            {
                var container = storageModel.FirstEntityContainer as StorageEntityContainer;
                if (container != null)
                {
                    var element = container.Children.OfType<EFElement>().FirstOrDefault<EFElement>();
                    if (element == null)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}
