﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine;

    internal class DbGenWorkflowFileListConverter : ExtensibleFileListConverter
    {
        protected override string SubDirPath
        {
            get { return DatabaseGenerationEngine._dbGenFolderName; }
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var standardValues = new List<string>();
            standardValues.AddRange(DatabaseGenerationEngine.WorkflowFileManager.VSFiles.Select(fi => MacroizeFilePath(fi.FullName)));
            standardValues.AddRange(DatabaseGenerationEngine.WorkflowFileManager.UserFiles.Select(fi => MacroizeFilePath(fi.FullName)));
            return new StandardValuesCollection(standardValues);
        }
    }
}
