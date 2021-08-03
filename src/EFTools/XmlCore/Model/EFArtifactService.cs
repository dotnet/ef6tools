// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model
{
    internal class EFArtifactService
    {
        private readonly EFArtifact _artifact;

        public EFArtifactService(EFArtifact artifact)
        {
            _artifact = artifact;
        }

        /// <summary>
        ///     Return the "current" artifact for the currently loaded URI
        /// </summary>
        internal EFArtifact Artifact
        {
            get { return _artifact; }
        }
    }
}
