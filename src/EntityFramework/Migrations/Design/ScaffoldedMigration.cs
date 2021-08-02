﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Design
{
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a code-based migration that has been scaffolded and is ready to be written to a file.
    /// </summary>
    [Serializable]
    public class ScaffoldedMigration
    {
        private string _migrationId;
        private string _userCode;
        private string _designerCode;
        private string _language;
        private string _directory;
        private readonly Dictionary<string, object> _resources = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the unique identifier for this migration.
        /// Typically used for the file name of the generated code.
        /// </summary>
        public string MigrationId
        {
            get { return _migrationId; }
            set
            {
                Check.NotEmpty(value, "value");

                _migrationId = value;
            }
        }

        /// <summary>
        /// Gets or sets the scaffolded migration code that the user can edit.
        /// </summary>
        public string UserCode
        {
            get { return _userCode; }
            set
            {
                Check.NotEmpty(value, "value");

                _userCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the scaffolded migration code that should be stored in a code behind file.
        /// </summary>
        public string DesignerCode
        {
            get { return _designerCode; }
            set
            {
                Check.NotEmpty(value, "value");

                _designerCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the programming language used for this migration.
        /// Typically used for the file extension of the generated code.
        /// </summary>
        public string Language
        {
            get { return _language; }
            set
            {
                Check.NotEmpty(value, "value");

                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets the subdirectory in the user's project that this migration should be saved in.
        /// </summary>
        public string Directory
        {
            get { return _directory; }
            set
            {
                Check.NotEmpty(value, "value");

                _directory = value;
            }
        }

        /// <summary>
        /// Gets a dictionary of string resources to add to the migration resource file.
        /// </summary>
        public IDictionary<string, object> Resources
        {
            get { return _resources; }
        }

        /// <summary>
        /// Gets or sets whether the migration was re-scaffolded.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Rescaffold")]
        public bool IsRescaffold { get; set; }
    }
}
