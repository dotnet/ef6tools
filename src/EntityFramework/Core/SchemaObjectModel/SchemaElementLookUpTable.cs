// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;

    // <summary>
    // Summary description for SchemaElementLookUpTable.
    // </summary>
    internal sealed class SchemaElementLookUpTable<T> : IEnumerable<T>, ISchemaElementLookUpTable<T>
        where T : SchemaElement
    {
        #region Instance Fields

        private Dictionary<string, T> _keyToType;
        private readonly List<string> _keysInDefOrder = new List<string>();

        #endregion

        #region Public Methods

        public int Count
        {
            get { return KeyToType.Count; }
        }

        public bool ContainsKey(string key)
        {
            return KeyToType.ContainsKey(KeyFromName(key));
        }

        public T LookUpEquivalentKey(string key)
        {
            key = KeyFromName(key);
            T element;

            if (KeyToType.TryGetValue(key, out element))
            {
                return element;
            }

            return null;
        }

        public T this[string key]
        {
            get { return KeyToType[KeyFromName(key)]; }
        }

        public T GetElementAt(int index)
        {
            return KeyToType[_keysInDefOrder[index]];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new SchemaElementLookUpTableEnumerator<T, T>(KeyToType, _keysInDefOrder);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SchemaElementLookUpTableEnumerator<T, T>(KeyToType, _keysInDefOrder);
        }

        public IEnumerator<S> GetFilteredEnumerator<S>()
            where S : T
        {
            return new SchemaElementLookUpTableEnumerator<S, T>(KeyToType, _keysInDefOrder);
        }

        // <summary>
        // Add the given type to the schema look up table. If there is an error, it
        // adds the error and returns false. otherwise, it adds the type to the lookuptable
        // and returns true
        // </summary>
        public AddErrorKind TryAdd(T type)
        {
            DebugCheck.NotNull(type);

            if (String.IsNullOrEmpty(type.Identity))
            {
                return AddErrorKind.MissingNameError;
            }

            var key = KeyFromElement(type);
            T element;
            if (KeyToType.TryGetValue(key, out element))
            {
                return AddErrorKind.DuplicateNameError;
            }

            KeyToType.Add(key, type);
            _keysInDefOrder.Add(key);

            return AddErrorKind.Succeeded;
        }

        public void Add(T type, bool doNotAddErrorForEmptyName, Func<object, string> duplicateKeyErrorFormat)
        {
            DebugCheck.NotNull(type);
            DebugCheck.NotNull(duplicateKeyErrorFormat);

            var error = TryAdd(type);

            if (error == AddErrorKind.MissingNameError)
            {
                if (!doNotAddErrorForEmptyName)
                {
                    type.AddError(
                        ErrorCode.InvalidName, EdmSchemaErrorSeverity.Error,
                        Strings.MissingName);
                }
                return;
            }
            else if (error == AddErrorKind.DuplicateNameError)
            {
                type.AddError(
                    ErrorCode.AlreadyDefined, EdmSchemaErrorSeverity.Error,
                    duplicateKeyErrorFormat(type.FQName));
            }
            else
            {
                Debug.Assert(error == AddErrorKind.Succeeded, "Invalid error encountered");
            }
        }

        #endregion

        #region Internal Methods

        #endregion

        #region Private Methods

        private static string KeyFromElement(T type)
        {
            return KeyFromName(type.Identity);
        }

        private static string KeyFromName(string unnormalizedKey)
        {
            DebugCheck.NotEmpty(unnormalizedKey);

            return unnormalizedKey;
        }

        #endregion

        #region Private Properties

        private Dictionary<string, T> KeyToType
        {
            get
            {
                if (_keyToType == null)
                {
                    _keyToType = new Dictionary<string, T>(StringComparer.Ordinal);
                }
                return _keyToType;
            }
        }

        #endregion
    }
}
