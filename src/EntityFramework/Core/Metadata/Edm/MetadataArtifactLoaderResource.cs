// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.SchemaObjectModel;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    // <summary>
    // This class represents one resource item to be loaded from an assembly.
    // </summary>
    internal class MetadataArtifactLoaderResource : MetadataArtifactLoader, IComparable
    {
        private readonly bool _alreadyLoaded;
        private readonly Assembly _assembly;
        private readonly string _resourceName;

        // <summary>
        // Constructor - loads the resource stream
        // </summary>
        // <param name="uriRegistry"> The global registry of URIs </param>
        internal MetadataArtifactLoaderResource(Assembly assembly, string resourceName, ICollection<string> uriRegistry)
        {
            DebugCheck.NotNull(assembly);
            DebugCheck.NotNull(resourceName);

            _assembly = assembly;
            _resourceName = resourceName;

            var tempPath = MetadataArtifactLoaderCompositeResource.CreateResPath(_assembly, _resourceName);
            _alreadyLoaded = uriRegistry.Contains(tempPath);
            if (!_alreadyLoaded)
            {
                uriRegistry.Add(tempPath);

                // '_alreadyLoaded' is not set because while we would like to prevent
                // other instances of MetadataArtifactLoaderFile that wrap the same
                // _path from being added to the list of paths/readers, we do want to
                // include this particular instance.
            }
        }

        public override string Path
        {
            get { return MetadataArtifactLoaderCompositeResource.CreateResPath(_assembly, _resourceName); }
        }

        // <summary>
        // Implementation of IComparable.CompareTo()
        // </summary>
        // <param name="obj"> The object to compare to </param>
        // <returns> 0 if the loaders are "equal" (i.e., have the same _path value) </returns>
        public int CompareTo(object obj)
        {
            var loader = obj as MetadataArtifactLoaderResource;
            if (loader != null)
            {
                return string.Compare(Path, loader.Path, StringComparison.OrdinalIgnoreCase);
            }

            Debug.Assert(false, "object is not a MetadataArtifactLoaderResource");
            return -1;
        }

        // <summary>
        // Equals() returns true if the objects have the same _path value
        // </summary>
        // <param name="obj"> The object to compare to </param>
        // <returns> true if the objects have the same _path value </returns>
        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        // <summary>
        // GetHashCode override that defers the result to the _path member variable.
        // </summary>
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        // <summary>
        // Get paths to artifacts for a specific DataSpace.
        // </summary>
        // <param name="spaceToGet"> The DataSpace for the artifacts of interest </param>
        // <returns> A List of strings identifying paths to all artifacts for a specific DataSpace </returns>
        public override List<string> GetPaths(DataSpace spaceToGet)
        {
            var list = new List<string>();
            if (!_alreadyLoaded
                && IsArtifactOfDataSpace(Path, spaceToGet))
            {
                list.Add(Path);
            }
            return list;
        }

        // <summary>
        // Get paths to all artifacts
        // </summary>
        // <returns> A List of strings identifying paths to all resources </returns>
        public override List<string> GetPaths()
        {
            var list = new List<string>();
            if (!_alreadyLoaded)
            {
                list.Add(Path);
            }
            return list;
        }

        // <summary>
        // Create and return an XmlReader around the resource represented by this instance.
        // </summary>
        // <returns> A List of XmlReaders for all resources </returns>
        public override List<XmlReader> GetReaders(Dictionary<MetadataArtifactLoader, XmlReader> sourceDictionary)
        {
            var list = new List<XmlReader>();
            if (!_alreadyLoaded)
            {
                var reader = CreateReader();
                list.Add(reader);

                if (sourceDictionary != null)
                {
                    sourceDictionary.Add(this, reader);
                }
            }
            return list;
        }

        private XmlReader CreateReader()
        {
            var stream = LoadResource();

            var readerSettings = Schema.CreateEdmStandardXmlReaderSettings();
            // close the stream when the xmlreader is closed
            // now the reader owns the stream
            readerSettings.CloseInput = true;

            // we know that we aren't reading a fragment
            readerSettings.ConformanceLevel = ConformanceLevel.Document;
            var reader = XmlReader.Create(stream, readerSettings);
            // cannot set the base URI because res:// URIs cause the schema parser
            // to choke

            return reader;
        }

        // <summary>
        // Create and return an XmlReader around the resource represented by this instance
        // if it is of the requested DataSpace type.
        // </summary>
        // <param name="spaceToGet"> The DataSpace corresponding to the requested artifacts </param>
        // <returns> A List of XmlReader objects </returns>
        public override List<XmlReader> CreateReaders(DataSpace spaceToGet)
        {
            var list = new List<XmlReader>();
            if (!_alreadyLoaded)
            {
                if (IsArtifactOfDataSpace(Path, spaceToGet))
                {
                    var reader = CreateReader();
                    list.Add(reader);
                }
            }
            return list;
        }

        // <summary>
        // This method parses the path to the resource and attempts to load it.
        // The method also accounts for the wildcard assembly name.
        // </summary>
        private Stream LoadResource()
        {
            Stream resourceStream;
            if (TryCreateResourceStream(out resourceStream))
            {
                return resourceStream;
            }
            throw new MetadataException(Strings.UnableToLoadResource);
        }

        private bool TryCreateResourceStream(out Stream resourceStream)
        {
            resourceStream = _assembly.GetManifestResourceStream(_resourceName);
            return resourceStream != null;
        }
    }
}
