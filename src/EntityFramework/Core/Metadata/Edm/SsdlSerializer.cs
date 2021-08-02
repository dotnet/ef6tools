// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Serializes the storage (database) section of an <see cref="EdmModel"/> to XML.
    /// </summary>
    public class SsdlSerializer
    {
        /// <summary>
        /// Occurs when an error is encountered serializing the model.
        /// </summary>
        public event EventHandler<DataModelErrorEventArgs> OnError;

        /// <summary>
        /// Serialize the <see cref="EdmModel" /> to the <see cref="XmlWriter" />
        /// </summary>
        /// <param name="dbDatabase"> The EdmModel to serialize </param>
        /// <param name="provider"> Provider information on the Schema element </param>
        /// <param name="providerManifestToken"> ProviderManifestToken information on the Schema element </param>
        /// <param name="xmlWriter"> The XmlWriter to serialize to </param>
        /// <param name="serializeDefaultNullability">A value indicating whether to serialize Nullable attributes when they are set to the default value.</param>
        /// <returns> true if model can be serialized, otherwise false </returns>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nullability")]
        public virtual bool Serialize(
            EdmModel dbDatabase, string provider, string providerManifestToken, XmlWriter xmlWriter, bool serializeDefaultNullability = true)
        {
            Check.NotNull(dbDatabase, "dbDatabase");
            Check.NotEmpty(provider, "provider");
            Check.NotEmpty(providerManifestToken, "providerManifestToken");
            Check.NotNull(xmlWriter, "xmlWriter");

            if (ValidateModel(dbDatabase))
            {
                CreateVisitor(xmlWriter, dbDatabase, serializeDefaultNullability)
                    .Visit(dbDatabase, provider, providerManifestToken);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serialize the <see cref="EdmModel" /> to the <see cref="XmlWriter" />
        /// </summary>
        /// <param name="dbDatabase"> The EdmModel to serialize </param>
        /// <param name="namespaceName"> Namespace name on the Schema element </param>
        /// <param name="provider"> Provider information on the Schema element </param>
        /// <param name="providerManifestToken"> ProviderManifestToken information on the Schema element </param>
        /// <param name="xmlWriter"> The XmlWriter to serialize to </param>
        /// <param name="serializeDefaultNullability">A value indicating whether to serialize Nullable attributes when they are set to the default value.</param>
        /// <returns> true if model can be serialized, otherwise false </returns>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nullability")]
        public virtual bool Serialize(
            EdmModel dbDatabase, string namespaceName, string provider, string providerManifestToken, XmlWriter xmlWriter,
            bool serializeDefaultNullability = true)
        {
            Check.NotNull(dbDatabase, "dbDatabase");
            Check.NotEmpty(namespaceName, "namespaceName");
            Check.NotEmpty(provider, "provider");
            Check.NotEmpty(providerManifestToken, "providerManifestToken");
            Check.NotNull(xmlWriter, "xmlWriter");

            if (ValidateModel(dbDatabase))
            {
                CreateVisitor(xmlWriter, dbDatabase, serializeDefaultNullability)
                    .Visit(dbDatabase, namespaceName, provider, providerManifestToken);
                return true;
            }

            return false;
        }

        private bool ValidateModel(EdmModel model)
        {
            bool modelIsValid = true;

            Action<DataModelErrorEventArgs> onErrorAction =
                e =>
                {
                    // Ssdl serializer writes metadata items marked as invalid as comments
                    // therefore we should not report errors for those.
                    var metadataItem = e.Item as MetadataItem;
                    if (metadataItem == null || !MetadataItemHelper.IsInvalid(metadataItem))
                    {
                        modelIsValid = false;
                        if (OnError != null)
                        {
                            OnError(this, e);
                        }
                    }
                };

            if (model.NamespaceNames.Count() > 1
                || model.Containers.Count() != 1)
            {
                onErrorAction(
                    new DataModelErrorEventArgs
                    {
                        ErrorMessage = Strings.Serializer_OneNamespaceAndOneContainer,
                    });
            }

            var validator = new DataModelValidator();
            validator.OnError += (_, e) => onErrorAction(e);
            validator.Validate(model, true);

            return modelIsValid;
        }

        private static EdmSerializationVisitor CreateVisitor(XmlWriter xmlWriter, EdmModel dbDatabase, bool serializeDefaultNullability)
        {
            return new EdmSerializationVisitor(xmlWriter, dbDatabase.SchemaVersion, serializeDefaultNullability);
        }
    }
}
