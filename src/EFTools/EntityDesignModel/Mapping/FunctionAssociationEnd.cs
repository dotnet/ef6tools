﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Mapping
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml.Linq;
    using Microsoft.Data.Entity.Design.Model.Entity;

    internal class FunctionAssociationEnd : EFElement
    {
        internal static readonly string ElementName = "AssociationEnd";

        internal static readonly string AttributeAssociationSet = "AssociationSet";
        internal static readonly string AttributeFrom = "From";
        internal static readonly string AttributeTo = "To";

        private readonly List<FunctionScalarProperty> _properties = new List<FunctionScalarProperty>();
        private SingleItemBinding<AssociationSet> _assocSet;
        private SingleItemBinding<AssociationSetEnd> _from;
        private SingleItemBinding<AssociationSetEnd> _to;

        internal FunctionAssociationEnd(EFElement parent, XElement element)
            : base(parent, element)
        {
            Debug.Assert((parent as ModificationFunction) != null, "parent should be a ModificationFunction");
        }

        internal override string EFTypeName
        {
            get { return ElementName; }
        }

        internal ModificationFunction ModificationFunction
        {
            get
            {
                var parent = Parent as ModificationFunction;
                Debug.Assert(parent != null, "this.Parent should be a ModificationFunction");
                return parent;
            }
        }

        /// <summary>
        ///     Manages the content of the AssociationSet attribute
        /// </summary>
        internal SingleItemBinding<AssociationSet> AssociationSet
        {
            get
            {
                if (_assocSet == null)
                {
                    _assocSet = new SingleItemBinding<AssociationSet>(
                        this,
                        AttributeAssociationSet,
                        AssociationSetNameNormalizer.NameNormalizer
                        );
                }
                return _assocSet;
            }
        }

        /// <summary>
        ///     Manages the content of the From attribute
        /// </summary>
        internal SingleItemBinding<AssociationSetEnd> From
        {
            get
            {
                if (_from == null)
                {
                    _from = new SingleItemBinding<AssociationSetEnd>(
                        this,
                        AttributeFrom,
                        AssociationSetEndNameNormalizer.NameNormalizer
                        );
                }
                return _from;
            }
        }

        /// <summary>
        ///     Manages the content of the To attribute
        /// </summary>
        internal SingleItemBinding<AssociationSetEnd> To
        {
            get
            {
                if (_to == null)
                {
                    _to = new SingleItemBinding<AssociationSetEnd>(
                        this,
                        AttributeTo,
                        AssociationSetEndNameNormalizer.NameNormalizer
                        );
                }
                return _to;
            }
        }

        internal void AddScalarProperty(FunctionScalarProperty prop)
        {
            _properties.Add(prop);
        }

        internal IList<FunctionScalarProperty> ScalarProperties()
        {
            return _properties.AsReadOnly();
        }

#if DEBUG
        internal override ICollection<string> MyAttributeNames()
        {
            var s = base.MyAttributeNames();
            s.Add(AttributeAssociationSet);
            s.Add(AttributeFrom);
            s.Add(AttributeTo);
            return s;
        }
#endif

#if DEBUG
        internal override ICollection<string> MyChildElementNames()
        {
            var s = base.MyChildElementNames();
            s.Add(FunctionScalarProperty.ElementName);
            return s;
        }
#endif

        // we unfortunately get a warning from the compiler when we use the "base" keyword in "iterator" types generated by using the
        // "yield return" keyword.  By adding this method, I was able to get around this.  Unfortunately, I wasn't able to figure out
        // a way to implement this once and have derived classes share the implementation (since the "base" keyword is resolved at 
        // compile-time and not at runtime.
        private IEnumerable<EFObject> BaseChildren
        {
            get { return base.Children; }
        }

        internal override IEnumerable<EFObject> Children
        {
            get
            {
                foreach (var efobj in BaseChildren)
                {
                    yield return efobj;
                }

                foreach (var child in ScalarProperties())
                {
                    yield return child;
                }

                yield return AssociationSet;
                yield return From;
                yield return To;
            }
        }

        protected override void OnChildDeleted(EFContainer efContainer)
        {
            var sp = efContainer as FunctionScalarProperty;
            if (sp != null)
            {
                _properties.Remove(sp);
                return;
            }

            base.OnChildDeleted(efContainer);
        }

        protected override void PreParse()
        {
            Debug.Assert(State != EFElementState.Parsed, "this object should not already be in the parsed state");

            ClearEFObject(_assocSet);
            _assocSet = null;

            ClearEFObject(_from);
            _from = null;

            ClearEFObject(_to);
            _to = null;

            ClearEFObjectCollection(_properties);

            base.PreParse();
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            if (elem.Name.LocalName == FunctionScalarProperty.ElementName)
            {
                var prop = new FunctionScalarProperty(this, elem);
                _properties.Add(prop);
                prop.Parse(unprocessedElements);
            }
            else
            {
                return base.ParseSingleElement(unprocessedElements, elem);
            }

            return true;
        }

        private string DisplayNameInternal(bool localize)
        {
            string resource;
            if (localize)
            {
                resource = Resources.MappingModel_FunctionAssociationEndDisplayName;
            }
            else
            {
                resource = "{0} {1}->{2} (AssociationEnd)";
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                resource,
                AssociationSet.RefName,
                From.RefName,
                To.RefName);
        }

        internal override string DisplayName
        {
            get { return DisplayNameInternal(true); }
        }

        internal override string NonLocalizedDisplayName
        {
            get { return DisplayNameInternal(false); }
        }

        protected override void DoResolve(EFArtifactSet artifactSet)
        {
            AssociationSet.Rebind();
            From.Rebind();
            To.Rebind();

            if (AssociationSet.Status == BindingStatus.Known
                && From.Status == BindingStatus.Known
                && To.Status == BindingStatus.Known)
            {
                State = EFElementState.Resolved;
            }
        }

        internal override void GetXLinqInsertPosition(EFElement child, out XNode insertAt, out bool insertBefore)
        {
            if (child is FunctionScalarProperty)
            {
                /// 557417: push these to the top so that they are always before any ResultBinding elements
                insertAt = FirstChildXElementOrNull();
                insertBefore = true;
            }
            else
            {
                base.GetXLinqInsertPosition(child, out insertAt, out insertBefore);
            }
        }
    }
}
