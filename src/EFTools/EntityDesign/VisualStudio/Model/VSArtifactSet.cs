// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.Model
{
    using EnvDTE;
    using Microsoft.Data.Entity.Design.Model;
    using Microsoft.Data.Entity.Design.VisualStudio.Package;

    internal class VSArtifactSet : EntityDesignArtifactSet
    {
        internal VSArtifactSet(EFArtifact artifact)
            : base(artifact)
        {
        }

        internal Project GetProjectForArtifactSet()
        {
            Project project = null;
            string documentPath = null;
            var artifact = this.GetEntityDesignArtifact();
            if (artifact != null)
            {
                documentPath = artifact.Uri.LocalPath;
                project = VSHelpers.GetProjectForDocument(documentPath, PackageManager.Package);
            }
            return project;
        }
    }
}
